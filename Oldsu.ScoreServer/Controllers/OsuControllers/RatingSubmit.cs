using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oldsu.Types;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/rating/submit-rate.php")]
    public class SubmitRating : Controller
    {
        public SubmitRating()
        {
            
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var db = new Database();
            
            var beatmapHash = HttpContext.Request.Query["c"];
            var ratingValue = HttpContext.Request.Query["v"];
            
            var user = await db.AuthenticateAsync(
                HttpContext.Request.Query["u"], HttpContext.Request.Query["p"]);
            
            var beatmap = await db.Beatmaps
                .Where(b => b.BeatmapHash.Equals(beatmapHash))
                .Include(b => b.Beatmapset)
                .FirstOrDefaultAsync();
                
            if (user == null || beatmap == null)
                return Content("no");
            
            if (beatmap.Beatmapset.RankingStatus == 0)
                return Content("no");
            
            if ((beatmap.Beatmapset.CreatorID ?? 0) == user.UserID)
                return Content("no");

            var hasRated = await db.Ratings
                .Where(r => r.UserID == user.UserID &&
                            r.BeatmapsetID == beatmap.BeatmapsetID)
                .FirstOrDefaultAsync() != null;

            if (hasRated)
                return Content($"alreadyvoted\n{beatmap.Beatmapset.Rating:1}");

            var allRatings = db.Ratings
                .Where(r => r.BeatmapsetID == beatmap.BeatmapsetID)
                .AsAsyncEnumerable();
            
            var ratingRow = new Rating
            {
                UserID = user.UserID,
                BeatmapsetID = beatmap.BeatmapsetID,
                Rate = float.Parse(ratingValue.ToString())
            };

            db.Add(ratingRow);
            
            var sumOfRatings = 0f;

            await foreach (var rating in allRatings)
                sumOfRatings += rating.Rate;

            sumOfRatings += float.Parse(ratingValue.ToString());
            
            var newAverage = sumOfRatings / (beatmap.Beatmapset.RatingCount + 1);

            beatmap.Beatmapset.Rating = newAverage;
            beatmap.Beatmapset.RatingCount++;

            await db.SaveChangesAsync();

            return Content($"{newAverage}");
        }
    }
}