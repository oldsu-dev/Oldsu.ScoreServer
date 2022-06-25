using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oldsu.Utils.Cache;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/preview/images/{id}")]
    public class PreviewImage : ControllerBase
    {
        private static readonly AsyncDictionaryWithExpiration<string> ResponseCache = new();
        
        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            var (actionIsFound, action) = await ResponseCache.TryGetValue(id);

            if (actionIsFound)
                return action as IActionResult;

            await using var db = new Database();
            IActionResult respAction;

            var map = await db.Beatmapsets
                .Where(b => b.BeatmapsetID == int.Parse(id))
                .FirstOrDefaultAsync();

            if (map == null)
                return NotFound();
            
            // map is a bancho map
            if (map.OriginalBeatmapsetID != null)
            {
                respAction = Redirect($"https://b.ppy.sh/thumb/{map.OriginalBeatmapsetID}.jpg");
                
                await ResponseCache.TryAdd(id, respAction, DateTime.Now.AddSeconds(300));
                return respAction;
            }


            // todo oldsu map
            return Ok();
        }
    }
    
    [ApiController]
    [Route("/preview/largeimages/{id}")]
    public class PreviewLargeImage : ControllerBase
    {
        private static readonly AsyncDictionaryWithExpiration<string> ResponseCache = new();
        
        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            var (actionIsFound, action) = await ResponseCache.TryGetValue(id);

            if (actionIsFound)
                return action as IActionResult;

            await using var db = new Database();
            IActionResult respAction;

            var map = await db.Beatmapsets
                .Where(b => b.BeatmapsetID == int.Parse(id))
                .FirstOrDefaultAsync();

            if (map == null)
                return NotFound();
            
            // map is a bancho map
            if (map.OriginalBeatmapsetID != null)
            {
                respAction = Redirect($"https://b.ppy.sh/thumb/{map.OriginalBeatmapsetID}l.jpg");
                
                await ResponseCache.TryAdd(id, respAction, DateTime.Now.AddSeconds(300));
                return respAction;
            }

            // todo oldsu map
            return Ok();
        }
    }
}