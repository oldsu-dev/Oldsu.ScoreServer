using System.Collections.Generic;
using System.Threading.Tasks;
using Oldsu.Types;

namespace Oldsu.ScoreServer.Managers
{
    public class RedisScoreManager : IScoreManager
    {
        public Task<List<HighScoreWithRank>> GetScoresAsync(string hash, byte gamemode)
        {
            throw new System.NotImplementedException();
        }

        public Task<HighScoreWithRank> GetPersonalBestAsync(string hash, byte gamemode, uint userId)
        {
            throw new System.NotImplementedException();
        }
    }
}