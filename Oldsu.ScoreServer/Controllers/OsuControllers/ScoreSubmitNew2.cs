using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpMultipartParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oldsu.Enums;
using Oldsu.Logging;
using Oldsu.ScoreServer.Managers;
using Oldsu.Types;
using Oldsu.Utils;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/web/osu-submit-new2.php")]
    public class ScoreSubmitNew2 : ControllerBase
    {
        private BanchoInterface _banchoInterface;
        
        public ScoreSubmitNew2(BanchoInterface banchoInterface)
        {
            _banchoInterface = banchoInterface;
        }

        [HttpPost]
        public async Task Post()
        {
            var parser = await MultipartFormDataParser.ParseAsync(HttpContext.Request.Body);

            string[] scoreInfo = parser.GetParameterValue("score").Split(":");

            await using var db = new Database();
            
            var user = await db.AuthenticateAsync(
                scoreInfo[1], parser.GetParameterValue("password"));

            if (user == null)
            {
                await HttpContext.Response.WriteStringAsync(ScoreSubmitManager.WrongPasswordMessage);
                await HttpContext.Response.CompleteAsync();

                await Global.LoggingManager.LogCritical<ScoreSubmitNew>(
                    $"{scoreInfo[1]} submitted a score with a wrong password. (attempted fraud?)");

                return;
            }

            var serializedScore = await TypeExtensions.SerializeScoreString(scoreInfo, user.UserID);

            await Global.LoggingManager.LogInfo<ScoreSubmitNew>(
                $"{user.Username} ({user.UserID}) procs: {parser.GetParameterValue("procs")}");
            
            if (!Request.Headers.TryGetValue("User-Agent", out var uAgent) || (uAgent.ToString() != "oldsu!/2100" && uAgent.ToString() != "oldsu!/2110")) 
            {
                await HttpContext.Response.CompleteAsync();

                await Global.LoggingManager.LogInfo<ScoreSubmitNew>(
                        $"{user.Username} ({user.UserID}) tried to submit a score with a wrong user agent: {uAgent.ToString()}");

                return;
            }

            string ua = uAgent.ToString();
            string[] splittedUa = ua.Split('/');

            serializedScore!.Version = splittedUa[1];
            serializedScore!.User = user;

            if (user.Banned)
            {
                await HttpContext.Response.WriteStringAsync(ScoreSubmitManager.BannedMessage);
                await HttpContext.Response.CompleteAsync();

                await Global.LoggingManager.LogInfo<ScoreSubmitNew>(
                    $"{user.Username} ({user.UserID}) tried to submit a score while banned.");

                return;
            }

            var beatmap = await db.Beatmaps
                .Where(b => b.BeatmapHash == serializedScore.BeatmapHash)
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();

            var submitManager = new ScoreSubmitManager(user, serializedScore, beatmap);

            if (!submitManager.SetStrategy())
            {
                await HttpContext.Response.CompleteAsync();

                await Global.LoggingManager.LogInfo<ScoreSubmitNew>(
                    $"{user.Username} ({user.UserID}) tried to submit a score with an unsupported client.");

                return;
            }

            var (isSubmittable, responseString, bannedReason) = submitManager.ValidateScore();

            var replayFound = false;

            Stream replay = null;

            FilePart part = parser.Files.FirstOrDefault(n => n.Name == "replay");
            
            if (part == null)
                replayFound = false;
            
            else if (part.Data.Length is 0 or > 50000000)
            {
                // if user didnt pass the map the replay is going to be 0
                if (!(part.Data.Length == 0 && serializedScore.Passed == false))
                {
                    isSubmittable = false;

                    if (bannedReason != null)
                        bannedReason += $" Replay was of size {part.Data.Length}.";
                    else
                        bannedReason = $"Replay was of size {part.Data.Length}.";
                }
            }
            else
            {
                replayFound = true;
                replay = part.Data;
            }


            if (!replayFound)
                if (bannedReason != null)
                    bannedReason += $" replay file not found.";
                else
                    bannedReason = $"replay file not found.";

            if (!isSubmittable)
            {
                await HttpContext.Response.WriteStringAsync(responseString);

                if (responseString == ScoreSubmitManager.BannedMessage)
                {
                    await db.BanUser(user.UserID, bannedReason);
                    await _banchoInterface.KickUser(user.Username);
                }

                await HttpContext.Response.CompleteAsync();
                
                await Global.LoggingManager.LogCritical<ScoreSubmitNew>(
                    $"{user.Username} ({user.UserID}) tried to submit an unsubmittable score due to: {bannedReason}");

                return;
            }

            var stats = await db.Stats
                .Where(s => s.UserID == user.UserID &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .FirstOrDefaultAsync();

            // user either new or trying a new gamemode
            if (stats == null)
            {
                await db.AddStatsAsync(user.UserID, serializedScore.Gamemode);
                stats = await db.Stats
                    .Where(s => s.UserID == user.UserID &&
                                s.Mode == (Mode)serializedScore.Gamemode)
                    .FirstAsync();
            }
            
            var oldStats = db.Entry(stats).CurrentValues.Clone().ToObject() as Stats;

            var oldScore = await db.HighScoresWithRank
                .Where(s => s.UserId == serializedScore.UserId && 
                             s.BeatmapHash == serializedScore.BeatmapHash &&
                             s.Gamemode == serializedScore.Gamemode)
                .FirstOrDefaultAsync();
            
            var transaction = await db.Database.BeginTransactionAsync();

            try {
                await submitManager.SubmitScore();

                submitManager.UpdateStats(stats, oldScore);

                await db.ExecuteStatUpdate(stats!);
                
                await transaction.CommitAsync();
            } catch {
                /* something bad happened, abort all the changes */
                await transaction.RollbackAsync();
                throw;
            }
            
            var newScore = await db.HighScoresWithRank                
                .Where(s => s.UserId == serializedScore.UserId && 
                            s.BeatmapHash == serializedScore.BeatmapHash &&
                            s.Gamemode == serializedScore.Gamemode)
                .FirstOrDefaultAsync();

            if (serializedScore.Passed)
                if (Global.AllowFileSaving)
                {
                    await using var replayFileStream = System.IO.File.Create($"{Global.ReplayFolder}{newScore.ScoreId}.osr");

                    replay!.Seek(0, SeekOrigin.Begin);
                    
                    await replay.CopyToAsync(replayFileStream);
                }
            
            
            var nextUserStatsWithRank = await db.StatsWithRank
                .Where(s => s.Rank == s.Rank - 1 &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .FirstOrDefaultAsync();

            var nextUserStats = nextUserStatsWithRank?.ToStats();
            
            if (serializedScore.Passed)
            {
                await HttpContext.Response.WriteStringAsync(
                    submitManager.GetScorePanelString((newScore, oldScore), (stats, oldStats), nextUserStats));
            }

            await HttpContext.Response.CompleteAsync();
        }
    }
}