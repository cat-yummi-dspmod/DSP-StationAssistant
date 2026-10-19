using HarmonyLib;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class DuplicateItemPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), "OnItemPickerReturn")]
        [CatAPI.Guard("503e7f1250016a66")]
        public static bool Station_Prefix(UIStationStorage __instance, ItemProto itemProto)
        {
            if (!Settings.AllowDuplicate || itemProto == null || __instance.station == null
                || !SlotStorage.IsTower(__instance.station))
            {
                return true;
            }
            if (__instance.index < 0 || __instance.index >= __instance.station.storage.Length)
            {
                return true;
            }
            if (!Uses(__instance.station, itemProto.ID))
            {
                return true;
            }
            StationStore store = __instance.station.storage[__instance.index];
            __instance.stationWindow.transport.SetStationStorage(
                __instance.station.id,
                __instance.index,
                itemProto.ID,
                store.max > 0 ? store.max : 1,
                ELogisticStorage.Supply,
                __instance.station.isStellar ? ELogisticStorage.Supply : ELogisticStorage.None,
                GameMain.mainPlayer);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "OnItemPickerReturn")]
        [CatAPI.Guard("aa616b132d594380")]
        public static bool Panel_Prefix(UIControlPanelStationStorage __instance, ItemProto itemProto)
        {
            if (!Settings.AllowDuplicate || itemProto == null || !__instance.isStationDataValid
                || !SlotStorage.IsTower(__instance.station))
            {
                return true;
            }
            if (__instance.index < 0 || __instance.index >= __instance.station.storage.Length)
            {
                return true;
            }
            if (!Uses(__instance.station, itemProto.ID))
            {
                return true;
            }
            StationStore store = __instance.station.storage[__instance.index];
            __instance.masterInspector.transport.SetStationStorage(
                __instance.station.id,
                __instance.index,
                itemProto.ID,
                store.max > 0 ? store.max : 1,
                ELogisticStorage.Supply,
                __instance.station.isStellar ? ELogisticStorage.Supply : ELogisticStorage.None,
                GameMain.mainPlayer);
            return false;
        }

        private static bool Uses(StationComponent station, int itemId)
        {
            StationStore[] storage = station.storage;
            for (int i = 0; i < storage.Length; i++)
            {
                if (storage[i].itemId == itemId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
