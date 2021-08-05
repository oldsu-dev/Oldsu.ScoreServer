using System.Collections.Generic;
using System.Threading.Tasks;
using Oldsu.Types;

namespace Oldsu.ScoreServer.Cache
{
    public class RedisScoreManager : IScoreManager
    {
        public Task<IAsyncEnumerable<HighScoreWithRank>> GetScores(string hash, byte gamemode)
        {
            throw new System.NotImplementedException();
        }

        public Task<HighScoreWithRank> GetPersonalBest(string hash, byte gamemode, uint userId)
        {
            throw new System.NotImplementedException();
        }
    }
}