﻿using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Oldsu.Enums;
using Oldsu.ScoreServer.Managers.ScoreSubmissionStrategies;
using Oldsu.Types;
using Oldsu.Utils;

namespace Oldsu.ScoreServer.Managers
{
    public class ScoreSubmitManager
    {
        public const string BannedMessage = "error: ban";
        public const string WrongPasswordMessage = "error: pass";
        public const string ErrorMessage = "error: no";

        private IScoreSubmissionStrategy _scoreSubmissionStrategy;

        private ScoreRow _score;
        private Beatmap? _beatmap;
        private UserInfo _user;

        public ScoreSubmitManager(UserInfo user, ScoreRow score, Beatmap? beatmap)
        {
            _user = user;
            _score = score;
            _beatmap = beatmap;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>if a suitable version was found</returns>
        public bool SetStrategy()
        {
            _scoreSubmissionStrategy = _score.Version switch
            { // change to enums
                "2100" => new B904ScoreSubmissionStrategy(),
                _ => null,
            };

            return _scoreSubmissionStrategy != null;
        }

        //return value submittable and error string and ban reason
        public (bool, string?, string?) ValidateScore()
        {
            var mods = (Mod)_score.Mods;
            
            if ((((mods & Mod.DoubleTime) == Mod.DoubleTime && (mods & Mod.HalfTime) == Mod.HalfTime)) ||
                ((mods & Mod.HardRock) == Mod.HardRock && (mods & Mod.Easy) == Mod.Easy))
            {
                return (false, BannedMessage, "Invalid mods.");
            }
            
            if (((mods & Mod.Relax) == Mod.Relax) || ((mods & Mod.Autoplay) == Mod.Autoplay) ||
                ((mods & Mod.Relax2) == Mod.Relax2))
            {
                return (false, null, null);
            }
            
            if (!_scoreSubmissionStrategy.ValidateScoreChecksum(_user.Username, _score))
                return (false, BannedMessage, "Invalid score checksum.");
                
            //todo check score

            return (true, null, null);
        }

        public string GetScorePanelString((HighScoreWithRank, HighScoreWithRank) comparableScores,
            (Stats, Stats) comparableUsers,
            Stats nextUser) =>
            _scoreSubmissionStrategy.GetScorePanelString(comparableScores,
                comparableUsers,
                nextUser);

        public async Task SubmitScore()
        {
            await using var db = new Database();

            _score.User = null;
            
            db.Scores.Add(_score);

            await db.SaveChangesAsync();
        }

        public void UpdateStats(Stats userStats, HighScoreWithRank? oldScore)
        {
            userStats.UpdateStats(_score);
            
            if (_score.Passed)
                if ((_beatmap?.Beatmapset.RankingStatus == RankingStatus.Ranked) && (!_beatmap.OverrideForApproval))
                    if (oldScore != null && _score.Score > oldScore.Score)
                        userStats.RankedScore += _score.Score - oldScore.Score;
                    else if (oldScore == null)
                        userStats.RankedScore += _score.Score;
        }
    }
}