using System;

namespace Nova.Core
{
    public enum BallPhase { Ready, InPlay, Over }
    public enum Target { Bumper, Slingshot, Lane, Mission }

    // Rules consume deterministic elapsed time; Unity's physics adapter owns hit/drain events.
    public sealed class GameRules
    {
        const int StartingBalls = 3;
        const double SaveSeconds = 10;
        double now;
        double saveUntil;
        int lanes;

        public long Score { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public int LaneChains { get; private set; }
        public int MissionProgress { get; private set; }
        public int BallsRemaining { get; private set; }
        public int ActiveBalls { get; private set; }
        public BallPhase Phase { get; private set; } = BallPhase.Over;
        public bool Multiball => ActiveBalls > 1;
        public bool BallSaveActive => Phase == BallPhase.InPlay && now < saveUntil;

        public void StartGame()
        {
            Score = 0;
            Multiplier = 1;
            LaneChains = MissionProgress = lanes = ActiveBalls = 0;
            BallsRemaining = StartingBalls;
            now = saveUntil = 0;
            Phase = BallPhase.Ready;
        }

        public void Plunge()
        {
            if (Phase != BallPhase.Ready || BallsRemaining == 0)
                throw new InvalidOperationException("No ball ready to plunge");
            ActiveBalls = 1;
            saveUntil = now + SaveSeconds;
            Phase = BallPhase.InPlay;
        }

        public void AdvanceTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || now + seconds == double.PositiveInfinity)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            now += seconds;
        }

        public long Hit(Target target)
        {
            if (Phase != BallPhase.InPlay) return 0;
            int points;
            switch (target)
            {
                case Target.Bumper: points = 100; break;
                case Target.Slingshot: points = 50; break;
                case Target.Lane: points = 25; break;
                case Target.Mission: points = 500; break;
                default: throw new ArgumentOutOfRangeException(nameof(target));
            }
            long awarded = checked((long)points * Multiplier);
            Score = checked(Score + awarded);
            if (target == Target.Lane)
            {
                if (++lanes == 3)
                {
                    lanes = 0;
                    LaneChains++;
                    Multiplier = Math.Min(5, Multiplier + 1);
                }
            }
            if (target == Target.Mission) MissionProgress++;
            return awarded;
        }

        public void StartMultiball()
        {
            if (Phase != BallPhase.InPlay || ActiveBalls != 1)
                throw new InvalidOperationException("Multiball requires one live ball");
            ActiveBalls = 3;
            saveUntil = now + SaveSeconds;
        }

        // Returns true when physics must respawn a saved ball.
        public bool Drain()
        {
            if (Phase != BallPhase.InPlay || ActiveBalls == 0)
                throw new InvalidOperationException("No live ball to drain");
            ActiveBalls--;
            if (BallSaveActive)
            {
                ActiveBalls++;
                return true;
            }
            if (ActiveBalls > 0) return false;
            BallsRemaining--;
            Multiplier = 1;
            lanes = 0;
            Phase = BallsRemaining == 0 ? BallPhase.Over : BallPhase.Ready;
            return false;
        }
    }
}
