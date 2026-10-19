using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace StationAssistant
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(CatAPI.HarnessPlugin.PluginGuid)]
    [BepInDependency(LogisticsStationSlotsConflict.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.yummi.dsp.stationassistant";
        public const string PluginName = "Station Assistant";
        public const string PluginVersion = "0.1.8";

        private static readonly Harmony harmony = new Harmony(PluginGuid);
        private static readonly CatAPI.PatchOptions HarnessOptions = new CatAPI.PatchOptions { ModName = PluginName };

        internal static ManualLogSource Log;
        internal static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Settings.Bind(Config);
            CatFeature.Apply();
            MainMenuBootstrap.Init(this);
            ConflictStartup.BeginScan(this);
            CatAPI.Harness.RegisterConflict(PluginName, LogisticsStationSlotsConflict.Guid, "LogisticsStationSlots");
            CatAPI.Harness.PatchAll(harmony, typeof(MainMenuBootstrap), HarnessOptions);
            Log.LogInfo($"{PluginName} v{PluginVersion} 等待主菜单就绪后再启用");
        }

        internal void ApplyPatches()
        {
            if (LogisticsStationSlotsConflict.IsPresent())
            {
                Log.LogError(LogisticsStationSlotsConflict.GetErrorMessage());
                return;
            }

            CatAPI.Harness.PatchAll(harmony, typeof(StationExpandPatch), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(StationLogicPatch), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(StationWindowPatch), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(ControlPanelStoragePaging), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(WheelZoomGuard), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(HoverIconPatch), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(NumericEditPatch), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(DuplicateItemPatch), HarnessOptions);
            CatAPI.Harness.PatchAll(harmony, typeof(ModSaveBridge), HarnessOptions);
            Log.LogInfo($"{PluginName} v{PluginVersion} 已加载");
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
