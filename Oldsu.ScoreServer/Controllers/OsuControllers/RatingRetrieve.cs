using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oldsu.Types;
using Oldsu.Enums;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/rating/ingame-rate.php")]
    public class RatingRetrieve : Controller
    {
        public RatingRetrieve()
        {
            
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await using var db = new Database();
            
            var beatmapHash = HttpContext.Request.Query["c"];

            var user = await db.AuthenticateAsync(
                HttpContext.Request.Query["u"], HttpContext.Request.Query["p"]);
            
            var beatmap = await db.Beatmaps
                .Where(b => b.BeatmapHash.Equals(beatmapHash))
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();
                
            if (user == null)
                return Content("auth fail");
            
            if (beatmap == null)
                return Content("no exist");
            
            if (beatmap.Beatmapset.RankingStatus is not RankingStatus.Ranked or RankingStatus.Approved)
                return Content("not ranked");
            
            if ((beatmap.Beatmapset.CreatorID ?? 0) == user.UserID)
                return Content($"owner\n{beatmap.Beatmapset.Rating:1}");

            var hasRated = await db.Ratings
                .Where(r => r.UserID == user.UserID &&
                            r.BeatmapsetID == beatmap.BeatmapsetID)
                .FirstOrDefaultAsync() != null;

            if (hasRated)
                return Content($"alreadyvoted\n{beatmap.Beatmapset.Rating:0.00}");

            return Content($"ok\n{beatmap.Beatmapset.Rating:0.00}");
        }
    }
}
