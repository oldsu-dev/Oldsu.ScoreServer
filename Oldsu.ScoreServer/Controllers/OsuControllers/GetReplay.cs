using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/web/osu-getreplay.php")]
    public class GetReplay : Controller
    {
        public GetReplay()
        {
            
        }

        [HttpGet]
        public async Task Get()
        {
            if (!int.TryParse(HttpContext.Request.Query["c"].ToString(), out var scoreId))
            {
                await Global.LoggingManager.LogCritical<GetReplay>
                    ($"IP {HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? "unknown"}" +
                     $" tried to get a replay by the unparseable id of {HttpContext.Request.Query["c"].ToString()}");

                await HttpContext.Response.CompleteAsync();
                return;
            }

            var replay = await System.IO.File.ReadAllBytesAsync($"{Global.ReplayFolder}{scoreId}.osr");

            if (replay.Length == 0)
                await HttpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("0"));
            else
                await HttpContext.Response.Body.WriteAsync(replay);

            await HttpContext.Response.CompleteAsync();
        }
    }
}