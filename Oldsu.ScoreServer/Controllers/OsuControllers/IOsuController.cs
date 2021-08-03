using System.Threading.Tasks;

namespace Oldsu.ScoreServer.Controllers.OsuControllers
{
    public interface IOsuController
    {
        public Task WriteResponse();
    }
}