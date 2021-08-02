using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oldsu.Types;

namespace Oldsu.ScoreServer.Controllers
{
    [ApiController]
    [Route("/web/osu-getscores6.php")]
    public class GetScores : ControllerBase, IOsuController
    {
        /// <summary>
        ///     Contains a cache of total ranks in a score in the leaderboard.
        ///     The field is going to be incremented, when a score gets submitted on the map.
        /// </summary>
        public static ConcurrentDictionary<(int, byte), int> TotalLeaderboardRankCache = new ();

        private readonly ILogger<ScoreSubmission> _logger;

        private User _requestingUser;
        private Beatmap _beatmap;
        private byte _gamemode;
        
        private IAsyncEnumerable<ScoreRow> _scoresOnMap;

        public GetScores(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task Get()
        {
            var db = new Database();
            
            var userId = HttpContext.Request.Query["u"].ToString();
            
            _requestingUser = await db.Users
                .FindAsync(uint.Parse(userId));

            // the requesting user is not found from the database, end the request
            if (_requestingUser == null)
            {
                await HttpContext.Response.WriteStringAsync("unknown requesting user");
                await HttpContext.Response.CompleteAsync();
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
            
            if (!TotalLeaderboardRankCache.TryGetValue((_beatmap.BeatmapID, _gamemode), out _))
            {
                var allScores = db.Scores
                    .Where(s => s.BeatmapHash.Equals(mapHash) && s.Gamemode.Equals(_gamemode))
                    .Include(s => s.User)
                    .OrderByDescending(s => s.Score)
                    .Distinct();

                TotalLeaderboardRankCache.TryAdd((_beatmap.BeatmapID, _gamemode), allScores.Count());

                _scoresOnMap = allScores
                    .AsAsyncEnumerable();
            }
            else
            {
                _scoresOnMap = db.Scores
                    .Where(s => s.BeatmapHash.Equals(mapHash) && s.Gamemode.Equals(_gamemode))
                    .Include(s => s.User)
                    .OrderByDescending(s => s.Score)
                    .Distinct()
                    .AsAsyncEnumerable();
            }

            await WriteResponse();
        }

        public async Task WriteResponse()
        {
            /*
             `response`:
               ranking status: approved, unranked, ranked, etc..
               online offset: self explanatory
               map title: the thing you see in the beginning of a map
               map rating: the stars you see on top left.
               
               first score: 1|56|6|1|..... user's personal best score. if there is no personal best it's empty/something that isn't valid
               other scores: same thing...... all the other scores that show up on the leaderboard
            */
            
            await HttpContext.Response.WriteStringAsync(
                $"{_beatmap.Beatmapset.RankingStatus}\n0\n \n{_beatmap.Rating}\n");

            var scoresAlreadyAdded = new HashSet<string>();
            
            var db = new Database();
            
            // get personal best score and write it to the content stream, if it doesnt exist just write /n
            // yes it's a pretty hefty query but what can you do
            var personalBestScore = await db.Scores
                .Where(s => s.BeatmapHash.Equals(_beatmap.BeatmapHash) &&
                            s.Gamemode.Equals(_gamemode) &&
                            s.UserId.Equals(_requestingUser.UserID))
                .Include(s => s.User)
                .OrderByDescending(s => s.Score)
                .GroupBy(s => s.UserId)
                .Select(g => g.FirstOrDefault())
                .FirstOrDefaultAsync();

            if (personalBestScore != null)
                await HttpContext.Response.WriteStringAsync(personalBestScore.ToString());
            else
                await HttpContext.Response.WriteStringAsync("\n");
            
            if (TotalLeaderboardRankCache.TryGetValue((_beatmap.BeatmapID, _gamemode), out var totalScores))
            {
                await foreach (var score in _scoresOnMap)
                    await HttpContext.Response.WriteStringAsync(score.ToString());

                for (int i = 50; i < totalScores; i++)
                    await HttpContext.Response.WriteStringAsync($"0|0|0|0|0|0|0|0|0|0|0|0|0|0|{i}|0");
            }
        }
    }
}