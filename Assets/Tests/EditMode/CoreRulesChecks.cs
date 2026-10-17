using System;
using Nova.Core;

namespace Nova.Core.Tests
{
    // Shared by Unity EditMode (CoreSmokeTests) and the offline .NET runner (Tests/Program.cs).
    public static class CoreRulesChecks
    {
        static void Expect(bool value, string message)
        {
            if (!value) throw new Exception("FAILED: " + message);
        }

        static void Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("FAILED: " + message);
        }

        static GameRules Started()
        {
            var game = new GameRules();
            game.StartGame();
            game.Plunge();
            return game;
        }

        public static void ScoringTable()
        {
            var game = Started();
            Expect(game.Hit(Target.Bumper) == 100, "bumper 100");
            Expect(game.Hit(Target.Slingshot) == 50, "slingshot 50");
            Expect(game.Hit(Target.Lane) == 25, "lane 25");
            Expect(game.Hit(Target.Mission) == 500, "mission 500");
            Expect(game.Score == 675 && game.MissionProgress == 1, "sum and mission progress");
            Expect(new GameRules().Hit(Target.Bumper) == 0, "no score before game starts");
        }

        public static void LaneChainRaisesMultiplier()
        {
            var game = Started();
            game.Hit(Target.Lane); game.Hit(Target.Lane);
            Expect(game.Multiplier == 1 && game.LaneChains == 0, "two lanes do not complete chain");
            game.Hit(Target.Lane);
            Expect(game.LaneChains == 1 && game.Multiplier == 2, "three lanes complete chain");
            Expect(game.Hit(Target.Slingshot) == 100, "multiplier applies to following target");
            for (int i = 0; i < 30; i++) game.Hit(Target.Lane);
            Expect(game.Multiplier == 5, "multiplier capped at 5");
        }

        public static void BallSaveExpires()
        {
            var game = Started();
            game.AdvanceTime(9.9);
            Expect(game.BallSaveActive, "save active before expiry");
            Expect(game.Drain() && game.ActiveBalls == 1 && game.BallsRemaining == 3, "drain inside window is saved");
            game.AdvanceTime(0.1);
            Expect(!game.BallSaveActive, "save expired at boundary");
            Expect(!game.Drain() && game.BallsRemaining == 2 && game.Phase == BallPhase.Ready, "drain after expiry costs ball");
            Throws<ArgumentOutOfRangeException>(() => game.AdvanceTime(-1), "negative time rejected");
            Throws<ArgumentOutOfRangeException>(() => game.AdvanceTime(double.NaN), "NaN time rejected");
        }

        public static void MultiballStartAndEnd()
        {
            var game = Started();
            game.AdvanceTime(20);
            game.StartMultiball();
            Expect(game.Multiball && game.ActiveBalls == 3, "multiball has 3 balls");
            Throws<InvalidOperationException>(() => game.StartMultiball(), "cannot restart multiball");
            game.AdvanceTime(11);
            Expect(!game.Drain() && game.ActiveBalls == 2 && game.Phase == BallPhase.InPlay, "first multiball drain keeps playing");
            Expect(!game.Drain() && game.ActiveBalls == 1 && !game.Multiball, "multiball ends at one ball");
            Expect(!game.Drain() && game.Phase == BallPhase.Ready && game.BallsRemaining == 2, "last ball drain ends turn");
        }

        public static void DrainEdgeCases()
        {
            var game = new GameRules();
            Throws<InvalidOperationException>(() => game.Plunge(), "plunge before game");
            game.StartGame();
            Throws<InvalidOperationException>(() => game.Drain(), "drain before plunge");
            for (int ball = 0; ball < 3; ball++)
            {
                game.Plunge();
                game.Hit(Target.Lane);
                game.AdvanceTime(30);
                game.Drain();
            }
            Expect(game.Phase == BallPhase.Over && game.BallsRemaining == 0, "game over after three balls");
            Expect(game.Multiplier == 1, "multiplier resets on ball loss");
            Throws<InvalidOperationException>(() => game.Plunge(), "plunge after game over");
            Throws<InvalidOperationException>(() => game.Drain(), "drain after game over");
            Expect(game.Hit(Target.Bumper) == 0, "no score after game over");
        }

        public static void RecordBookRoundTrip()
        {
            var book = new RecordBook();
            book.Record("nova-classic", 1000, 12);
            book.Record("nova-classic", 400, 30);
            var loaded = RecordBook.FromJson(book.ToJson());
            Expect(!loaded.Tampered, "clean book not tampered");
            Expect(loaded.Records.Count == 1 && loaded.Records[0].HighScore == 1000
                && loaded.Records[0].LongestMultiballSeconds == 30, "records keep maxima");
            Throws<ArgumentException>(() => book.Record("", 1, 1), "blank table rejected");
            Throws<ArgumentException>(() => book.Record("t", -1, 1), "negative score rejected");
        }

        public static void RecordBookDetectsTamper()
        {
            var book = new RecordBook();
            book.Record("nova-classic", 1000, 12);
            string json = book.ToJson();
            string edited = json.Replace("1000", "9999");
            Expect(edited != json, "tamper edit applied");
            var loaded = RecordBook.FromJson(edited);
            Expect(loaded.Tampered, "edited payload flagged");
            Expect(loaded.Records[0].HighScore == 9999, "tampered data kept for user decision, not deleted");
            Throws<FormatException>(() => RecordBook.FromJson("{\"SchemaVersion\":99,\"Payload\":\"[]\",\"Checksum\":\"x\"}"), "future schema rejected");
            Throws<Exception>(() => RecordBook.FromJson("not json"), "garbage rejected");
            Throws<ArgumentException>(() => RecordBook.FromJson(""), "empty rejected");
        }

        public static void RunAll()
        {
            ScoringTable();
            LaneChainRaisesMultiplier();
            BallSaveExpires();
            MultiballStartAndEnd();
            DrainEdgeCases();
            RecordBookRoundTrip();
            RecordBookDetectsTamper();
        }
    }
}
