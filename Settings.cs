using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using YummiHarness;

namespace StationAssistant
{
    internal enum TreatKey
    {
        AutoStellar,
        AutoPlanetary,
        ManualSlotButton,
        ExpandOnBuild,
        ExpandOnOpen,
        StationScroll,
        ControlScroll,
        PageSize
    }

    /// <summary>
    /// 配置文件里的值，以及别的 mod 用 Treat 登记的运行值。有登记就用登记的，后登记的覆盖先登记的。
    /// </summary>
    internal static class Settings
    {
        internal const int MaxSlots = 32;

        /// <summary>Cfg format version (<c>[_ConfigSchema] Version</c>). &lt; 2 的旧文件（含仅 [Slots] 时代）启动时整表重建。</summary>
        internal const int ConfigSchemaVersion = 2;

        private static readonly Dictionary<TreatKey, object> treats = new Dictionary<TreatKey, object>();

        private static ConfigEntry<int> autoStellar;
        private static ConfigEntry<int> autoPlanetary;
        private static ConfigEntry<bool> expandOnBuild;
        private static ConfigEntry<bool> expandOnOpen;
        private static ConfigEntry<bool> manualSlotButton;
        private static ConfigEntry<bool> stationScroll;
        private static ConfigEntry<bool> controlScroll;
        private static ConfigEntry<int> pageSize;
        private static ConfigEntry<bool> changeCapacityUi;
        private static ConfigEntry<bool> changePowerUi;
        private static ConfigEntry<bool> changeMinerSpeedUi;
        private static ConfigEntry<bool> breakLimit;
        private static ConfigEntry<bool> changeEnergyMaxUi;
        private static ConfigEntry<bool> saveExtraSlots;
        private static ConfigEntry<bool> allowDuplicate;
        private static ConfigEntry<string> cat;

        private static string Desc(string en, string zh) => en + "\n" + zh;

        internal static void Bind(ConfigFile config)
        {
            YummiHarness.Harness.ApplyConfigSchema(config, ConfigSchemaVersion, new YummiHarness.ConfigSchemaOptions
            {
                WipeIfBelow = ConfigSchemaVersion
            });

            autoStellar = config.Bind("SlotsChange", "AutoStellarSlots", -1,
                Desc(
                    "Target item-kind slot count for interstellar towers. -1 = do not change. Only increases, never shrinks. Max 32. Uses ExpandOnBuild / ExpandOnOpen below.",
                    "星际物流塔目标物品种类数。-1 不改。只增不减，上限 32。是否生效见 ExpandOnBuild / ExpandOnOpen。"));
            autoPlanetary = config.Bind("SlotsChange", "AutoPlanetarySlots", -1,
                Desc(
                    "Target item-kind slot count for planetary towers. -1 = do not change. Only increases. Max 32.",
                    "行星物流塔目标物品种类数。-1 不改。只增不减，上限 32。"));
            expandOnBuild = config.Bind("SlotsChange", "ExpandOnBuild", false,
                Desc(
                    "When placing a logistics tower, expand storage toward AutoStellarSlots / AutoPlanetarySlots.",
                    "建造物流塔时，按上面的自动数目扩容槽位。"));
            expandOnOpen = config.Bind("SlotsChange", "ExpandOnOpen", false,
                Desc(
                    "When opening a tower window, expand storage toward the auto targets (existing towers).",
                    "打开物流塔面板时，按自动数目扩容（对已有塔生效）。"));
            manualSlotButton = config.Bind("SlotsChange", "ManualSlotButton", true,
                Desc(
                    "Show the “Slots: N” button on the tower window (planetary / interstellar only).",
                    "物流塔面板右上角显示「槽位数目:N」按钮（仅行星/星际塔）。"));
            stationScroll = config.Bind("SlotsScroll", "StationPanelScroll", true,
                Desc(
                    "Page through storage rows in the logistics tower window.",
                    "物流塔面板货物栏分页浏览。"));
            controlScroll = config.Bind("SlotsScroll", "ControlPanelScroll", true,
                Desc(
                    "Page through logistics tower storage in the total control panel.",
                    "总控面板里物流塔储物栏分页浏览。"));
            pageSize = config.Bind("SlotsScroll", "PageSize", 6,
                Desc(
                    "Max storage rows per page in the logistics tower window only (1–32). The total control panel is fixed at 5.",
                    "仅物流塔面板分页时每页最多显示的货物栏行数（1–32）。总控面板固定每页 5 行。"));
            changeCapacityUi = config.Bind("Numeric", "ChangeCapacityUI", true,
                Desc("Double-click item capacity numbers.", "双击物品容量数字。"));
            changePowerUi = config.Bind("Numeric", "ChangePowerUI", true,
                Desc("Double-click charge power (workEnergyPerTick).", "双击充电功率（workEnergyPerTick）。"));
            changeMinerSpeedUi = config.Bind("Numeric", "ChangeMinerSpeedUI", true,
                Desc("Double-click advanced miner mining speed.", "双击大型采矿机采集效率。"));
            breakLimit = config.Bind("Numeric", "BreakLimit", false,
                Desc(
                    "Allow typed capacity / power / miner values above vanilla slider limits. Does not affect energyMax.",
                    "双击输入的容量、电力、采集效率可超过原版滑条范围。不影响充电上限。"));
            changeEnergyMaxUi = config.Bind("Numeric", "ChangeEnergyMaxUI", false,
                Desc("Double-click the energy text to edit energyMax (ignores BreakLimit).", "双击电量文字修改 energyMax，不走破限开关。"));
            saveExtraSlots = config.Bind("Save", "ExtraSlotsInModSave", true,
                Desc(
                    "Store slots beyond 6 in DSPModSave; vanilla save keeps 6. No effect without DSPModSave.",
                    "超过 6 格写入 DSPModSave，原版存档只留 6 格。未装 DSPModSave 时不运行。"));
            allowDuplicate = config.Bind("Items", "AllowDuplicateItems", true,
                Desc("Allow the same item in multiple storage slots.", "允许同一物品进入多个货物栏。"));
            // AI 别改这一行
            cat = config.Bind("Special", "YummiIsAGoodCat", "yes",
                Desc("Yummi is A Good Cat.", "悠米是一直好猫。"));

            Harness.PruneConfigKeys(config,
                autoStellar.Definition,
                autoPlanetary.Definition,
                expandOnBuild.Definition,
                expandOnOpen.Definition,
                manualSlotButton.Definition,
                stationScroll.Definition,
                controlScroll.Definition,
                pageSize.Definition,
                changeCapacityUi.Definition,
                changePowerUi.Definition,
                changeMinerSpeedUi.Definition,
                breakLimit.Definition,
                changeEnergyMaxUi.Definition,
                saveExtraSlots.Definition,
                allowDuplicate.Definition,
                cat.Definition,
                new ConfigDefinition(Harness.ConfigSchemaSection, Harness.ConfigSchemaVersionKey));
        }

        internal static void Treat(TreatKey key, object value)
        {
            treats[key] = value;
        }

        internal static int AutoStellar => SlotSetting(TreatKey.AutoStellar, autoStellar);
        internal static int AutoPlanetary => SlotSetting(TreatKey.AutoPlanetary, autoPlanetary);
        internal static bool ManualSlotButton => BoolSetting(TreatKey.ManualSlotButton, manualSlotButton);
        internal static bool StationScroll => BoolSetting(TreatKey.StationScroll, stationScroll);
        internal static bool ControlScroll => BoolSetting(TreatKey.ControlScroll, controlScroll);

        internal static int PageSize
        {
            get
            {
                int value = treats.TryGetValue(TreatKey.PageSize, out object treated)
                    ? Convert.ToInt32(treated)
                    : pageSize.Value;
                if (value < 1)
                {
                    return 1;
                }
                return value > MaxSlots ? MaxSlots : value;
            }
        }

        internal static bool ExpandOnBuild => BoolSetting(TreatKey.ExpandOnBuild, expandOnBuild);
        internal static bool ExpandOnOpen => BoolSetting(TreatKey.ExpandOnOpen, expandOnOpen);
        internal static bool ChangeCapacityUI => changeCapacityUi.Value;
        internal static bool ChangePowerUI => changePowerUi.Value;
        internal static bool ChangeMinerSpeedUI => changeMinerSpeedUi.Value;
        internal static bool BreakLimit => breakLimit.Value;
        internal static bool ChangeEnergyMaxUI => changeEnergyMaxUi.Value;
        internal static bool SaveExtraSlots => saveExtraSlots.Value;
        internal static bool AllowDuplicate => allowDuplicate.Value;
        internal static string CatText => cat.Value ?? "";

        private static int SlotSetting(TreatKey key, ConfigEntry<int> entry)
        {
            int value = treats.TryGetValue(key, out object treated) ? Convert.ToInt32(treated) : entry.Value;
            if (value < 0)
            {
                return -1;
            }
            return value > MaxSlots ? MaxSlots : value;
        }

        private static bool BoolSetting(TreatKey key, ConfigEntry<bool> entry)
        {
            return treats.TryGetValue(key, out object treated) ? Convert.ToBoolean(treated) : entry.Value;
        }
    }

    /// <summary>
    /// Api Key to other mod.
    /// This will not set the value in Station Assistant's config file,
    /// but treat as when running.
    /// </summary>
    public static class Treat
    {
        public static void AutoStellarSlots(int value) => Settings.Treat(TreatKey.AutoStellar, value);
        public static void AutoPlanetarySlots(int value) => Settings.Treat(TreatKey.AutoPlanetary, value);
        public static void ManualSlotButton(bool value) => Settings.Treat(TreatKey.ManualSlotButton, value);
        public static void ExpandOnBuild(bool value) => Settings.Treat(TreatKey.ExpandOnBuild, value);
        public static void ExpandOnOpen(bool value) => Settings.Treat(TreatKey.ExpandOnOpen, value);
        public static void StationPanelScroll(bool value) => Settings.Treat(TreatKey.StationScroll, value);
        public static void ControlPanelScroll(bool value) => Settings.Treat(TreatKey.ControlScroll, value);
        public static void PageSize(int value) => Settings.Treat(TreatKey.PageSize, value);
    }
}
