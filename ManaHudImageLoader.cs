using System.Collections.Generic;
using System.IO;
using ModLoaderInterfaces;

namespace BetterNecromancy
{
    public sealed class ManaHudImageLoader : IOnLoadingImages
    {
        private static readonly Dictionary<string, string> EventOverlayFiles = new Dictionary<string, string>
        {
            { ModEntry.Namespace + ".EventOverlay.BloodMoon.Top", "event_overlay_bloodmoon_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.BloodMoon.Left", "event_overlay_bloodmoon_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.BloodMoon.Right", "event_overlay_bloodmoon_rightedge.png" },
            { ModEntry.Namespace + ".EventOverlay.PlagueFog.Top", "event_overlay_plaguefog_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.PlagueFog.Left", "event_overlay_plaguefog_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.PlagueFog.Right", "event_overlay_plaguefog_rightedge.png" },
            { ModEntry.Namespace + ".EventOverlay.ArcaneStorm.Top", "event_overlay_arcanestorm_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.ArcaneStorm.Left", "event_overlay_arcanestorm_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.ArcaneStorm.Right", "event_overlay_arcanestorm_rightedge.png" },
            { ModEntry.Namespace + ".EventOverlay.Horde.Top", "event_overlay_horde_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.Horde.Left", "event_overlay_horde_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.Horde.Right", "event_overlay_horde_rightedge.png" },
            { ModEntry.Namespace + ".EventOverlay.BloodMoonAftermath.Top", "event_overlay_bloodmoon_aftermath_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.BloodMoonAftermath.Left", "event_overlay_bloodmoon_aftermath_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.BloodMoonAftermath.Right", "event_overlay_bloodmoon_aftermath_rightedge.png" },
            { ModEntry.Namespace + ".EventOverlay.PlagueFogAftermath.Top", "event_overlay_plaguefog_aftermath_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.PlagueFogAftermath.Left", "event_overlay_plaguefog_aftermath_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.PlagueFogAftermath.Right", "event_overlay_plaguefog_aftermath_rightedge.png" },
            { ModEntry.Namespace + ".EventOverlay.ArcaneStormAftermath.Top", "event_overlay_arcanestorm_aftermath_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.ArcaneStormAftermath.Left", "event_overlay_arcanestorm_aftermath_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.ArcaneStormAftermath.Right", "event_overlay_arcanestorm_aftermath_rightedge.png" },
            { ModEntry.Namespace + ".EventOverlay.HordeAftermath.Top", "event_overlay_horde_aftermath_topedge.png" },
            { ModEntry.Namespace + ".EventOverlay.HordeAftermath.Left", "event_overlay_horde_aftermath_leftedge.png" },
            { ModEntry.Namespace + ".EventOverlay.HordeAftermath.Right", "event_overlay_horde_aftermath_rightedge.png" }
        };

        public void IOnLoadingImages(Dictionary<string, string> imagesToLoad)
        {
            if (string.IsNullOrEmpty(ModEntry.ModFolder))
                return;

            for (var i = 0; i <= 20; i++)
            {
                imagesToLoad[ModEntry.Namespace + ".ManaBar" + i] = Path.Combine(ModEntry.ModFolder, "icons", "mana_bar_" + i + ".png").Replace("\\", "/");
            }

            foreach (var pair in EventOverlayFiles)
            {
                imagesToLoad[pair.Key] = Path.Combine(ModEntry.ModFolder, "icons", pair.Value).Replace("\\", "/");
            }
        }
    }
}
