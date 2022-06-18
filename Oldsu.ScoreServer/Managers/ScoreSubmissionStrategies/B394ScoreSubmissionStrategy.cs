using Oldsu.Types;

namespace Oldsu.ScoreServer.Managers.ScoreSubmissionStrategies
{
    public class B904ScoreSubmissionStrategy : IScoreSubmissionStrategy 
    {
        public bool ValidateScoreChecksum(ScoreRow scoreRow)
        {
            return true;
        }

        public string GetScorePanelString((HighScoreWithRank, HighScoreWithRank) comparableScores, (Stats, Stats) comparableUsers,
            Stats nextUser)
        {
            var (score, _) = comparableScores;

            if (nextUser == null)
                return $"{score.Rank}\n0\nfruitplatter.png";
            else
                return $"{score.Rank}\n{nextUser.RankedScore}\nfruitplatter.png";
        }
    }
}