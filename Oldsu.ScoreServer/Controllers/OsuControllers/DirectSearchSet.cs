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
    [Route("/web/osu-search-set.php")]
    public class DirectSearchSet : ControllerBase
    {
        private readonly ILogger<ScoreSubmission> _logger;
        
        public DirectSearchSet(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task Get()
        {
            var requestQuery = HttpContext.Request.Query["q"].ToString();

            IAsyncEnumerable<Beatmapset> mapCollections;

            string k, v;

            await using var db = new Database();

            IQueryable<Beatmap> mapQuery = db.Beatmaps;

            if (HttpContext.Request.Query.ContainsKey("s"))
                mapQuery = mapQuery
                    .Where(b => b.BeatmapsetID == int.Parse(HttpContext.Request.Query["s"].ToString()));
            else if (HttpContext.Request.Query.ContainsKey("b"))
                mapQuery = mapQuery
                    .Where(b => b.BeatmapID == int.Parse(HttpContext.Request.Query["b"].ToString()));
            else
            {
                await HttpContext.Response.CompleteAsync();
                return;
            }

            var map = await mapQuery
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();

            if (map == null)
            {
                await HttpContext.Response.CompleteAsync();
                return;
            }

            await HttpContext.Response.WriteStringAsync($"\n{map.BeatmapsetID}.osz|{map.Beatmapset.Artist}|{map.Beatmapset.Title}|{map.Beatmapset.CreatorName}|" +
                                                        $"{map.Beatmapset.RankingStatus}|{map.Beatmapset.Rating}|0|{map.BeatmapsetID}|" +
                                                        $"0|0|0|0|0|");
            
            await HttpContext.Response.CompleteAsync();
        }
    }
}