using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Oldsu.Types;
using Oldsu.Utils.Cache;

namespace Oldsu.ScoreServer.Managers
{
    public class NativeScoreManager : IScoreManager
    {
        private readonly AsyncDictionaryWithExpiration<(string, byte)> _mapLeaderboardCache = new ();
        private readonly AsyncDictionaryWithExpiration<(string, byte, uint)> _personalBestCache = new ();

        public async Task<List<HighScoreWithRank>> GetScoresAsync(string hash, byte gamemode)
        {
            var (isFound, value) = await _mapLeaderboardCache.TryGetValue((hash, gamemode));

            if (isFound)
            {
                return value as List<HighScoreWithRank>;
            }
            else
            {
                await using var db = new Database();
                
                var scoresOnMap = await db.HighScoresWithRank
                    .Where(s => s.BeatmapHash.Equals(hash) &&
                                s.Gamemode.Equals(gamemode) &&
                                s.Passed)
                    .Include(s => s.User)
                    .OrderByDescending(s => s.Score)
                    .Take(50)
                    .AsQueryable()
                    .ToListAsync();

                await AddScoresIntoCache(hash, gamemode, scoresOnMap);

                return scoresOnMap;
            }
        }

        public async Task<HighScoreWithRank> GetPersonalBestAsync(string hash, byte gamemode, uint userId)
        {
            var (isFound, value) = await _personalBestCache.TryGetValue((hash, gamemode, userId));

            if (isFound)
            {
                return value as HighScoreWithRank;
            }
            else
            {
                await using var db = new Database();
                
                var personalBestScore = await db.HighScoresWithRank
                    .Where(s => s.BeatmapHash.Equals(hash) &&
                                s.Gamemode.Equals(gamemode) &&
                                s.UserId.Equals(userId) &&
                                s.Passed)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync();

                await AddPersonalBestScoreIntoCache(hash, gamemode, userId, personalBestScore);

                return personalBestScore;
            }
        }

        private async Task AddScoresIntoCache(string hash, byte gamemode, List<HighScoreWithRank> scores) 
            => await _mapLeaderboardCache.TryAdd((hash, gamemode), scores, DateTime.Now.AddSeconds(20));

        private async Task AddPersonalBestScoreIntoCache(string hash, byte gamemode, uint userId, HighScoreWithRank score)
            => await _personalBestCache.TryAdd((hash, gamemode, userId), score, DateTime.Now.AddSeconds(5));
    }
}