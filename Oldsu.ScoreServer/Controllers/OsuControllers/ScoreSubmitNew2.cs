using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private readonly ILogger<ScoreSubmission> _logger;
        
        public ScoreSubmitNew2(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task Post()
        {
            var parser = await MultipartFormDataParser.ParseAsync(HttpContext.Request.Body);

            var serializedScore = await TypeExtensions.SerializeScoreString(
                parser.GetParameterValue("score").Split(":"));

            await using var db = new Database();

            var user = await db.AuthenticateAsync(
                serializedScore?.User?.Username ?? "", parser.GetParameterValue("password"));

            if (user == null)
            {
                await HttpContext.Response.WriteStringAsync(ScoreSubmitManager.WrongPasswordMessage);
                await HttpContext.Response.CompleteAsync();

                await Global.LoggingManager.LogCritical<ScoreSubmitNew>(
                    $"{serializedScore?.User?.Username} submitted a score with a wrong password. (attempted fraud?)");

                return;
            }

            await Global.LoggingManager.LogInfo<ScoreSubmitNew>(
                $"{user.Username} ({user.UserID}) procs: {parser.GetParameterValue("procs")}");
            
            if (!Request.Headers.TryGetValue("User-Agent", out var uAgent) || uAgent.ToString() != "oldsu!/2100") 
            {
                await HttpContext.Response.CompleteAsync();

                await Global.LoggingManager.LogInfo<ScoreSubmitNew>(
                        $"{user.Username} ({user.UserID}) tried to submit a score with a wrong user agent: {uAgent.ToString()}");

                return;
            }

            serializedScore!.Version = uAgent.ToString();
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

            var submitManager = new ScoreSubmitManager
                (serializedScore, beatmap);

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
            
            foreach (var file in parser.Files)
            {
                switch (file.Name)
                {
                    case "replay":
                        replayFound = true;
                        if (file.Data.Length is 0 or > 50000000)
                        {
                            // if user didnt pass the map the replay is going to be 0
                            if (!(file.Data.Length == 0 && serializedScore.Passed == false))
                            {
                                isSubmittable = false;

                                if (bannedReason != null)
                                    bannedReason += $" Replay was of size {file.Data.Length}.";
                                else
                                    bannedReason = $"Replay was of size {file.Data.Length}.";
                            }
                        }
                        else
                            replay = file.Data;

                        break;
                    default:
                        isSubmittable = false;
                        if (bannedReason != null)
                            bannedReason += $" unknown form values {file.Name}.";
                        else
                            bannedReason = $"unknown form values {file.Name}.";
                        await Global.LoggingManager.LogCritical<ScoreSubmitNew>(
                            $"{user.Username} ({user.UserID}) submitted a score with unknown form values {file.Name}.");
                        break;
                }
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
                    //db.ban(username)
                    await Task.Delay(1);
                
                await HttpContext.Response.CompleteAsync();
                
                await Global.LoggingManager.LogCritical<ScoreSubmitNew>(
                    $"{user.Username} ({user.UserID}) tried to submit an unsubmittable score due to: {bannedReason}");

                return;
            }

            var oldStats = await db.StatsWithRank
                .Where(s => s.UserID == user.UserID &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // user either new or trying a new gamemode
            if (oldStats == null)
                await db.AddStatsAsync(user.UserID, serializedScore.Gamemode);
            
            oldStats = await db.StatsWithRank
                .Where(s => s.UserID == user.UserID &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            
            var newStats = db.Entry(oldStats).CurrentValues.Clone().ToObject() as StatsWithRank;

            var oldScore = await db.HighScoresWithRank
                .Where(s => s.UserId == serializedScore.UserId && 
                             s.BeatmapHash == serializedScore.BeatmapHash &&
                             s.Gamemode == serializedScore.Gamemode)
                .FirstOrDefaultAsync();
            
            var transaction = await db.Database.BeginTransactionAsync();

            try {
                await submitManager.SubmitScore();

                submitManager.UpdateStats(newStats, oldScore);

                await db.ExecuteStatUpdate(newStats!);
                
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
            
            var nextUserStats = await db.StatsWithRank
                .Where(s => s.Rank == s.Rank - 1 &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .FirstOrDefaultAsync();

            if (serializedScore.Passed)
            {
                await HttpContext.Response.WriteStringAsync(
                    submitManager.GetScorePanelString((newScore, oldScore), (newStats, oldStats), nextUserStats));
            }

            await HttpContext.Response.CompleteAsync();
        }
    }
}