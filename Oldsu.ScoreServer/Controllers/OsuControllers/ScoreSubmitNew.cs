using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            var replay = HttpContext.Request.Body;

            var serializedScore = await TypeExtensions.SerializeScoreString(
                HttpContext.Request.Query["score"].ToString().Split(":"));

            if (serializedScore.Passed)
                return;

            await using var db = new Database();

            var user = await db.AuthenticateAsync(
                serializedScore?.User?.Username ?? "", HttpContext.Request.Query["pass"]);
            
            if (user == null)
            {
                await HttpContext.Response.WriteStringAsync(ScoreSubmitManager.WrongPasswordMessage);
                await HttpContext.Response.CompleteAsync();
                return;
            }

            serializedScore!.User = user;

            if (user.Banned)
            {
                await HttpContext.Response.WriteStringAsync(ScoreSubmitManager.BannedMessage);
                await HttpContext.Response.CompleteAsync();
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
                return;
            }

            var oldStats = await db.StatsWithRank
                .Where(s => s.UserID == user.UserID)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (oldStats == null)
                await db.AddStatsAsync(user.UserID, serializedScore.Gamemode);
            
            oldStats = await db.StatsWithRank
                .Where(s => s.UserID == user.UserID)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            
            var newStats = db.Entry(oldStats).CurrentValues.Clone().ToObject() as StatsWithRank;

            var oldScore = await db.HighScoresWithRank
                .Where(s => s.BeatmapHash == serializedScore.BeatmapHash)
                .FirstOrDefaultAsync();

            await submitManager.SubmitScore();
            
            var newScore = await db.HighScoresWithRank                
                .Where(s => s.BeatmapHash == serializedScore.BeatmapHash)
                .FirstOrDefaultAsync();

            var nextUserStats = await db.StatsWithRank
                .Where(s => s.Rank == s.Rank - 1)
                .FirstOrDefaultAsync();

            submitManager.UpdateStats(newStats, oldScore);
            
            await newStats.SaveChangesAsync();

            await HttpContext.Response.WriteStringAsync(
                submitManager.GetScorePanelString((newScore, oldScore), (newStats, oldStats), nextUserStats));
            
            await HttpContext.Response.CompleteAsync();
        }
    }
}