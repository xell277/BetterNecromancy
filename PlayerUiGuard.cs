namespace BetterNecromancy
{
    internal static class PlayerUiGuard
    {
        private const long StableJoinGraceMs = 5000L;
        private static readonly System.Collections.Generic.Dictionary<int, long> ReadySinceByPlayer = new System.Collections.Generic.Dictionary<int, long>();

        public static bool CanSend(Players.Player player)
        {
            return player != null &&
                   player.IsConnectionReady &&
                   player.ConnectionState.ToString() == "Connected" &&
                   player.ActiveColony != null;
        }

        public static bool CanSendStable(Players.Player player)
        {
            if (!CanSend(player))
            {
                Forget(player);
                return false;
            }

            var playerKey = GetPlayerKey(player);
            if (playerKey < 0)
                return false;

            var now = Pipliz.Time.MillisecondsSinceStart;
            if (!ReadySinceByPlayer.TryGetValue(playerKey, out var readySinceAtMs))
            {
                ReadySinceByPlayer[playerKey] = now;
                return false;
            }

            return now - readySinceAtMs >= StableJoinGraceMs;
        }

        public static bool CanBroadcastWorldEffects()
        {
            var hasReadyPlayer = false;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (player == null)
                    continue;

                if (!CanSendStable(player))
                    return false;

                hasReadyPlayer = true;
            }

            return hasReadyPlayer;
        }

        public static bool ShouldDeferPlayerFacingEffects()
        {
            foreach (var player in Players.ConnectedPlayers)
            {
                if (player != null && !CanSendStable(player))
                    return true;
            }

            return false;
        }

        private static int GetPlayerKey(Players.Player player)
        {
            return player != null ? player.ID.ID.ID : -1;
        }

        private static void Forget(Players.Player player)
        {
            var playerKey = GetPlayerKey(player);
            if (playerKey >= 0)
                ReadySinceByPlayer.Remove(playerKey);
        }
    }
}
