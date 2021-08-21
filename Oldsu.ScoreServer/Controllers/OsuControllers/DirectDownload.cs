using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/d/{id}")]
    public class DirectDownload : ControllerBase
    {
        private readonly ILogger<ScoreSubmission> _logger;
        
        public DirectDownload(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            // todo set config file

            await using var db = new Database();

            var map = await db.Beatmapsets
                .Where(b => b.BeatmapsetID == int.Parse(id))
                .FirstOrDefaultAsync();

            if (map == null)
                return NotFound();
            
            // map is a bancho map
            if (map.OriginalBeatmapsetID != null)
                return Redirect($"https://chimu.moe/d/{map.OriginalBeatmapsetID}");

            // todo oldsu map
            return Ok();
        }
    }
}