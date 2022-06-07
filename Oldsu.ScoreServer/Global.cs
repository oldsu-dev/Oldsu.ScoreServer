using System;
using Oldsu.Logging;
using Oldsu.Logging.Strategies;

namespace Oldsu.ScoreServer
{
    public class Global
    {
        public static LoggingManager LoggingManager = new LoggingManager(new NoLog());

        public const string BanchoBeatmapMirror = "https://chimu.moe/";
        public const string FallbackBanchoBeatmapMirror = "https://hentai.ninja/";

        /// <summary>
        ///     Boolean if replay and screenshot file saving is enabled.
        /// </summary>
        public const bool AllowFileSaving = true;
        
        public const string ReplayFolder = "replays/";
        public const string ScreenshotFolder = "screenshots/";
    }
}