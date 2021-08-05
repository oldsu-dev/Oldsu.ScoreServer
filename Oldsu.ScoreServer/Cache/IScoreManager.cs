using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Oldsu.Types;

namespace Oldsu.ScoreServer.Cache
{
    interface IScoreManager
    {
        public Task<IAsyncEnumerable<HighScoreWithRank>> GetScores(string hash, byte gamemode);
        public Task<HighScoreWithRank> GetPersonalBest(string hash, byte gamemode, uint userId);
    }
}