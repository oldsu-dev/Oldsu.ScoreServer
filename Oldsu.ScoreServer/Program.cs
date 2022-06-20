using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oldsu.Logging;
using Oldsu.Logging.Strategies;

namespace Oldsu.ScoreServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (Global.AllowFileSaving)
            {
                Directory.CreateDirectory(Global.ReplayFolder);
                Directory.CreateDirectory(Global.ScreenshotFolder);   
            }

            if (Environment.GetEnvironmentVariable("OLDSU_MONGO_DB_CONNECTION_STRING") != null)
            {
                Global.LoggingManager= new LoggingManager(new MongoDbWriter(
                    Environment.GetEnvironmentVariable("OLDSU_MONGO_DB_CONNECTION_STRING")!));
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
