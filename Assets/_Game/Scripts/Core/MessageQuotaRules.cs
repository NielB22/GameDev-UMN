namespace CyberWatch.Core
{
    /// <summary>
    /// The hourly message target, in ONE place: used by the quota check during the shift and by the
    /// "Cara Bermain" rules text, so what the player is told is always exactly what happens.
    /// </summary>
    public static class MessageQuotaRules
    {
        /// <summary>Messages the player must have answered by each hour change.</summary>
        public static int PerHour(GameDifficulty difficulty) =>
            difficulty == GameDifficulty.LemburTerakhir ? 5 : 4;

        /// <summary>Threat added for every message still missing at an hour change.</summary>
        public static int PenaltyPerMissing(GameDifficulty difficulty) =>
            difficulty == GameDifficulty.LemburTerakhir ? 10 : 8;

        public static int PerHour() => PerHour(GameManager.Instance.Difficulty);

        public static int PenaltyPerMissing() => PenaltyPerMissing(GameManager.Instance.Difficulty);
    }
}
