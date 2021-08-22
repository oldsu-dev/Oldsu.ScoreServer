﻿using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
    [Route("/web/osu-submit-new.php")]
    public class ScoreSubmitNew : ControllerBase // score submission for atleast 2009-2012
    {
        private readonly ILogger<ScoreSubmission> _logger;
        
        public ScoreSubmitNew(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }
        
        [HttpPost]
        public async Task Post()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            // todo save replay
            var replay = HttpContext.Request.Body;

            var serializedScore = await TypeExtensions.SerializeScoreString(
                HttpContext.Request.Query["score"].ToString().Split(":"));

            // todo not passed logic
            if (!serializedScore.Passed)
                return;

            await using var db = new Database();

            var user = await db.AuthenticateAsync(
                serializedScore?.User?.Username ?? "", HttpContext.Request.Query["pass"]);
            
            if (user == null)
            {
                await HttpContext.Response.WriteStringAsync(ScoreSubmitManager.WrongPasswordMessage);
                await HttpContext.Response.CompleteAsync();
                
                await Global.LoggingManager.LogInfo<ScoreSubmitNew>($"{user.Username} ({user.UserID}) submitted a score with a wrong password.");

                
                return;
            }

            serializedScore!.User = user;

            if (user.Banned)
            {
                await HttpContext.Response.WriteStringAsync(ScoreSubmitManager.BannedMessage);
                await HttpContext.Response.CompleteAsync();

                await Global.LoggingManager.LogInfo<ScoreSubmitNew>($"{user.Username} ({user.UserID}) submitted a score while banned.");
                    
                return;
            }

            var beatmap = await db.Beatmaps
                .Where(b => b.BeatmapHash == serializedScore.BeatmapHash)
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();

            if (beatmap == null)
                await db.TestAddMapAsync(serializedScore.BeatmapHash);
            
            beatmap = await db.Beatmaps
                .Where(b => b.BeatmapHash == serializedScore.BeatmapHash)
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();
            
            var submitManager = new ScoreSubmitManager
                (serializedScore, beatmap);

            if (!submitManager.SetStrategy())
            {
                await HttpContext.Response.CompleteAsync();
                
                await Global.LoggingManager.LogInfo<ScoreSubmitNew>($"{user.Username} ({user.UserID}) tried to submit a score with an unsupported client.");

                return;
            }

            var (isSubmittable, responseString, bannedReason) = submitManager.ValidateScore();
            
            if (!isSubmittable)
            {
                await HttpContext.Response.WriteStringAsync(responseString);

                if (responseString == ScoreSubmitManager.BannedMessage)
                    //db.ban(username)
                    await Task.Delay(1);
                
                await HttpContext.Response.CompleteAsync();
                
                await Global.LoggingManager.LogCritical<ScoreSubmitNew>(
                    $"{user.Username} ({user.UserID}) tried to submit an unsubmittable score due to {bannedReason}");

                return;
            }

            var oldStats = await db.StatsWithRank
                .Where(s => s.UserID == user.UserID &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (oldStats == null)
                await db.AddStatsAsync(user.UserID, serializedScore.Gamemode);
            
            oldStats = await db.StatsWithRank
                .Where(s => s.UserID == user.UserID &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            
            var newStats = db.Entry(oldStats).CurrentValues.Clone().ToObject() as StatsWithRank;

            var oldScore = await db.HighScoresWithRank
                .Where(s => s.BeatmapHash == serializedScore.BeatmapHash &&
                            s.Gamemode == serializedScore.Gamemode)
                .FirstOrDefaultAsync();

            await submitManager.SubmitScore();
            
            var newScore = await db.HighScoresWithRank                
                .Where(s => s.BeatmapHash == serializedScore.BeatmapHash &&
                            s.Gamemode == serializedScore.Gamemode)
                .FirstOrDefaultAsync();

            var nextUserStats = await db.StatsWithRank
                .Where(s => s.Rank == s.Rank - 1 &&
                            s.Mode == (Mode)serializedScore.Gamemode)
                .FirstOrDefaultAsync();

            submitManager.UpdateStats(newStats, oldScore);

            await db.ExecuteStatUpdate(newStats!);

            await HttpContext.Response.WriteStringAsync(
                submitManager.GetScorePanelString((newScore, oldScore), (newStats, oldStats), nextUserStats));
            
            await HttpContext.Response.CompleteAsync();
            
            sw.Stop();
            await Global.LoggingManager.LogInfo<GetScores>($"Request took {sw.ElapsedMilliseconds}ms");
        }
    }
}