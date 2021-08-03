using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/web/osu-submit-modular.php")]
    public class ScoreSubmission : ControllerBase, IOsuController
    {
        private readonly ILogger<ScoreSubmission> _logger;

        public ScoreSubmission(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            // TODO
            return "0";
        }

        public async Task WriteResponse()
        {
            
        }
    }
}