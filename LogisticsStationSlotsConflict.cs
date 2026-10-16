using System;
using BepInEx.Bootstrap;

namespace StationAssistant
{
    internal static class LogisticsStationSlotsConflict
    {
        internal const string Guid = "local.logisticsstationslots";

        internal static bool IsPresent()
        {
            if (Chainloader.PluginInfos.ContainsKey(Guid))
            {
                return true;
            }
            foreach (BepInEx.PluginInfo info in Chainloader.PluginInfos.Values)
            {
                if (info.Metadata.Name.Equals("LogisticsStationSlots", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (info.Metadata.GUID.IndexOf("logisticsstationslots", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        internal static string GetErrorMessage()
        {
            return
                $"Could not load [{Plugin.PluginName} {Plugin.PluginVersion}] because it is incompatible with LogisticsStationSlots ({Guid}). This mod has been disabled; uninstall one of them.\n\n" +
                $"无法加载 [{Plugin.PluginName} {Plugin.PluginVersion}]，因其与 LogisticsStationSlots（{Guid}）不兼容。本 mod 已停用，请卸载其中一个。";
        }
    }
}
