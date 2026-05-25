using System;
using System.Collections.Generic;
using colonyserver.Assets.UIGeneration;
using colonyshared.NetworkUI;
using colonyshared.NetworkUI.UIGeneration;
using TMPro;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class PlayerToastManager
    {
        private sealed class ToastState
        {
            public string Message;
            public string Color;
            public long ExpiresAtMs;
            public int OffsetY;
            public float Width;
            public float FontSize;
        }

        private const string ToastHudKey = "BetterNecromancy.ToastHud";
        private const long DefaultDurationMs = 4500L;
        private const long UpdateIntervalMs = 250L;

        private static readonly Dictionary<Players.Player, ToastState> ActiveToasts = new Dictionary<Players.Player, ToastState>();
        private static long _nextUpdateAtMs;

        public static void Show(Players.Player player, string message, string color = "#f1e7c8", long durationMs = DefaultDurationMs, int offsetY = -118, float width = 980f, float fontSize = 16f)
        {
            if (!PlayerUiGuard.CanSendStable(player) || string.IsNullOrWhiteSpace(message))
                return;

            var now = Pipliz.Time.MillisecondsSinceStart;
            ActiveToasts[player] = new ToastState
            {
                Message = message,
                Color = string.IsNullOrWhiteSpace(color) ? "#f1e7c8" : color,
                ExpiresAtMs = now + Math.Max(750L, durationMs),
                OffsetY = offsetY,
                Width = width,
                FontSize = fontSize
            };

            Render(player, ActiveToasts[player]);
        }

        public static void Broadcast(string message, string color = "#f1e7c8", long durationMs = DefaultDurationMs, int offsetY = -118, float width = 980f, float fontSize = 16f)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player))
                    Show(player, message, color, durationMs, offsetY, width, fontSize);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".PlayerToastManager.OnUpdate")]
        public static void OnUpdate()
        {
            var now = Pipliz.Time.MillisecondsSinceStart;
            if (now < _nextUpdateAtMs)
                return;

            _nextUpdateAtMs = now + UpdateIntervalMs;
            if (ActiveToasts.Count == 0)
                return;

            var toRemove = new List<Players.Player>();
            foreach (var pair in ActiveToasts)
            {
                var player = pair.Key;
                var toast = pair.Value;

                if (!PlayerUiGuard.CanSendStable(player) || toast == null || now >= toast.ExpiresAtMs)
                {
                    if (player != null)
                        UIManager.RemoveUILabel(ToastHudKey, player);

                    toRemove.Add(player);
                    continue;
                }

                Render(player, toast);
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                if (toRemove[i] != null)
                    ActiveToasts.Remove(toRemove[i]);
            }
        }

        private static void Render(Players.Player player, ToastState toast)
        {
            if (!PlayerUiGuard.CanSendStable(player) || toast == null)
                return;

            UIManager.AddorUpdateUILabel(
                ToastHudKey,
                UIElementDisplayType.Global,
                toast.Message,
                new Pipliz.Vector3Int(0, toast.OffsetY, 0),
                AnchorPresets.TopCenter,
                toast.Width,
                player,
                toast.FontSize,
                FontType.DidactGothic,
                toast.Color,
                TextAlignmentOptions.Center);
        }
    }
}
