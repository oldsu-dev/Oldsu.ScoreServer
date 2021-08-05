using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Oldsu.Types;

namespace Oldsu.ScoreServer.Cache
{
    interface IScoreManager
    {
        public Task<List<HighScoreWithRank>> GetScoresAsync(string hash, byte gamemode);
        public Task<HighScoreWithRank> GetPersonalBestAsync(string hash, byte gamemode, uint userId);
    }
}