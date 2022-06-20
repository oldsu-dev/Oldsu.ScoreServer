using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Oldsu.ScoreServer;

public class BanchoInterface
{
    private string _endpoint;
    
    public BanchoInterface(string endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task KickUser(string username)
    {
        using HttpClient httpClient = new HttpClient();

        await httpClient.PostAsJsonAsync($"http://{_endpoint}/api/kickUser", new
        {
            username
        }, new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
    }
}