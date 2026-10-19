using HarmonyLib;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class StationExpandPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.Init))]
        [CatAPI.Guard("7f968c39fde5bbec")]
        public static void StationComponent_Init_Postfix(StationComponent __instance)
        {
            SlotStorage.ExpandIfNeeded(__instance, true);
        }
    }
}
