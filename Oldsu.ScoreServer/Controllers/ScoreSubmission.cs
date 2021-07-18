using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Oldsu.ScoreServer.Controllers
{
    [ApiController]
    [Route("/web/osu-submit-modular.php")]
    public class ScoreSubmission : ControllerBase
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
    }
}