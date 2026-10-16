using System.Collections;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace StationAssistant
{
    [HarmonyPatch]
    internal static class MainMenuBootstrap
    {
        private static BaseUnityPlugin host;
        private static bool preloadHookFired;

        internal static void Init(BaseUnityPlugin pluginHost)
        {
            host = pluginHost;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        [HarmonyPriority(Priority.Last)]
        [YummiHarness.Guard("4185730b6448e635")]
        public static void InvokeOnLoadWorkEnded_Postfix()
        {
            if (preloadHookFired || host == null)
            {
                return;
            }
            preloadHookFired = true;
            host.StartCoroutine(AfterPreloadSplash());
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIRoot), "_OnOpen")]
        [HarmonyPriority(Priority.Last)]
        [YummiHarness.Guard("05fc7f22583910d4")]
        public static void UIRoot_OnOpen_Postfix()
        {
            if (!DSPGame.IsMenuDemo || host == null)
            {
                return;
            }
            host.StartCoroutine(AfterPreloadSplash());
        }

        private static IEnumerator AfterPreloadSplash()
        {
            if (!VFPreload.done)
            {
                yield return new WaitUntil(() => VFPreload.done);
            }
            yield return null;
            ConflictStartup.OnMainMenuReady();
        }
    }

    internal static class FeatureStartup
    {
        private static bool featuresEnabled;

        internal static void EnableFeatures()
        {
            if (featuresEnabled || ConflictStartup.Blocked)
            {
                return;
            }
            featuresEnabled = true;
            Plugin.Instance.ApplyPatches();
        }
    }
}
