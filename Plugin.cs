using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace StationAssistant
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(YummiHarness.HarnessPlugin.PluginGuid)]
    [BepInDependency(LogisticsStationSlotsConflict.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.yummi.dsp.stationassistant";
        public const string PluginName = "Station Assistant（未完成，不建议使用）";
        public const string PluginVersion = "0.1.0";

        private static readonly Harmony harmony = new Harmony(PluginGuid);
        private static readonly YummiHarness.PatchOptions HarnessOptions = new YummiHarness.PatchOptions { ModName = PluginName };

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
            YummiHarness.Harness.RegisterConflict(PluginName, LogisticsStationSlotsConflict.Guid, "LogisticsStationSlots");
            YummiHarness.Harness.PatchAll(harmony, typeof(MainMenuBootstrap), HarnessOptions);
            Log.LogInfo($"{PluginName} v{PluginVersion} 等待主菜单就绪后再启用");
        }

        internal void ApplyPatches()
        {
            if (LogisticsStationSlotsConflict.IsPresent())
            {
                Log.LogError(LogisticsStationSlotsConflict.GetErrorMessage());
                return;
            }

            YummiHarness.Harness.PatchAll(harmony, typeof(StationExpandPatch), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(StationLogicPatch), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(StationWindowPatch), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(ControlPanelStoragePaging), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(WheelZoomGuard), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(HoverIconPatch), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(NumericEditPatch), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(DuplicateItemPatch), HarnessOptions);
            YummiHarness.Harness.PatchAll(harmony, typeof(ModSaveBridge), HarnessOptions);
            Log.LogInfo($"{PluginName} v{PluginVersion} 已加载");
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
