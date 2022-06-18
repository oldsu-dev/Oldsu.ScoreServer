using Oldsu.Types;

namespace Oldsu.ScoreServer.Managers.ScoreSubmissionStrategies
{
    // the interface name is kind of misleading bc it does things like validate the score check sum too
    public interface IScoreSubmissionStrategy
    {
        public bool ValidateScoreChecksum(ScoreRow scoreRow);
        public string GetScorePanelString((HighScoreWithRank, HighScoreWithRank) comparableScores,
                                          (Stats, Stats) comparableUsers,
                                          Stats nextUser);
    }
}