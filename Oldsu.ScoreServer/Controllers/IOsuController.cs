using System.Threading.Tasks;

namespace Oldsu.ScoreServer.Controllers
{
    public interface IOsuController
    {
        public Task<string> GetResponse();
    }
}