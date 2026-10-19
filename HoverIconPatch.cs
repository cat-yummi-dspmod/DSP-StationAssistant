using System;
using HarmonyLib;
using UnityEngine;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class HoverIconPatch
    {
        private static bool logged;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIEntityBriefInfo), "_OnCreate")]
        [CatAPI.Guard("69d3bf097eb80ed0")]
        public static void OnCreate_Prefix(UIEntityBriefInfo __instance)
        {
            Grow(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIEntityBriefInfo), "_OnUpdate")]
        [CatAPI.Guard("a72ad04c70fde856")]
        public static void OnUpdate_Prefix(UIEntityBriefInfo __instance)
        {
            Grow(__instance);
        }

        private static void Grow(UIEntityBriefInfo info)
        {
            if (info?.icons == null || info.icons.Length >= Settings.MaxSlots)
            {
                return;
            }
            UIIconCountInc sample = null;
            for (int i = 0; i < info.icons.Length; i++)
            {
                if (info.icons[i] != null)
                {
                    sample = info.icons[i];
                    break;
                }
            }
            int previous = info.icons.Length;
            Array.Resize(ref info.icons, Settings.MaxSlots);
            if (sample == null)
            {
                return;
            }
            for (int i = previous; i < info.icons.Length; i++)
            {
                UIIconCountInc icon = UnityEngine.Object.Instantiate(sample, sample.transform.parent);
                icon.visible = false;
                info.icons[i] = icon;
            }
            if (!logged)
            {
                logged = true;
                Plugin.Log.LogInfo($"悬停图标 {previous} → {info.icons.Length}");
            }
        }
    }
}
