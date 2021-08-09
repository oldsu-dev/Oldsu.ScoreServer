using System.Collections.Generic;
using System.Threading.Tasks;
using Oldsu.Types;

namespace Oldsu.ScoreServer.Managers
{
    interface IScoreManager
    {
        public Task<List<HighScoreWithRank>> GetScoresAsync(string hash, byte gamemode);
        public Task<HighScoreWithRank> GetPersonalBestAsync(string hash, byte gamemode, uint userId);
    }
}