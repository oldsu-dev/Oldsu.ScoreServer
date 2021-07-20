using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Oldsu.ScoreServer.Controllers
{
    [ApiController]
    [Route("/web/osu-getscores6.php")]
    public class GetScores : ControllerBase
    {
        private readonly ILogger<ScoreSubmission> _logger;

        public GetScores(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ContentResult> Get()
        {
            var db = new Database();

            var mapHash = HttpContext.Request.Query["c"];
            var userId = HttpContext.Request.Query["u"];
            var gamemode = HttpContext.Request.Query["m"];

            var user = await db.Users
                .FindAsync(uint.Parse(userId));

            if (user == null)
                return Content("error");
            
            var scoresOnMap = await db.Scores
                .Where(s => s.BeatmapHash.Equals(mapHash))
                .Include(s => s.User)
                .ToArrayAsync();
            
            var beatmap = await db.Beatmaps
                .Where(b => b.BeatmapHash.Equals(mapHash))
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();

            if (beatmap == null)
                return Content("-1\n0\n \n0");

            /*
             `response`:
               ranking status: approved, unranked, ranked, etc..
               online offset: self explanatory
               map title: the thing you see in the beginning of a map
               map rating: the stars you see on top left.
               
               first score: 1|56|6|1|..... user's personal best score. if there is no personal best it's empty/something that isn't valid
               other scores: same thing...... all the other scores that show up on the leaderboard
            */
            
            var responseString = $"{beatmap.Beatmapset.RankingStatus}\n0\n \n{beatmap.Rating}\n";
            
            var scoresAlreadyAdded = new List<string>();
            var scores = new StringBuilder();
            var personalBestScore = "\n";
            var leaderboardRank = 1;
            
            foreach (var score in scoresOnMap)
                // only adds users whose username isn't yet in `scoresAlreadyAdded`
                if (scoresAlreadyAdded.FirstOrDefault(u => u.Contains(score.User.Username)) == null)
                {
                    if (score.User.Username == user.Username)
                        personalBestScore = $"{score.ScoreId}|{score.User.Username}|{score.Score}|{score.MaxCombo}|{score.Hit50}|{score.Hit100}|" +
                                            $"{score.Hit300}|{score.HitMiss}|{score.HitKatu}|{score.HitGeki}|{(score.Perfect?1:0)}|{score.Mods}|" +
                                            $"{score.UserId}|{leaderboardRank}|1\n";
                    
                    scores.Append($"{score.ScoreId}|{score.User.Username}|{score.Score}|{score.MaxCombo}|{score.Hit50}|{score.Hit100}|" +
                                  $"{score.Hit300}|{score.HitMiss}|{score.HitKatu}|{score.HitGeki}|{(score.Perfect?1:0)}|{score.Mods}|" +
                                  $"{score.UserId}|{leaderboardRank}|1\n");
                    
                    scoresAlreadyAdded.Add(score.User.Username);
                    leaderboardRank++;
                }

            responseString += $"{personalBestScore}{scores}";

            return Content(responseString);
        }
    }
}