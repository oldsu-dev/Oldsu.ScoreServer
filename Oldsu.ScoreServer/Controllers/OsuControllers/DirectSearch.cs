using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Oldsu.Types;

using DbFunctions = System.Data.Entity.DbFunctions;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/web/osu-search.php")]
    public class DirectSearch : ControllerBase
    {
        private readonly ILogger<ScoreSubmission> _logger;
        
        public DirectSearch(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task Get()
        {
            var requestQuery = HttpContext.Request.Query["q"].ToString();

            IAsyncEnumerable<Beatmapset> mapCollections;

            await using var db = new Database();

            // todo add more query settings like ranked=2007, sr=3, ar=7 etc..
            mapCollections = requestQuery switch
            {
                "Newest" => db.Beatmapsets.OrderByDescending(b => b.RankingStatus)
                    .Take(100)
                    .AsQueryable()
                    .AsAsyncEnumerable(),
                "Top Rated" => db.Beatmapsets.OrderByDescending(b => b.Rating)
                    .Take(100)
                    .AsQueryable()
                    .AsAsyncEnumerable(),
                _ => db.Beatmapsets
                    .FromSqlRaw(
                        "SELECT * FROM Beatmapsets WHERE Title LIKE CONCAT('%', {0} '%') or Artist LIKE CONCAT('%', {0}, '%') or CreatorName LIKE CONCAT('%', {0}, '%')",
                        requestQuery)
                    .OrderByDescending(b => b.Rating)
                    .Take(100)
                    .AsQueryable()
                    .AsAsyncEnumerable()
            };

            var stringBuilder = new StringBuilder("5435345");
            
            await foreach (var map in mapCollections)
                stringBuilder.Append($"\n{map.BeatmapsetID}.osz|{map.Artist}|{map.Title}|{map.CreatorName}|" +
                                     $"{map.RankingStatus}|{map.Rating}|{map.SubmittedAt}|{map.BeatmapsetID}|" +
                                     $"0|0|0|0|0|");

            await HttpContext.Response.WriteStringAsync(stringBuilder.ToString());
            await HttpContext.Response.CompleteAsync();
        }
    }
}