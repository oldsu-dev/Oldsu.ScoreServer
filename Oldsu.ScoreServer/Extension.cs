using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Oldsu.ScoreServer
{
    public static class Extension
    {
        /// <summary>
        ///     Writes a string into a http response.
        /// </summary>
        public static async Task WriteStringAsync(this HttpResponse httpResponse, string input)
            => await httpResponse.Body.WriteAsync(Encoding.UTF8.GetBytes(input));
    }
}