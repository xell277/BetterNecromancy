using System;
using System.Collections.Generic;
using Chatting;
using colonyserver.Assets.UIGeneration;
using colonyshared.NetworkUI;
using colonyshared.NetworkUI.UIGeneration;
using NPC;
using TMPro;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class WorldEventManager
    {
        private enum ActiveWorldEvent
        {
            None,
            BloodMoon,
            PlagueFog,
            ArcaneStorm,
            HordeAssault
        }

        private enum ActiveAftermath
        {
            None,
            SanguineResidue,
            PlagueSpores,
            ArcaneCharge,
            WarDrums
        }

        private sealed class ColonyVictoryBlessingRuntime
        {
            public double EndsAt;
            public readonly HashSet<NPCID> AppliedFollowers = new HashSet<NPCID>();
            public readonly Dictionary<NPCID, long> LastScaledQueueSource = new Dictionary<NPCID, long>();
        }

        private sealed class AftermathKillRewardDefinition
        {
            public string ItemKey;
            public int MinAmount;
            public int MaxAmount;
            public float Weight;
        }

        private const string EventHudKey = "BetterNecromancy.WorldEventHud";
        private const string EventOverlayTopKey = "BetterNecromancy.WorldEventOverlay.Top";
        private const string EventOverlayLeftKey = "BetterNecromancy.WorldEventOverlay.Left";
        private const string EventOverlayRightKey = "BetterNecromancy.WorldEventOverlay.Right";
        private const string BloodMoonOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.BloodMoon";
        private const string PlagueFogOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.PlagueFog";
        private const string ArcaneStormOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.ArcaneStorm";
        private const string HordeOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.Horde";
        private const string BloodMoonAftermathOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.BloodMoonAftermath";
        private const string PlagueFogAftermathOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.PlagueFogAftermath";
        private const string ArcaneStormAftermathOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.ArcaneStormAftermath";
        private const string HordeAftermathOverlayImagePrefix = ModEntry.Namespace + ".EventOverlay.HordeAftermath";
        private const double EventMinIntervalSeconds = 900d;
        private const double EventMaxIntervalSeconds = 1500d;
        private const double EventDurationSeconds = 480d;
        private const double AftermathDurationSeconds = 420d;
        private const double AftermathFxIntervalSeconds = 11d;
        private const double AftermathRewardMessageCooldownSeconds = 7d;
        private const double WeatherLockCheckIntervalSeconds = 1d;
        private const double AmbientAudioCheckIntervalSeconds = 1d;
        private const float ColonyVictorySpeedMultiplier = 2f;
        private const float ColonyVictoryWorkSpeedMultiplier = 2f;
        private const int EventAmbientLoopLengthMs = 20000;
        private const string BloodMoonAmbientCollection = "bn_event_bloodmoon_ambient";
        private const string BloodMoonAftermathAmbientCollection = "bn_event_bloodmoon_aftermath_ambient";
        private const string PlagueFogAmbientCollection = "bn_event_plaguefog_ambient";
        private const string PlagueFogAftermathAmbientCollection = "bn_event_plaguefog_aftermath_ambient";
        private const string ArcaneStormAmbientCollection = "bn_event_arcanestorm_ambient";
        private const string ArcaneStormAftermathAmbientCollection = "bn_event_arcanestorm_aftermath_ambient";
        private const string HordeAmbientCollection = "bn_event_horde_ambient";
        private const string HordeAftermathAmbientCollection = "bn_event_horde_aftermath_ambient";
        private static ActiveWorldEvent _currentEvent = ActiveWorldEvent.None;
        private static ActiveAftermath _currentAftermath = ActiveAftermath.None;
        private static readonly Dictionary<Colony, ColonyVictoryBlessingRuntime> ColonyVictoryBlessings = new Dictionary<Colony, ColonyVictoryBlessingRuntime>();
        private static readonly Dictionary<Colony, double> AftermathRewardMessageCooldowns = new Dictionary<Colony, double>();
        private static readonly Dictionary<int, AudioManager.AudioClipPlayingID> AmbientLoopIds = new Dictionary<int, AudioManager.AudioClipPlayingID>();
        private static readonly HashSet<int> MissingWeatherWarnedPlayers = new HashSet<int>();
        private static bool _weatherSyncEnabled = true;
        private static string _activeAmbientAudioCollection;
        private static double _eventEndsAt = double.MaxValue;
        private static double _aftermathEndsAt = double.MaxValue;
        private static double _nextRollAt = double.MaxValue;
        private static double _nextUpdateAt = double.MaxValue;
        private static double _nextAftermathFxAt = double.MaxValue;
        private static double _nextWeatherLockCheckAt = double.MaxValue;
        private static double _nextAmbientAudioCheckAt = double.MaxValue;
        private static string _lastAppliedCloudTintTheme;
        private static bool _cloudTintSyncHealthy = true;

        public static bool HasActiveEvent => _currentEvent != ActiveWorldEvent.None && Pipliz.Time.SecondsSinceStartDouble < _eventEndsAt;
        public static bool HasActiveAftermath => _currentAftermath != ActiveAftermath.None && Pipliz.Time.SecondsSinceStartDouble < _aftermathEndsAt;
        public static bool WeatherSyncEnabled => _weatherSyncEnabled;
        public static bool IsHordeAssaultActive => HasActiveEvent && _currentEvent == ActiveWorldEvent.HordeAssault;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerConnectedLate, BetterNecromancy.ModEntry.Namespace + ".WorldEventManager.OnPlayerConnectedLate")]
        public static void OnPlayerConnectedLate(Players.Player player)
        {
            if (!PlayerUiGuard.CanSendStable(player))
                return;

            UpdateOverlayForPlayer(player);
        }

        public static string GetActiveEventId()
        {
            switch (_currentEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return "bloodmoon";
                case ActiveWorldEvent.PlagueFog:
                    return "plaguefog";
                case ActiveWorldEvent.ArcaneStorm:
                    return "arcanestorm";
                case ActiveWorldEvent.HordeAssault:
                    return "horde";
                default:
                    return "none";
            }
        }

        public static string GetEliteThemeId()
        {
            if (HasActiveEvent)
                return GetActiveEventId();

            switch (_currentAftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    return "bloodmoon";
                case ActiveAftermath.PlagueSpores:
                    return "plaguefog";
                case ActiveAftermath.ArcaneCharge:
                    return "arcanestorm";
                case ActiveAftermath.WarDrums:
                    return "horde";
                default:
                    return "none";
            }
        }

        public static float GetEliteThemeScale()
        {
            if (HasActiveEvent)
                return 1f;

            return HasActiveAftermath ? 0.55f : 0f;
        }

        public static string GetStatusText()
        {
            if (!HasActiveEvent && !HasActiveAftermath)
            {
                var nextIn = _nextRollAt == double.MaxValue
                    ? -1
                    : UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt((float)(_nextRollAt - Pipliz.Time.SecondsSinceStartDouble)));
                var blessingText = GetColonyBlessingStatusText();

                return nextIn >= 0
                    ? "No world event active. Next event roll in about " + nextIn + "s. Weather sync: " + GetWeatherSyncLabel() + "." + blessingText
                    : "No world event active." + blessingText;
            }

            if (_currentEvent == ActiveWorldEvent.HordeAssault)
                return HordeEventManager.GetStatusText() + " Weather sync: " + GetWeatherSyncLabel() + ".";

            if (HasActiveEvent)
            {
                var remaining = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt((float)(_eventEndsAt - Pipliz.Time.SecondsSinceStartDouble)));
                return GetEventDisplayName(_currentEvent) + " active for about " + remaining + "s. " + GetEventGameplaySummary(_currentEvent) + " Weather sync: " + GetWeatherSyncLabel() + ".";
            }

            var aftermathRemaining = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt((float)(_aftermathEndsAt - Pipliz.Time.SecondsSinceStartDouble)));
            return "Aftermath: " + GetAftermathDisplayName(_currentAftermath) + " for about " + aftermathRemaining + "s. " + GetAftermathGameplaySummary(_currentAftermath) + " Weather sync: " + GetWeatherSyncLabel() + ".";
        }

        public static void TryGrantAftermathKillReward(Colony colony)
        {
            colony = ResolvePrimaryEventColony(colony);
            if (!HasActiveAftermath || !IsPrimaryEventColony(colony))
                return;

            if (!TryRollAftermathKillReward(_currentAftermath, out var itemIndex, out var amount, out var displayName))
                return;

            colony.ColonyGroup.Stockpile.Add(itemIndex, amount);
            colony.ColonyGroup.Stockpile.SendToOwners();

            var now = Pipliz.Time.SecondsSinceStartDouble;
            if (AftermathRewardMessageCooldowns.TryGetValue(colony, out var nextAllowedAt) && now < nextAllowedAt)
                return;

            AftermathRewardMessageCooldowns[colony] = now + AftermathRewardMessageCooldownSeconds;
            NotifyOwners(colony, GetAftermathDisplayName(_currentAftermath) + " leaves behind " + amount + " " + displayName + " in the colony stockpile.");
        }

        public static bool TryStartEvent(string name, out string message)
        {
            return TryStartEvent(name, null, out message);
        }

        public static bool TryStartEvent(string name, Players.Player initiator, out string message)
        {
            if (!TryParseEvent(name, out var worldEvent))
            {
                message = "Unknown event. Use bloodmoon, plaguefog, arcanestorm, horde or clear.";
                return false;
            }

            if (!TryStartResolvedEvent(worldEvent, initiator, announce: true, out var failureMessage))
            {
                message = failureMessage;
                return false;
            }

            message = worldEvent == ActiveWorldEvent.HordeAssault
                ? "Started world event: " + GetEventDisplayName(worldEvent) + ". " + HordeEventManager.GetStatusText()
                : "Started world event: " + GetEventDisplayName(worldEvent) + ".";
            return true;
        }

        public static bool TryClearEvent(out string message)
        {
            if (!HasActiveEvent && !HasActiveAftermath)
            {
                message = "No active world event or aftermath to clear.";
                return false;
            }

            if (!HasActiveEvent && HasActiveAftermath)
            {
                var clearedAftermathName = GetAftermathDisplayName(_currentAftermath);
                ClearAftermath();
                message = "Cleared event aftermath: " + clearedAftermathName + ".";
                return true;
            }

            var endedName = GetEventDisplayName(_currentEvent);
            EndCurrentEvent(announce: false, grantRewards: false);
            message = "Cleared world event: " + endedName + ".";
            return true;
        }

        public static string GetWeatherSyncStatusText(Players.Player player)
        {
            if (!BetterWeatherBridge.IsAvailable)
                return _weatherSyncEnabled
                    ? "Event weather sync: BetterWeather not detected. Events still work without synced weather. Install BetterWeather or use /bnmagic weather off."
                    : "Event weather sync: off. BetterWeather not detected. Events will continue without synced weather.";

            var syncText = _weatherSyncEnabled ? "on" : "off";
            return "Event weather sync: " + syncText + ". BetterWeather: " + BetterWeatherBridge.TryGetStatusText(player);
        }

        public static void SetWeatherSyncEnabled(bool enabled)
        {
            _weatherSyncEnabled = enabled;
            _nextWeatherLockCheckAt = 0d;
            MissingWeatherWarnedPlayers.Clear();

            if (!_weatherSyncEnabled)
            {
                if (BetterWeatherBridge.IsAvailable)
                {
                    BetterWeatherBridge.TryClearVisualTheme();
                    ResetCloudTintTheme();
                    BetterWeatherBridge.TryForceWeather("Clear");
                }
                return;
            }

            ApplyCurrentWeatherState();
        }

        public static float GetColonyPointMultiplier()
        {
            switch (_currentEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return 1.5f;
                case ActiveWorldEvent.PlagueFog:
                    return 1.75f;
                case ActiveWorldEvent.ArcaneStorm:
                    return 1.2f;
                case ActiveWorldEvent.HordeAssault:
                    return 1.35f;
                default:
                    switch (_currentAftermath)
                    {
                        case ActiveAftermath.SanguineResidue:
                            return 1.12f;
                        case ActiveAftermath.WarDrums:
                            return 1.18f;
                        default:
                            return 1f;
                    }
            }
        }

        public static float GetMonsterDamageTakenMultiplier()
        {
            switch (_currentEvent)
            {
                case ActiveWorldEvent.PlagueFog:
                    return 0.82f;
                case ActiveWorldEvent.BloodMoon:
                    return 0.92f;
                case ActiveWorldEvent.HordeAssault:
                    return 0.9f;
                default:
                    switch (_currentAftermath)
                    {
                        case ActiveAftermath.PlagueSpores:
                            return 0.94f;
                        case ActiveAftermath.WarDrums:
                            return 0.96f;
                        default:
                            return 1f;
                    }
            }
        }

        public static float GetMonsterDamageDealtMultiplier()
        {
            switch (_currentEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return 1.25f;
                case ActiveWorldEvent.PlagueFog:
                    return 1.1f;
                case ActiveWorldEvent.HordeAssault:
                    return 1.15f;
                default:
                    switch (_currentAftermath)
                    {
                        case ActiveAftermath.SanguineResidue:
                            return 1.06f;
                        case ActiveAftermath.WarDrums:
                            return 1.08f;
                        default:
                            return 1f;
                    }
            }
        }

        public static float GetMagicSpellDamageMultiplier()
        {
            if (_currentEvent == ActiveWorldEvent.ArcaneStorm)
                return 1.18f;

            return _currentAftermath == ActiveAftermath.ArcaneCharge ? 1.08f : 1f;
        }

        public static int GetMagicCooldownReductionMsBonus()
        {
            if (_currentEvent == ActiveWorldEvent.ArcaneStorm)
                return 100;

            return _currentAftermath == ActiveAftermath.ArcaneCharge ? 45 : 0;
        }

        public static int GetMagicManaCostReductionBonus(int baseCost)
        {
            if (_currentEvent == ActiveWorldEvent.ArcaneStorm)
                return baseCost >= 3 ? 1 : 0;

            return _currentAftermath == ActiveAftermath.ArcaneCharge && baseCost >= 5 ? 1 : 0;
        }

        public static int GetManaRegenIntervalReductionMsBonus()
        {
            if (_currentEvent == ActiveWorldEvent.ArcaneStorm)
                return 350;

            return _currentAftermath == ActiveAftermath.ArcaneCharge ? 160 : 0;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, BetterNecromancy.ModEntry.Namespace + ".WorldEventManager.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            _currentEvent = ActiveWorldEvent.None;
            _currentAftermath = ActiveAftermath.None;
            ColonyVictoryBlessings.Clear();
            AftermathRewardMessageCooldowns.Clear();
            _eventEndsAt = double.MaxValue;
            _aftermathEndsAt = double.MaxValue;
            _nextUpdateAt = double.MaxValue;
            _nextAftermathFxAt = double.MaxValue;
            _nextWeatherLockCheckAt = double.MaxValue;
            _nextAmbientAudioCheckAt = double.MaxValue;
            _lastAppliedCloudTintTheme = null;
            _cloudTintSyncHealthy = true;
            StopAmbientAudioLoops();
            ScheduleNextRoll();
            ResetCloudTintTheme(forceSmoke: true);
            ClearHudForAll();
            ClearOverlayForAll();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".WorldEventManager.OnUpdate")]
        public static void OnUpdate()
        {
            if (!World.Initialized)
                return;

            var now = Pipliz.Time.SecondsSinceStartDouble;
            if (_nextRollAt == double.MaxValue)
                ScheduleNextRoll();

            MaintainWeatherLock(now);
            NotifyMissingWeatherForConnectedPlayers();
            MaintainAmbientAudio(now);

            if (_currentEvent == ActiveWorldEvent.None && now >= _nextRollAt)
                TryStartRandomEvent();

            if (now < _nextUpdateAt)
                return;

            _nextUpdateAt = now + 1d;
            UpdateColonyVictoryBlessings(now);
            UpdateOverlayForAll();

            if (!HasActiveEvent)
            {
                if (HasActiveAftermath)
                {
                    if (now >= _aftermathEndsAt)
                    {
                        var endedAftermath = _currentAftermath;
                        ClearAftermath();
                        Broadcast("Aftermath faded: " + GetAftermathDisplayName(endedAftermath) + ".");
                    }
                    else
                    {
                        TryPlayAftermathFx(now);
                        UpdateHudForAll(now);
                    }
                }
                else
                {
                    ClearHudForAll();
                    ClearOverlayForAll();
                }

                return;
            }

            if (_currentEvent == ActiveWorldEvent.HordeAssault)
            {
                ClearHudForAll();

                if (!HordeEventManager.IsActive)
                    EndCurrentEvent(announce: true, grantRewards: false);

                return;
            }

            if (now >= _eventEndsAt)
            {
                EndCurrentEvent(announce: true, grantRewards: true);
                return;
            }

            UpdateHudForAll(now);
        }

        private static bool TryStartResolvedEvent(ActiveWorldEvent worldEvent, Players.Player initiator, bool announce, out string failureMessage)
        {
            failureMessage = null;

            if (worldEvent != ActiveWorldEvent.HordeAssault)
            {
                if (initiator != null && !CanPlayerTriggerMainColonyEvent(initiator, out failureMessage))
                    return false;

                if (initiator == null && !HasAnyPrimaryEventColony())
                {
                    failureMessage = "No main colony with a banner and stockpile is available for world events right now.";
                    return false;
                }
            }

            if (_currentEvent == ActiveWorldEvent.HordeAssault && worldEvent != ActiveWorldEvent.HordeAssault)
                HordeEventManager.Clear();

            if (worldEvent == ActiveWorldEvent.HordeAssault)
            {
                var started = initiator != null
                    ? HordeEventManager.TryStartForPlayer(initiator, out failureMessage)
                    : HordeEventManager.TryStartAutomatic(out failureMessage);

                if (!started)
                    return false;
            }

            _currentEvent = worldEvent;
            ClearAftermath();
            _eventEndsAt = worldEvent == ActiveWorldEvent.HordeAssault
                ? double.MaxValue
                : Pipliz.Time.SecondsSinceStartDouble + EventDurationSeconds;
            _nextUpdateAt = 0d;
            _nextWeatherLockCheckAt = 0d;
            ApplyCurrentWeatherState();
            RefreshAmbientAudio(nowOverride: Pipliz.Time.SecondsSinceStartDouble);

            if (announce)
                Broadcast("World Event: " + GetEventDisplayName(worldEvent) + ". " + GetEventStartMessage(worldEvent));

            return true;
        }

        private static void EndCurrentEvent(bool announce, bool grantRewards)
        {
            var endedEvent = _currentEvent;
            _currentEvent = ActiveWorldEvent.None;
            _eventEndsAt = double.MaxValue;
            _nextUpdateAt = 0d;
            _nextWeatherLockCheckAt = 0d;
            if (endedEvent == ActiveWorldEvent.HordeAssault)
                HordeEventManager.Clear();
            else if (grantRewards)
                GrantWorldEventRewards(endedEvent);
            if (announce && endedEvent != ActiveWorldEvent.None)
            {
                StartAftermath(endedEvent);
                GrantColonyVictoryBlessings(endedEvent);
            }
            ScheduleNextRoll();
            ClearHudForAll();
            ApplyCurrentWeatherState();
            ResetCloudTintTheme(forceSmoke: true);
            RefreshAmbientAudio(nowOverride: Pipliz.Time.SecondsSinceStartDouble);

            if (announce && endedEvent != ActiveWorldEvent.None)
                Broadcast("World Event ended: " + GetEventDisplayName(endedEvent) + ".");
        }

        private static void GrantWorldEventRewards(ActiveWorldEvent worldEvent)
        {
            if (worldEvent == ActiveWorldEvent.None || worldEvent == ActiveWorldEvent.HordeAssault)
                return;

            var eventId = GetEventRewardId(worldEvent);
            if (string.IsNullOrEmpty(eventId))
                return;

            var eventName = GetEventDisplayName(worldEvent);
            var rewardedColonies = 0;
            var processedColonies = new HashSet<ColonyID>();
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();

            while (colonies.MoveNext())
            {
                var colony = ResolvePrimaryEventColony(colonies.Current);
                if (!IsPrimaryEventColony(colony) || !processedColonies.Add(colony.ColonyID))
                    continue;

                if (!EventRewardManager.GrantToColony(colony, eventId, eventName))
                    continue;

                rewardedColonies++;
            }

            if (rewardedColonies > 0)
                Broadcast(eventName + " leaves behind themed rewards in " + rewardedColonies + " colony stockpile(s).");
        }

        private static void ScheduleNextRoll()
        {
            var delay = EventMaxIntervalSeconds <= EventMinIntervalSeconds
                ? EventMinIntervalSeconds
                : Pipliz.Random.Next((int)EventMinIntervalSeconds, (int)EventMaxIntervalSeconds + 1);

            _nextRollAt = Pipliz.Time.SecondsSinceStartDouble + delay;
        }

        private static ActiveWorldEvent GetRandomEvent()
        {
            switch (Pipliz.Random.Next(0, 5))
            {
                case 0:
                    return ActiveWorldEvent.HordeAssault;
                case 1:
                    return ActiveWorldEvent.BloodMoon;
                case 2:
                    return ActiveWorldEvent.PlagueFog;
                default:
                    return ActiveWorldEvent.ArcaneStorm;
            }
        }

        private static void TryStartRandomEvent()
        {
            var selectedEvent = GetRandomEvent();
            if (TryStartResolvedEvent(selectedEvent, null, announce: true, out _))
                return;

            var fallbacks = new[] { ActiveWorldEvent.BloodMoon, ActiveWorldEvent.PlagueFog, ActiveWorldEvent.ArcaneStorm };
            var startIndex = Pipliz.Random.Next(0, fallbacks.Length);

            for (var i = 0; i < fallbacks.Length; i++)
            {
                var candidate = fallbacks[(startIndex + i) % fallbacks.Length];
                if (TryStartResolvedEvent(candidate, null, announce: true, out _))
                    return;
            }

            ScheduleNextRoll();
        }

        private static bool TryParseEvent(string name, out ActiveWorldEvent worldEvent)
        {
            switch ((name ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "bloodmoon":
                case "blood":
                    worldEvent = ActiveWorldEvent.BloodMoon;
                    return true;
                case "plaguefog":
                case "plague":
                case "fog":
                    worldEvent = ActiveWorldEvent.PlagueFog;
                    return true;
                case "arcanestorm":
                case "arcane":
                case "storm":
                    worldEvent = ActiveWorldEvent.ArcaneStorm;
                    return true;
                case "horde":
                case "hordeassault":
                case "assault":
                    worldEvent = ActiveWorldEvent.HordeAssault;
                    return true;
                default:
                    worldEvent = ActiveWorldEvent.None;
                    return false;
            }
        }

        private static string GetEventDisplayName(ActiveWorldEvent worldEvent)
        {
            switch (worldEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return "Blood Moon";
                case ActiveWorldEvent.PlagueFog:
                    return "Plague Fog";
                case ActiveWorldEvent.ArcaneStorm:
                    return "Arcane Storm";
                case ActiveWorldEvent.HordeAssault:
                    return "Horde Assault";
                default:
                    return "None";
            }
        }

        private static string GetEventRewardId(ActiveWorldEvent worldEvent)
        {
            switch (worldEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return "BloodMoon";
                case ActiveWorldEvent.PlagueFog:
                    return "PlagueFog";
                case ActiveWorldEvent.ArcaneStorm:
                    return "ArcaneStorm";
                case ActiveWorldEvent.HordeAssault:
                    return "HordeAssault";
                default:
                    return string.Empty;
            }
        }

        private static string GetEventGameplaySummary(ActiveWorldEvent worldEvent)
        {
            switch (worldEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return "Zombies hit harder, resist a little more, Bloodbound elites drain life and kills grant bonus colony points.";
                case ActiveWorldEvent.PlagueFog:
                    return "Zombies are tougher to finish, Plagueborn elites rot nearby defenders and kills grant heavy bonus colony points.";
                case ActiveWorldEvent.ArcaneStorm:
                    return "Magic hits harder, cools faster, costs less mana, regen speeds up and Stormtouched elites call lightning.";
                case ActiveWorldEvent.HordeAssault:
                    return "A full horde is assaulting a colony. Warbound elites rally nearby undead, reinforcements arrive as the wave weakens, and the boss bar tracks group strength instead of boss HP.";
                default:
                    return string.Empty;
            }
        }

        private static string GetEventStartMessage(ActiveWorldEvent worldEvent)
        {
            switch (worldEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return "The dead are enraged. Monster damage is up, Bloodbound elites start feeding on nearby defenders and kills now reward more colony points.";
                case ActiveWorldEvent.PlagueFog:
                    return "A sick green fog crawls in. Zombies are harder to put down, Plagueborn elites spread rot and the rewards are richer.";
                case ActiveWorldEvent.ArcaneStorm:
                    return "Arcane pressure surges through the world. Wands hit harder, recover faster, drink less mana and Stormtouched elites begin striking from above.";
                case ActiveWorldEvent.HordeAssault:
                    return "A full horde is on the march. Watch the strength bar, survive the reinforcements, bring down the Warbound elites and earn a colony reward when the wave breaks.";
                default:
                    return string.Empty;
            }
        }

        private static string GetEventHudColor(ActiveWorldEvent worldEvent)
        {
            switch (worldEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    return "#ff8a8a";
                case ActiveWorldEvent.PlagueFog:
                    return "#c7ef98";
                case ActiveWorldEvent.ArcaneStorm:
                    return "#a7dbff";
                case ActiveWorldEvent.HordeAssault:
                    return "#ffd3a1";
                default:
                    return "#ffffff";
            }
        }

        private static string GetAftermathDisplayName(ActiveAftermath aftermath)
        {
            switch (aftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    return "Sanguine Residue";
                case ActiveAftermath.PlagueSpores:
                    return "Plague Spores";
                case ActiveAftermath.ArcaneCharge:
                    return "Arcane Charge";
                case ActiveAftermath.WarDrums:
                    return "War Drums";
                default:
                    return "None";
            }
        }

        private static string GetAftermathGameplaySummary(ActiveAftermath aftermath)
        {
            switch (aftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    return "Bloodbound elites still stalk colonies, kills keep granting a small colony-point bonus and the fallen may leave blood-soaked spoils.";
                case ActiveAftermath.PlagueSpores:
                    return "Plagueborn elites still surface, the dead remain slightly harder to put down and infected remains can be reclaimed.";
                case ActiveAftermath.ArcaneCharge:
                    return "Magic retains a lighter arcane buff: quicker regen, cheaper casts, a little more spell damage and storm residues in the stockpile.";
                case ActiveAftermath.WarDrums:
                    return "Warbound elites linger after the assault, colony-point gains stay elevated and the battlefield keeps yielding salvage.";
                default:
                    return string.Empty;
            }
        }

        private static string GetAftermathHudColor(ActiveAftermath aftermath)
        {
            switch (aftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    return "#ffb0b0";
                case ActiveAftermath.PlagueSpores:
                    return "#d6f3b0";
                case ActiveAftermath.ArcaneCharge:
                    return "#c7e7ff";
                case ActiveAftermath.WarDrums:
                    return "#ffe2b5";
                default:
                    return "#ffffff";
            }
        }

        private static string GetWeatherSyncLabel()
        {
            if (!BetterWeatherBridge.IsAvailable)
                return _weatherSyncEnabled ? "BetterWeather missing" : "off (BetterWeather missing)";

            return _weatherSyncEnabled ? "on" : "off";
        }

        private static void ApplyCurrentWeatherState()
        {
            if (!_weatherSyncEnabled || !BetterWeatherBridge.IsAvailable)
                return;

            var desiredVisualTheme = GetDesiredVisualThemeName();
            BetterWeatherBridge.TrySetEnabled(true);
            if (string.IsNullOrEmpty(desiredVisualTheme))
                BetterWeatherBridge.TryClearVisualTheme();
            else
                BetterWeatherBridge.TrySetVisualTheme(desiredVisualTheme);
            SyncCloudTintTheme();
            BetterWeatherBridge.TryForceWeather(GetDesiredWeatherKindName(includeClearFallback: true));
            _nextWeatherLockCheckAt = Pipliz.Time.SecondsSinceStartDouble + WeatherLockCheckIntervalSeconds;
        }

        private static string GetDesiredVisualThemeName()
        {
            if (HasActiveEvent)
            {
                switch (_currentEvent)
                {
                    case ActiveWorldEvent.BloodMoon:
                        return "BloodMoon";
                    case ActiveWorldEvent.PlagueFog:
                        return "PlagueFog";
                    case ActiveWorldEvent.ArcaneStorm:
                        return "ArcaneStorm";
                    case ActiveWorldEvent.HordeAssault:
                        return "Horde";
                }
            }

            if (HasActiveAftermath)
            {
                switch (_currentAftermath)
                {
                    case ActiveAftermath.SanguineResidue:
                        return "BloodMoonAftermath";
                    case ActiveAftermath.PlagueSpores:
                        return "PlagueFogAftermath";
                    case ActiveAftermath.ArcaneCharge:
                        return "ArcaneStormAftermath";
                    case ActiveAftermath.WarDrums:
                        return "HordeAftermath";
                }
            }

            return null;
        }

        private static string GetDesiredCloudTintThemeName()
        {
            if (HasActiveEvent)
            {
                switch (_currentEvent)
                {
                    case ActiveWorldEvent.BloodMoon:
                        return "EventRed";
                    case ActiveWorldEvent.PlagueFog:
                        return "EventGreen";
                    case ActiveWorldEvent.ArcaneStorm:
                        return "EventPurple";
                }
            }

            return null;
        }

        private static ActiveWorldEvent MapAftermathToWeatherEvent(ActiveAftermath aftermath)
        {
            switch (aftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    return ActiveWorldEvent.BloodMoon;
                case ActiveAftermath.PlagueSpores:
                    return ActiveWorldEvent.PlagueFog;
                case ActiveAftermath.ArcaneCharge:
                    return ActiveWorldEvent.ArcaneStorm;
                case ActiveAftermath.WarDrums:
                    return ActiveWorldEvent.HordeAssault;
                default:
                    return ActiveWorldEvent.None;
            }
        }

        private static void StartAftermath(ActiveWorldEvent endedEvent)
        {
            switch (endedEvent)
            {
                case ActiveWorldEvent.BloodMoon:
                    _currentAftermath = ActiveAftermath.SanguineResidue;
                    break;
                case ActiveWorldEvent.PlagueFog:
                    _currentAftermath = ActiveAftermath.PlagueSpores;
                    break;
                case ActiveWorldEvent.ArcaneStorm:
                    _currentAftermath = ActiveAftermath.ArcaneCharge;
                    break;
                case ActiveWorldEvent.HordeAssault:
                    _currentAftermath = ActiveAftermath.WarDrums;
                    break;
                default:
                    _currentAftermath = ActiveAftermath.None;
                    _aftermathEndsAt = double.MaxValue;
                    return;
            }

            _aftermathEndsAt = Pipliz.Time.SecondsSinceStartDouble + AftermathDurationSeconds;
            _nextAftermathFxAt = Pipliz.Time.SecondsSinceStartDouble + 1.25d;
            RefreshAmbientAudio(nowOverride: Pipliz.Time.SecondsSinceStartDouble);
            Broadcast("Aftermath: " + GetAftermathDisplayName(_currentAftermath) + ". " + GetAftermathGameplaySummary(_currentAftermath));
        }

        private static void GrantColonyVictoryBlessings(ActiveWorldEvent endedEvent)
        {
            if (endedEvent == ActiveWorldEvent.None)
                return;

            var now = Pipliz.Time.SecondsSinceStartDouble;
            var durationSeconds = GetColonyVictoryBlessingDurationSeconds();

            if (endedEvent == ActiveWorldEvent.HordeAssault)
            {
                var hordeColony = HordeEventManager.LastCompletedColony;
                if (hordeColony != null)
                    GrantColonyVictoryBlessing(hordeColony, durationSeconds, "Horde Assault");

                return;
            }

            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            var processedColonies = new HashSet<ColonyID>();
            while (colonies.MoveNext())
            {
                var colony = ResolvePrimaryEventColony(colonies.Current);
                if (!IsPrimaryEventColony(colony) || !processedColonies.Add(colony.ColonyID) || colony.FollowerCount <= 0)
                    continue;

                GrantColonyVictoryBlessing(colony, durationSeconds, GetEventDisplayName(endedEvent), now);
            }
        }

        private static void GrantColonyVictoryBlessing(Colony colony, double durationSeconds, string sourceName, double? nowOverride = null)
        {
            colony = ResolvePrimaryEventColony(colony);
            if (!IsPrimaryEventColony(colony) || durationSeconds <= 0d)
                return;

            if (!ColonyVictoryBlessings.TryGetValue(colony, out var blessing))
            {
                blessing = new ColonyVictoryBlessingRuntime();
                ColonyVictoryBlessings[colony] = blessing;
            }

            var livingFollowers = 0;
            foreach (var follower in colony.Followers)
            {
                if (follower == null || !follower.IsValid || follower.health <= 0f)
                    continue;

                ApplyVictoryBlessingToFollower(follower, blessing, nowOverride);
                livingFollowers++;
            }

            if (livingFollowers <= 0)
                return;

            var now = nowOverride ?? Pipliz.Time.SecondsSinceStartDouble;
            blessing.EndsAt = System.Math.Max(blessing.EndsAt, now + durationSeconds);

            var durationText = FormatDuration(durationSeconds);
            NotifyOwners(colony, sourceName + " aftermath empowers the colony: colonists now move and work at x2 speed for about " + durationText + ".");
        }

        private static void UpdateColonyVictoryBlessings(double now)
        {
            if (ColonyVictoryBlessings.Count == 0)
                return;

            var expired = default(List<Colony>);
            foreach (var pair in ColonyVictoryBlessings)
            {
                var colony = pair.Key;
                var blessing = pair.Value;
                if (!IsPrimaryEventColony(colony))
                {
                    if (expired == null)
                        expired = new List<Colony>();

                    expired.Add(colony);
                    continue;
                }

                if (blessing == null || now >= blessing.EndsAt)
                {
                    ResetColonyVictoryBlessing(colony);

                    if (expired == null)
                        expired = new List<Colony>();

                    expired.Add(colony);
                    continue;
                }

                foreach (var follower in colony.Followers)
                {
                    if (follower == null || !follower.IsValid || follower.health <= 0f)
                        continue;

                    ApplyVictoryBlessingToFollower(follower, blessing, now);
                }
            }

            if (expired == null)
                return;

            for (var i = 0; i < expired.Count; i++)
                ColonyVictoryBlessings.Remove(expired[i]);
        }

        private static void ResetColonyVictoryBlessing(Colony colony)
        {
            if (colony == null)
                return;

            foreach (var follower in colony.Followers)
            {
                if (follower != null && follower.IsValid)
                    RestoreFollowerVictoryBlessing(follower);
            }

            NotifyOwners(colony, "The colony's event haste fades and your colonists return to their normal pace.");
        }

        private static void ClearAftermath()
        {
            _currentAftermath = ActiveAftermath.None;
            _aftermathEndsAt = double.MaxValue;
            _nextAftermathFxAt = double.MaxValue;
            _nextWeatherLockCheckAt = 0d;
            AftermathRewardMessageCooldowns.Clear();
            ApplyCurrentWeatherState();
            ResetCloudTintTheme(forceSmoke: true);
            RefreshAmbientAudio(nowOverride: Pipliz.Time.SecondsSinceStartDouble);
        }

        private static void MaintainWeatherLock(double now)
        {
            if (!_weatherSyncEnabled || !BetterWeatherBridge.IsAvailable)
            {
                _nextWeatherLockCheckAt = double.MaxValue;
                return;
            }

            var desiredWeather = GetDesiredWeatherKindName(includeClearFallback: false);
            var desiredVisualTheme = GetDesiredVisualThemeName();
            if (string.IsNullOrEmpty(desiredWeather))
            {
                BetterWeatherBridge.TryClearVisualTheme();
                ResetCloudTintTheme();
                _nextWeatherLockCheckAt = double.MaxValue;
                return;
            }

            if (now < _nextWeatherLockCheckAt)
                return;

            _nextWeatherLockCheckAt = now + WeatherLockCheckIntervalSeconds;

            var resolved = BetterWeatherBridge.TryGetCurrentWeatherKind(out var currentWeather, out var enabled);
            if (!enabled)
                BetterWeatherBridge.TrySetEnabled(true);

            if (string.IsNullOrEmpty(desiredVisualTheme))
                BetterWeatherBridge.TryClearVisualTheme();
            else
                BetterWeatherBridge.TrySetVisualTheme(desiredVisualTheme);

            if (!resolved || !string.Equals(currentWeather, desiredWeather, StringComparison.OrdinalIgnoreCase))
                BetterWeatherBridge.TryForceWeather(desiredWeather);
        }

        private static void SyncCloudTintTheme()
        {
            if (!BetterWeatherBridge.IsAvailable || !_cloudTintSyncHealthy)
                return;

            var desiredTheme = GetDesiredCloudTintThemeName();
            if (string.Equals(_lastAppliedCloudTintTheme, desiredTheme, StringComparison.OrdinalIgnoreCase))
                return;

            if (string.IsNullOrEmpty(desiredTheme))
            {
                ResetCloudTintTheme();
                return;
            }

            if (BetterWeatherBridge.TrySetCloudTintTheme(desiredTheme))
            {
                _lastAppliedCloudTintTheme = desiredTheme;
                return;
            }

            _cloudTintSyncHealthy = false;
        }

        private static void ResetCloudTintTheme(bool forceSmoke = false)
        {
            if (!BetterWeatherBridge.IsAvailable)
            {
                _lastAppliedCloudTintTheme = null;
                return;
            }

            if (!_cloudTintSyncHealthy)
            {
                _lastAppliedCloudTintTheme = null;
                return;
            }

            if (forceSmoke || !string.IsNullOrEmpty(_lastAppliedCloudTintTheme))
            {
                if (!BetterWeatherBridge.TrySetCloudTintTheme("Smoke"))
                    _cloudTintSyncHealthy = false;
            }

            _lastAppliedCloudTintTheme = null;
        }

        private static void NotifyMissingWeatherForConnectedPlayers()
        {
            if (!_weatherSyncEnabled || BetterWeatherBridge.IsAvailable || (!HasActiveEvent && !HasActiveAftermath))
                return;

            const string message = "BetterWeather was not detected. BetterNecromancy world events still work, but weather syncing is unavailable. Install BetterWeather or use /bnmagic weather off.";

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                var playerKey = GetPlayerKey(player);
                if (playerKey < 0 || MissingWeatherWarnedPlayers.Contains(playerKey))
                    continue;

                PlayerToastManager.Show(player, message, "#f0dcb4", 5200L);
                MissingWeatherWarnedPlayers.Add(playerKey);
            }
        }

        private static void MaintainAmbientAudio(double now)
        {
            if (now < _nextAmbientAudioCheckAt)
                return;

            _nextAmbientAudioCheckAt = now + AmbientAudioCheckIntervalSeconds;
            RefreshAmbientAudio(now);
        }

        private static void RefreshAmbientAudio(double nowOverride)
        {
            var desiredCollection = GetDesiredAmbientAudioCollection();
            if (string.IsNullOrEmpty(desiredCollection))
            {
                StopAmbientAudioLoops();
                _activeAmbientAudioCollection = null;
                return;
            }

            if (!string.Equals(_activeAmbientAudioCollection, desiredCollection, StringComparison.OrdinalIgnoreCase))
            {
                StopAmbientAudioLoops();
                _activeAmbientAudioCollection = null;
            }

            if (EnsureAmbientLoopForConnectedPlayers(desiredCollection, EventAmbientLoopLengthMs))
                _activeAmbientAudioCollection = desiredCollection;

            _nextAmbientAudioCheckAt = nowOverride + AmbientAudioCheckIntervalSeconds;
        }

        private static string GetDesiredAmbientAudioCollection()
        {
            if (HasActiveEvent)
            {
                switch (_currentEvent)
                {
                    case ActiveWorldEvent.BloodMoon:
                        return BloodMoonAmbientCollection;
                    case ActiveWorldEvent.PlagueFog:
                        return PlagueFogAmbientCollection;
                    case ActiveWorldEvent.ArcaneStorm:
                        return ArcaneStormAmbientCollection;
                    case ActiveWorldEvent.HordeAssault:
                        return HordeAmbientCollection;
                }
            }

            if (HasActiveAftermath)
            {
                switch (_currentAftermath)
                {
                    case ActiveAftermath.SanguineResidue:
                        return BloodMoonAftermathAmbientCollection;
                    case ActiveAftermath.PlagueSpores:
                        return PlagueFogAftermathAmbientCollection;
                    case ActiveAftermath.ArcaneCharge:
                        return ArcaneStormAftermathAmbientCollection;
                    case ActiveAftermath.WarDrums:
                        return HordeAftermathAmbientCollection;
                }
            }

            return null;
        }

        private static bool EnsureAmbientLoopForConnectedPlayers(string audioCollection, int loopLengthMilliseconds)
        {
            if (string.IsNullOrEmpty(audioCollection))
                return false;

            if (!AudioManager.TryGetIndex(audioCollection, out AudioManager.AudioClipIndex clipIndex))
                return false;

            var anySent = false;
            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                var playerKey = GetPlayerKey(player);
                if (AmbientLoopIds.ContainsKey(playerKey))
                {
                    anySent = true;
                    continue;
                }

                try
                {
                    var playingId = AudioManager.AudioClipPlayingID.GenerateNew();
                    AudioManager.SendPlayLoopPacket(
                        player,
                        player.PositionStanding,
                        clipIndex,
                        playingId,
                        0,
                        System.Math.Max(1000, loopLengthMilliseconds));
                    AmbientLoopIds[playerKey] = playingId;
                    anySent = true;
                }
                catch
                {
                }
            }

            return anySent;
        }

        private static void StopAmbientAudioLoops()
        {
            if (AmbientLoopIds.Count == 0 || string.IsNullOrEmpty(_activeAmbientAudioCollection))
            {
                AmbientLoopIds.Clear();
                return;
            }

            if (!AudioManager.TryGetIndex(_activeAmbientAudioCollection, out AudioManager.AudioClipIndex clipIndex))
            {
                AmbientLoopIds.Clear();
                return;
            }

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                var playerKey = GetPlayerKey(player);
                if (!AmbientLoopIds.TryGetValue(playerKey, out var playingId))
                    continue;

                try
                {
                    AudioManager.Stop(player.PositionStanding, clipIndex, playingId);
                }
                catch
                {
                }
            }

            AmbientLoopIds.Clear();
        }

        private static int GetPlayerKey(Players.Player player)
        {
            return player != null ? player.ID.ID.ID : -1;
        }

        private static string GetDesiredWeatherKindName(bool includeClearFallback)
        {
            switch (HasActiveEvent ? _currentEvent : MapAftermathToWeatherEvent(_currentAftermath))
            {
                case ActiveWorldEvent.BloodMoon:
                    return "Rain";
                case ActiveWorldEvent.PlagueFog:
                    return "Fog";
                case ActiveWorldEvent.ArcaneStorm:
                    return "Storm";
                case ActiveWorldEvent.HordeAssault:
                    return "Rain";
                default:
                    return includeClearFallback ? "Clear" : null;
            }
        }

        private static void TryPlayAftermathFx(double now)
        {
            if (!HasActiveAftermath || now < _nextAftermathFxAt || PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            _nextAftermathFxAt = now + AftermathFxIntervalSeconds;
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            var processedColonies = new HashSet<ColonyID>();
            while (colonies.MoveNext())
            {
                var colony = ResolvePrimaryEventColony(colonies.Current);
                if (!IsPrimaryEventColony(colony) || !processedColonies.Add(colony.ColonyID))
                    continue;

                for (var i = 0; i < colony.Banners.Count; i++)
                {
                    var center = colony.Banners[i].Position.Vector + new UnityEngine.Vector3(0.5f, 0.6f, 0.5f);
                    PlayAftermathBannerFx(center, _currentAftermath);
                }
            }
        }

        private static void PlayAftermathBannerFx(UnityEngine.Vector3 center, ActiveAftermath aftermath)
        {
            switch (aftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    ServerManager.SendExplosionEffect(center, 8f, 0.95f, 1f, 0.2f);
                    ServerManager.SendParticleTrail(center + new UnityEngine.Vector3(-0.7f, 1.6f, 0f), center + new UnityEngine.Vector3(0.7f, 0f, 0f), 0.4f);
                    ServerManager.SendParticleTrail(center + new UnityEngine.Vector3(0f, 1.8f, -0.7f), center + new UnityEngine.Vector3(0f, 0f, 0.7f), 0.4f);
                    break;
                case ActiveAftermath.PlagueSpores:
                    ServerManager.SendExplosionEffect(center, 8f, 0.75f, 1f, 0.2f);
                    for (var i = 0; i < 3; i++)
                    {
                        var offset = new UnityEngine.Vector3(UnityEngine.Random.Range(-0.9f, 0.9f), UnityEngine.Random.Range(0.5f, 1.6f), UnityEngine.Random.Range(-0.9f, 0.9f));
                        ServerManager.SendParticleTrail(center + offset, center + new UnityEngine.Vector3(0f, 0.15f, 0f), 0.55f);
                    }
                    break;
                case ActiveAftermath.ArcaneCharge:
                    ServerManager.SendExplosionEffect(center, 8f, 1.05f, 1f, 0.2f);
                    ServerManager.SendParticleTrail(center + new UnityEngine.Vector3(-0.8f, 1.8f, -0.8f), center + new UnityEngine.Vector3(0.8f, 0.2f, 0.8f), 0.36f);
                    ServerManager.SendParticleTrail(center + new UnityEngine.Vector3(0.8f, 1.8f, -0.8f), center + new UnityEngine.Vector3(-0.8f, 0.2f, 0.8f), 0.36f);
                    break;
                case ActiveAftermath.WarDrums:
                    ServerManager.SendExplosionEffect(center, 8f, 1.15f, 1f, 0.2f);
                    ServerManager.SendParticleTrail(center + new UnityEngine.Vector3(-1.0f, 1.2f, 0f), center + new UnityEngine.Vector3(1.0f, 0.2f, 0f), 0.32f);
                    ServerManager.SendParticleTrail(center + new UnityEngine.Vector3(0f, 1.5f, -1.0f), center + new UnityEngine.Vector3(0f, 0.2f, 1.0f), 0.32f);
                    break;
            }
        }

        private static Colony ResolvePrimaryEventColony(Colony colony)
        {
            if (colony == null)
                return null;

            return colony.ColonyGroup?.MainColony ?? colony;
        }

        private static bool IsPrimaryEventColony(Colony colony)
        {
            return colony != null &&
                   colony.Banners.Count > 0 &&
                   colony.ColonyGroup?.Stockpile != null &&
                   colony.ColonyGroup.MainColonyID == colony.ColonyID;
        }

        private static bool HasAnyPrimaryEventColony()
        {
            var seenPrimaryColonies = new HashSet<ColonyID>();
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            while (colonies.MoveNext())
            {
                var colony = ResolvePrimaryEventColony(colonies.Current);
                if (!IsPrimaryEventColony(colony) || !seenPrimaryColonies.Add(colony.ColonyID))
                    continue;

                return true;
            }

            return false;
        }

        private static bool CanPlayerTriggerMainColonyEvent(Players.Player initiator, out string failureMessage)
        {
            failureMessage = null;
            var colony = ResolvePrimaryEventColony(initiator?.ActiveColony);
            if (initiator != null && colony != null && initiator.OwnsColony(colony) && IsPrimaryEventColony(colony))
                return true;

            failureMessage = "World event commands only work at your active main colony with a stockpile. Outposts are blocked.";
            return false;
        }

        private static bool TryRollAftermathKillReward(ActiveAftermath aftermath, out ushort itemIndex, out int amount, out string displayName)
        {
            itemIndex = 0;
            amount = 0;
            displayName = null;

            var rollChance = GetAftermathKillRewardRollChance(aftermath);
            if (rollChance <= 0f || Pipliz.Random.NextFloat() > rollChance)
                return false;

            var definitions = GetAftermathKillRewardDefinitions(aftermath);
            if (definitions == null || definitions.Count == 0)
                return false;

            var totalWeight = 0f;
            for (var i = 0; i < definitions.Count; i++)
                totalWeight += UnityEngine.Mathf.Max(0f, definitions[i].Weight);

            if (totalWeight <= 0f)
                return false;

            var pick = Pipliz.Random.NextFloat() * totalWeight;
            var selected = definitions[definitions.Count - 1];
            for (var i = 0; i < definitions.Count; i++)
            {
                pick -= UnityEngine.Mathf.Max(0f, definitions[i].Weight);
                if (pick <= 0f)
                {
                    selected = definitions[i];
                    break;
                }
            }

            if (!BossLootTable.TryResolveRewardItem(selected.ItemKey, out itemIndex, out displayName))
                return false;

            amount = selected.MinAmount >= selected.MaxAmount
                ? selected.MinAmount
                : Pipliz.Random.Next(selected.MinAmount, selected.MaxAmount + 1);
            return amount > 0;
        }

        private static float GetAftermathKillRewardRollChance(ActiveAftermath aftermath)
        {
            switch (aftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    return 0.12f;
                case ActiveAftermath.PlagueSpores:
                    return 0.13f;
                case ActiveAftermath.ArcaneCharge:
                    return 0.14f;
                case ActiveAftermath.WarDrums:
                    return 0.15f;
                default:
                    return 0f;
            }
        }

        private static List<AftermathKillRewardDefinition> GetAftermathKillRewardDefinitions(ActiveAftermath aftermath)
        {
            switch (aftermath)
            {
                case ActiveAftermath.SanguineResidue:
                    return new List<AftermathKillRewardDefinition>
                    {
                        new AftermathKillRewardDefinition { ItemKey = "Mana", MinAmount = 1, MaxAmount = 2, Weight = 45f },
                        new AftermathKillRewardDefinition { ItemKey = "Esper", MinAmount = 1, MaxAmount = 1, Weight = 24f },
                        new AftermathKillRewardDefinition { ItemKey = "FireStone", MinAmount = 1, MaxAmount = 1, Weight = 18f },
                        new AftermathKillRewardDefinition { ItemKey = "Void", MinAmount = 1, MaxAmount = 1, Weight = 13f }
                    };
                case ActiveAftermath.PlagueSpores:
                    return new List<AftermathKillRewardDefinition>
                    {
                        new AftermathKillRewardDefinition { ItemKey = "Mana", MinAmount = 1, MaxAmount = 2, Weight = 24f },
                        new AftermathKillRewardDefinition { ItemKey = "Aether", MinAmount = 1, MaxAmount = 1, Weight = 20f },
                        new AftermathKillRewardDefinition { ItemKey = "TreatedBandage", MinAmount = 1, MaxAmount = 2, Weight = 30f },
                        new AftermathKillRewardDefinition { ItemKey = "Antibiotic", MinAmount = 1, MaxAmount = 2, Weight = 26f }
                    };
                case ActiveAftermath.ArcaneCharge:
                    return new List<AftermathKillRewardDefinition>
                    {
                        new AftermathKillRewardDefinition { ItemKey = "Mana", MinAmount = 1, MaxAmount = 3, Weight = 34f },
                        new AftermathKillRewardDefinition { ItemKey = "Esper", MinAmount = 1, MaxAmount = 2, Weight = 26f },
                        new AftermathKillRewardDefinition { ItemKey = "AirStone", MinAmount = 1, MaxAmount = 1, Weight = 20f },
                        new AftermathKillRewardDefinition { ItemKey = "WaterStone", MinAmount = 1, MaxAmount = 1, Weight = 20f }
                    };
                case ActiveAftermath.WarDrums:
                    return new List<AftermathKillRewardDefinition>
                    {
                        new AftermathKillRewardDefinition { ItemKey = "Mana", MinAmount = 1, MaxAmount = 3, Weight = 28f },
                        new AftermathKillRewardDefinition { ItemKey = "Elementium", MinAmount = 1, MaxAmount = 1, Weight = 32f },
                        new AftermathKillRewardDefinition { ItemKey = "AdamantineNugget", MinAmount = 1, MaxAmount = 2, Weight = 26f },
                        new AftermathKillRewardDefinition { ItemKey = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Weight = 14f }
                    };
                default:
                    return null;
            }
        }

        private static double GetColonyVictoryBlessingDurationSeconds()
        {
            var duration = TimeCycle.GameTimeSpan.FromGameDays(1d).GetRealTimeSpan().TotalSeconds;
            return duration > 0d ? duration : 1200d;
        }

        private static string GetColonyBlessingStatusText()
        {
            var activeBlessings = 0;
            var now = Pipliz.Time.SecondsSinceStartDouble;

            foreach (var pair in ColonyVictoryBlessings)
            {
                if (pair.Key != null && pair.Value != null && now < pair.Value.EndsAt)
                    activeBlessings++;
            }

            return activeBlessings > 0
                ? " Colony haste buffs active: " + activeBlessings + "."
                : string.Empty;
        }

        private static void UpdateHudForAll(double now)
        {
            string hudText;
            string color;

            if (HasActiveEvent)
            {
                var remaining = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt((float)(_eventEndsAt - now)));
                hudText = "World Event: " + GetEventDisplayName(_currentEvent) + " [" + FormatRemaining(remaining) + "]\n" + GetEventGameplaySummary(_currentEvent);
                color = GetEventHudColor(_currentEvent);
            }
            else
            {
                var remaining = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt((float)(_aftermathEndsAt - now)));
                hudText = "Event Aftermath: " + GetAftermathDisplayName(_currentAftermath) + " [" + FormatRemaining(remaining) + "]\n" + GetAftermathGameplaySummary(_currentAftermath);
                color = GetAftermathHudColor(_currentAftermath);
            }

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                UIManager.AddorUpdateUILabel(
                    EventHudKey,
                    UIElementDisplayType.Global,
                    hudText,
                    new Pipliz.Vector3Int(0, -72, 0),
                    AnchorPresets.TopCenter,
                    900f,
                    player,
                    16f,
                    FontType.DidactGothic,
                    color,
                    TextAlignmentOptions.Center);
            }
        }

        private static void ClearHudForAll()
        {
            foreach (var player in Players.ConnectedPlayers)
            {
                if (player != null)
                    UIManager.RemoveUILabel(EventHudKey, player);
            }
        }

        private static void UpdateOverlayForAll()
        {
            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                UpdateOverlayForPlayer(player);
            }
        }

        private static void UpdateOverlayForPlayer(Players.Player player)
        {
            if (!PlayerUiGuard.CanSendStable(player))
                return;

            var imageBaseName = GetDesiredOverlayImageName();
            if (string.IsNullOrEmpty(imageBaseName))
            {
                RemoveOverlayImages(player);
                return;
            }

            UIManager.AddorUpdateUIImage(
                EventOverlayTopKey,
                UIElementDisplayType.Global,
                imageBaseName + ".Top",
                new Pipliz.Vector3Int(0, 0, 0),
                AnchorPresets.HorStretchTop,
                player);

            UIManager.AddorUpdateUIImage(
                EventOverlayLeftKey,
                UIElementDisplayType.Global,
                imageBaseName + ".Left",
                new Pipliz.Vector3Int(0, 0, 0),
                AnchorPresets.VertStretchLeft,
                player);

            UIManager.AddorUpdateUIImage(
                EventOverlayRightKey,
                UIElementDisplayType.Global,
                imageBaseName + ".Right",
                new Pipliz.Vector3Int(0, 0, 0),
                AnchorPresets.VertStretchRight,
                player);
        }

        private static void ClearOverlayForAll()
        {
            foreach (var player in Players.ConnectedPlayers)
            {
                if (player != null)
                    RemoveOverlayImages(player);
            }
        }

        private static void RemoveOverlayImages(Players.Player player)
        {
            if (!PlayerUiGuard.CanSendStable(player))
                return;

            UIManager.RemoveUIImage(EventOverlayTopKey, player);
            UIManager.RemoveUIImage(EventOverlayLeftKey, player);
            UIManager.RemoveUIImage(EventOverlayRightKey, player);
        }

        private static string GetDesiredOverlayImageName()
        {
            if (HasActiveEvent)
            {
                switch (_currentEvent)
                {
                    case ActiveWorldEvent.BloodMoon:
                        return BloodMoonOverlayImagePrefix;
                    case ActiveWorldEvent.PlagueFog:
                        return PlagueFogOverlayImagePrefix;
                    case ActiveWorldEvent.ArcaneStorm:
                        return ArcaneStormOverlayImagePrefix;
                    case ActiveWorldEvent.HordeAssault:
                        return HordeOverlayImagePrefix;
                }
            }

            if (HasActiveAftermath)
            {
                switch (_currentAftermath)
                {
                    case ActiveAftermath.SanguineResidue:
                        return BloodMoonAftermathOverlayImagePrefix;
                    case ActiveAftermath.PlagueSpores:
                        return PlagueFogAftermathOverlayImagePrefix;
                    case ActiveAftermath.ArcaneCharge:
                        return ArcaneStormAftermathOverlayImagePrefix;
                    case ActiveAftermath.WarDrums:
                        return HordeAftermathOverlayImagePrefix;
                }
            }

            return null;
        }

        private static void Broadcast(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player))
                    PlayerToastManager.Show(player, message, "#f0dcb4", 5000L);
            }
        }

        private static string FormatRemaining(int totalSeconds)
        {
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private static string FormatDuration(double totalSeconds)
        {
            var totalMinutes = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt((float)(totalSeconds / 60d)));
            if (totalMinutes >= 60)
            {
                var hours = totalMinutes / 60;
                var minutes = totalMinutes % 60;
                return minutes > 0
                    ? hours + "h " + minutes + "m"
                    : hours + "h";
            }

            return totalMinutes + "m";
        }

        private static void ApplyVictoryBlessingToFollower(NPCBase follower, ColonyVictoryBlessingRuntime blessing, double? nowOverride = null)
        {
            if (follower == null || blessing == null || !follower.IsValid || follower.health <= 0f)
                return;

            if (!blessing.AppliedFollowers.Contains(follower.ID))
            {
                follower.SetSpeedMultiplier(ColonyVictorySpeedMultiplier);
                if (follower.speed.Speed > 0.001f)
                    follower.oneOverSpeed = 1f / follower.speed.Speed;
                blessing.AppliedFollowers.Add(follower.ID);
            }

            ScaleFollowerQueuedWork(follower, blessing, nowOverride);
        }

        private static void RestoreFollowerVictoryBlessing(NPCBase follower)
        {
            if (follower == null || !follower.IsValid)
                return;

            follower.SetSpeedMultiplier(1f);
            if (follower.speed.Speed > 0.001f)
                follower.oneOverSpeed = 1f / follower.speed.Speed;
        }

        private static void ScaleFollowerQueuedWork(NPCBase follower, ColonyVictoryBlessingRuntime blessing, double? nowOverride = null)
        {
            if (follower == null || blessing == null || !follower.IsValid)
                return;

            var sourceQueue = follower.queuedNextUpdateSimTIme;
            if (sourceQueue <= 0L || blessing.LastScaledQueueSource.TryGetValue(follower.ID, out var previousSource) && previousSource == sourceQueue)
                return;

            var nowMilliseconds = (long)System.Math.Round((nowOverride ?? Pipliz.Time.SecondsSinceStartDouble) * 1000d);
            var remainingMilliseconds = sourceQueue - nowMilliseconds;
            if (remainingMilliseconds <= 1L)
            {
                blessing.LastScaledQueueSource[follower.ID] = sourceQueue;
                return;
            }

            var scaledMilliseconds = System.Math.Max(1L, (long)System.Math.Round(remainingMilliseconds / ColonyVictoryWorkSpeedMultiplier));
            var newQueueTime = nowMilliseconds + scaledMilliseconds;
            if (newQueueTime >= sourceQueue)
            {
                blessing.LastScaledQueueSource[follower.ID] = sourceQueue;
                return;
            }

            follower.state.CooldownTill = new ServerTimeStamp(newQueueTime);
            follower.queuedNextUpdateSimTIme = newQueueTime;
            NPCTracker.updateQueueSim.Add(newQueueTime, follower);
            blessing.LastScaledQueueSource[follower.ID] = sourceQueue;
        }

        private static void NotifyOwners(Colony colony, string message)
        {
            if (colony == null || string.IsNullOrEmpty(message))
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player) && player.OwnsColony(colony))
                    PlayerToastManager.Show(player, message, "#d8f0bf", 5200L);
            }
        }
    }
}
