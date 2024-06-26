using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oldsu.Logging;
using Oldsu.Logging.Strategies;
using Oldsu.ScoreServer.Managers;
using Oldsu.Types;
using Oldsu.Enums;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/web/osu-getscores6.php")]
    public class GetScores : ControllerBase
    {
        private readonly Database _db;

        // change to redis when needed
        private static IScoreManager _scoreManager = new NativeScoreManager();

        private UserInfo _requestingUser;
        private Beatmap _beatmap;
        private byte _gamemode;
        
        private List<HighScoreWithRank> _scoresOnMap;

        public GetScores(Database database)
        {
            _db = database;
        }

        [HttpGet]
        public async Task Get()
        {
            var sw = new Stopwatch();
            sw.Start();

            await using var db = new Database();
            
            var userId = HttpContext.Request.Query["u"].ToString();
            
            _requestingUser = await db.UserInfo
                .FindAsync(uint.Parse(userId));

            // the requesting user is not found from the database, end the request
            if (_requestingUser == null)
            {
                await HttpContext.Response.WriteStringAsync("unknown requesting user");
                await HttpContext.Response.CompleteAsync();
                
                await Global.LoggingManager.LogCritical<GetScores>(
                    $"User with unknown id ({userId}) tried to fetch scores. " +
                    $"IP Address: {HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? "unknown"}");

                return;
            }

            var mapHash = HttpContext.Request.Query["c"].ToString();

            _beatmap = await db.Beatmaps
                .Where(b => b.BeatmapHash.Equals(mapHash))
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();

            // map not found means that its not submitted so "not submitted" header gets sent to client and then closed.
            if (_beatmap == null)
            {
                await HttpContext.Response.WriteStringAsync("-1\n0\n \n0");
                await HttpContext.Response.CompleteAsync();
                return;
            }

            _gamemode = byte.Parse(HttpContext.Request.Query["m"]);
            
            _scoresOnMap = await _scoreManager.GetScoresAsync(
                _beatmap.BeatmapHash, _gamemode);

            /*
             `response`:
               ranking status: approved, unranked, ranked, etc..
               online offset: self explanatory
               map title: the thing you see in the beginning of a map
               map rating: the stars you see on top left.
               
               first score: 1|56|6|1|..... user's personal best score. if there is no personal best it's empty/something that isn't valid
               other scores: same thing...... all the other scores that show up on the leaderboard
            */

            sbyte ClientRankedStatus = _beatmap.Beatmapset.RankingStatus switch
			{
				RankingStatus.Graveyard => 0,
				RankingStatus.WIP => 0,
				RankingStatus.Pending => 0,
				RankingStatus.Ranked => 2,
				RankingStatus.Approved => 3,
				_ => 0,
			};

            if (_beatmap.Beatmapset.RankingStatus == RankingStatus.Ranked && _beatmap.OverrideForApproval) {
                ClientRankedStatus = 3;
			}

			await HttpContext.Response.WriteStringAsync(
                $"{ClientRankedStatus}\n" +
                $"{_beatmap.Beatmapset.OnlineOffset}\n" +
                $"{_beatmap.Beatmapset.DisplayedTitle ?? $"[size:20,bold:0]{_beatmap.Beatmapset.Artist}|{_beatmap.Beatmapset.Title}"}\n" +
                $"{_beatmap.Beatmapset.Rating}\n");

            // get personal best score and write it to the content stream, if it doesnt exist just write /n
            // yes it's a pretty hefty query but what can you do
            var personalBestScore = await _scoreManager.GetPersonalBestAsync(
                _beatmap.BeatmapHash, _gamemode, _requestingUser.UserID);

            if (personalBestScore != null)
                await HttpContext.Response.WriteStringAsync(personalBestScore.ToString());
            else
                await HttpContext.Response.WriteStringAsync("\n");
            
            foreach (var score in _scoresOnMap)
                await HttpContext.Response.WriteStringAsync(score.ToString());
            
            await HttpContext.Response.CompleteAsync();
        }
    }
}
