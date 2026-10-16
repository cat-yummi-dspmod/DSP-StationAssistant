using System.Collections;
using BepInEx;

namespace StationAssistant
{
    internal static class ConflictStartup
    {
        internal static bool Blocked { get; private set; }
        internal static bool ScanComplete { get; private set; }

        internal static void BeginScan(BaseUnityPlugin host)
        {
            host.StartCoroutine(ScanAfterChainloader());
        }

        private static IEnumerator ScanAfterChainloader()
        {
            yield return null;
            ScanComplete = true;
            Blocked = LogisticsStationSlotsConflict.IsPresent();
            if (Blocked)
            {
                Plugin.Log?.LogError(LogisticsStationSlotsConflict.GetErrorMessage());
            }
        }

        internal static void OnMainMenuReady()
        {
            if (!ScanComplete)
            {
                Blocked = LogisticsStationSlotsConflict.IsPresent();
                ScanComplete = true;
            }
            if (Blocked || LogisticsStationSlotsConflict.IsPresent())
            {
                Blocked = true;
                Plugin.Log?.LogError(LogisticsStationSlotsConflict.GetErrorMessage());
                return;
            }
            FeatureStartup.EnableFeatures();
        }
    }
}
