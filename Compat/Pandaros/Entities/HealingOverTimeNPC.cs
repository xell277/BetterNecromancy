using NPC;
using Pipliz;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pandaros.API.Entities
{
    [ModLoader.ModManager]
    public class HealingOverTimeNPC
    {
        private static long _nextUpdate;
        private static readonly List<HealingOverTimeNPC> Instances = new List<HealingOverTimeNPC>();
        private static readonly List<HealingOverTimeNPC> ToRemove = new List<HealingOverTimeNPC>();

        public HealingOverTimeNPC(NPCBase npc, float initialHeal, float totalHealOverTime, int durationSeconds, ushort indicator)
        {
            HealPerTick = totalHealOverTime / durationSeconds;
            DurationSeconds = durationSeconds;
            InitialHeal = initialHeal;
            Target = npc;
            TotalHealOverTime = totalHealOverTime;
            TicksLeft = durationSeconds;
            Indicator = indicator;

            NewInstance?.Invoke(this, EventArgs.Empty);

            Instances.Add(this);
            HealNPC(Target, InitialHeal);
            if (!BetterNecromancy.PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                Target.SetIndicatorState(IndicatorState.NewItemIndicator(1f, Indicator));
            Tick += HandleTick;
        }

        public bool Invalid { get; set; }
        public NPCBase Target { get; }
        public float TotalHealOverTime { get; }
        public float InitialHeal { get; }
        public int DurationSeconds { get; }
        public int TicksLeft { get; private set; }
        public float HealPerTick { get; }
        public ushort Indicator { get; }

        public static event EventHandler NewInstance;
        public event EventHandler Complete;
        public event EventHandler Tick;

        private void HandleTick(object sender, EventArgs e)
        {
            TicksLeft--;

            try
            {
                HealNPC(Target, HealPerTick);

                if (TicksLeft <= 0)
                    Complete?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                Invalid = true;
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".HealingOverTimeNPC.Update")]
        public static void Update()
        {
            if (Pipliz.Time.MillisecondsSinceStart <= _nextUpdate || Instances.Count == 0)
                return;

            ToRemove.Clear();

            foreach (var healing in Instances)
            {
                try
                {
                    if (!BetterNecromancy.PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                        healing.Target.SetIndicatorState(IndicatorState.NewItemIndicator(1f, healing.Indicator));
                    healing.Tick?.Invoke(healing, EventArgs.Empty);

                    if (healing.TicksLeft <= 0 || healing.Invalid)
                        ToRemove.Add(healing);
                }
                catch
                {
                    ToRemove.Add(healing);
                }
            }

            foreach (var remove in ToRemove)
                Instances.Remove(remove);

            ToRemove.Clear();
            _nextUpdate = Pipliz.Time.MillisecondsSinceStart + 1000;
        }

        public static bool NPCIsBeingHealed(NPCBase npc)
        {
            return Instances.Any(instance => instance.Target == npc);
        }

        private static void HealNPC(NPCBase npc, float amount)
        {
            float npcHealthMax = npc.Colony.ColonyGroup.NPCHealthMax;
            npc.health = Mathf.Min(npcHealthMax, npc.health + amount);
            if (!BetterNecromancy.PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                global::Indicator.SendHealthToAllNear(npc.ID, 1f, npc.health / npcHealthMax, npc.Position);
        }
    }
}
