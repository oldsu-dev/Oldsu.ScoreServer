using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oldsu.Enums;
using Oldsu.Logging;
using Oldsu.ScoreServer.Managers;
using Oldsu.Types;
using Oldsu.Utils;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    [ApiController]
    [Route("/web/osu-submit-new.php")]
    public class ScoreSubmitNew : ControllerBase // score submission for atleast 2009-2012
    {
        private readonly ILogger<ScoreSubmission> _logger;
        
        public ScoreSubmitNew(ILogger<ScoreSubmission> logger)
        {
            _logger = logger;
        }
        
        [HttpPost]
        public async Task Post()
        {
            await HttpContext.Response.CompleteAsync();
        }
    }
}