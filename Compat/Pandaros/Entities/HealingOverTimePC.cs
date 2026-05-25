using Pipliz;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pandaros.API.Entities
{
    [ModLoader.ModManager]
    public class HealingOverTimePC
    {
        private static long _nextUpdate;
        private static readonly List<HealingOverTimePC> Instances = new List<HealingOverTimePC>();
        private static readonly List<HealingOverTimePC> ToRemove = new List<HealingOverTimePC>();

        public HealingOverTimePC(Players.Player player, float initialHeal, float totalHealOverTime, int durationSeconds)
        {
            HealPerTick = totalHealOverTime / durationSeconds;
            DurationSeconds = durationSeconds;
            InitialHeal = initialHeal;
            Target = player;
            TotalHealOverTime = totalHealOverTime;
            TicksLeft = durationSeconds;

            NewInstance?.Invoke(this, EventArgs.Empty);

            Instances.Add(this);
            HealPlayer(Target, InitialHeal);
            Tick += HandleTick;
        }

        public Players.Player Target { get; }
        public float TotalHealOverTime { get; }
        public float InitialHeal { get; }
        public int DurationSeconds { get; }
        public int TicksLeft { get; private set; }
        public float HealPerTick { get; }

        public static event EventHandler NewInstance;
        public event EventHandler Complete;
        public event EventHandler Tick;

        private void HandleTick(object sender, EventArgs e)
        {
            TicksLeft--;
            HealPlayer(Target, HealPerTick);

            if (TicksLeft <= 0)
                Complete?.Invoke(this, EventArgs.Empty);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".HealingOverTimePC.Update")]
        public static void Update()
        {
            if (Pipliz.Time.MillisecondsSinceStart <= _nextUpdate || Instances.Count == 0)
                return;

            ToRemove.Clear();

            foreach (var healing in Instances)
            {
                healing.Tick?.Invoke(healing, EventArgs.Empty);

                if (healing.TicksLeft <= 0)
                    ToRemove.Add(healing);
            }

            foreach (var remove in ToRemove)
                Instances.Remove(remove);

            ToRemove.Clear();
            _nextUpdate = Pipliz.Time.MillisecondsSinceStart + 1000;
        }

        private static void HealPlayer(Players.Player player, float amount)
        {
            var adjustedAmount = amount * PlayerMagicStateManager.GetHealingReceivedMultiplier(player);
            player.Health = Mathf.Min(player.HealthMax, player.Health + adjustedAmount);
            player.SendHealthPacket();
        }
    }
}
