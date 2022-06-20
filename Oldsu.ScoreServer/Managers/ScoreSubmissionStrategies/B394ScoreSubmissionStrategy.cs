using Oldsu.Types;
using Oldsu.Utils.Crypto;

namespace Oldsu.ScoreServer.Managers.ScoreSubmissionStrategies
{
    public class B904ScoreSubmissionStrategy : IScoreSubmissionStrategy 
    {
        public bool ValidateScoreChecksum(string username, ScoreRow scoreRow)
        {
            string checksum = MD5Hash.Compute(string.Format("{0}o14{1}{2}s{3}{4}u{5}{6}{7}{8}{9}{10}{11}{12}{13}{14:yyMMddHHmmss}", 
                 scoreRow.Hit100 + scoreRow.Hit300, 
                 scoreRow.Hit50,
                 scoreRow.HitGeki, 
                 scoreRow.HitKatu, 
                 scoreRow.HitMiss, 
                 scoreRow.Beatmap.BeatmapHash, 
                 scoreRow.MaxCombo, 
                 scoreRow.Perfect,
                 username, scoreRow.Score, 
                 scoreRow.Grade, 
                 (int)scoreRow.Mods, 
                 scoreRow.Passed, 
                 (int)scoreRow.Gamemode, 
                 scoreRow.SubmittedAt.ToUniversalTime()));

            return scoreRow.SubmitHash == checksum;
        }

        public string GetScorePanelString((HighScoreWithRank, HighScoreWithRank) comparableScores, (Stats, Stats) comparableUsers,
            Stats nextUser)
        {
            var (score, _) = comparableScores;

            if (nextUser == null)
                return $"{score.Rank}\n0\n";
            else
                return $"{score.Rank}\n{nextUser.RankedScore}\n";
        }
    }
}