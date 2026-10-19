using HarmonyLib;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class WheelZoomGuard
    {
        private static bool SuppressZoom()
        {
            try
            {
                if (StationWindowPatch.CursorOverPagingArea())
                {
                    return true;
                }
            }
            catch
            {
            }
            try
            {
                if (ControlPanelStoragePaging.CursorOverPagingArea())
                {
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VFInput), "get__cameraZoomIn")]
        [CatAPI.Guard("7af092698a0fcc7e")]
        public static bool ZoomIn_Prefix(ref float __result)
        {
            if (!SuppressZoom())
            {
                return true;
            }
            __result = 0f;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VFInput), "get__cameraZoomOut")]
        [CatAPI.Guard("4f5adcab95ca0a54")]
        public static bool ZoomOut_Prefix(ref float __result)
        {
            if (!SuppressZoom())
            {
                return true;
            }
            __result = 0f;
            return false;
        }
    }
}
