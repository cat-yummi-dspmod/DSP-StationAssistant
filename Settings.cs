using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace StationAssistant
{
    internal enum TreatKey
    {
        AutoStellar,
        AutoPlanetary,
        ManualSlotButton,
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

        private static readonly Dictionary<TreatKey, object> treats = new Dictionary<TreatKey, object>();

        private static ConfigEntry<int> autoStellar;
        private static ConfigEntry<int> autoPlanetary;
        private static ConfigEntry<bool> manualSlotButton;
        private static ConfigEntry<bool> stationScroll;
        private static ConfigEntry<bool> controlScroll;
        private static ConfigEntry<int> pageSize;
        private static ConfigEntry<bool> editCapacity;
        private static ConfigEntry<bool> editPower;
        private static ConfigEntry<bool> editMiner;
        private static ConfigEntry<bool> breakLimit;
        private static ConfigEntry<bool> editEnergyMax;
        private static ConfigEntry<bool> saveExtraSlots;
        private static ConfigEntry<bool> allowDuplicate;
        private static ConfigEntry<string> cat;

        internal static void Bind(ConfigFile config)
        {
            autoStellar = config.Bind("Slots", "AutoStellarSlots", -1,
                "星际物流塔自动货物栏。-1 不改。只增不减，制造和打开面板时生效。上限 32。");
            autoPlanetary = config.Bind("Slots", "AutoPlanetarySlots", -1,
                "行星物流塔自动货物栏。-1 不改。只增不减。上限 32。");
            manualSlotButton = config.Bind("Slots", "ManualSlotButton", true,
                "物流塔面板右上角显示「设置槽位数目」。");
            stationScroll = config.Bind("Slots", "StationPanelScroll", true,
                "物流塔面板按页查看货物栏。");
            controlScroll = config.Bind("Slots", "ControlPanelScroll", true,
                "总控面板里的物流塔储物栏按页查看。");
            pageSize = config.Bind("Slots", "PageSize", 6,
                "滚动时一页最多显示的格子数。");
            editCapacity = config.Bind("Numeric", "DoubleClickCapacity", true,
                "双击物品容量数字。暂时默认开。");
            editPower = config.Bind("Numeric", "DoubleClickPower", true,
                "双击充电功率（workEnergyPerTick）。暂时默认开。");
            editMiner = config.Bind("Numeric", "DoubleClickMinerSpeed", true,
                "双击大型采矿机采集效率。暂时默认开。");
            breakLimit = config.Bind("Numeric", "BreakLimit", true,
                "双击输入的容量、电力、采集效率允许超过原版滑条范围。不影响充电上限。暂时默认开。");
            editEnergyMax = config.Bind("Numeric", "DoubleClickEnergyMax", true,
                "双击电量文字修改 energyMax。暂时默认开，不走破限开关。");
            saveExtraSlots = config.Bind("Save", "ExtraSlotsInModSave", true,
                "超过 6 格的货物栏写入 DSPModSave，原版存档只留 6 格。没装 DSPModSave 时不运行。");
            allowDuplicate = config.Bind("Items", "AllowDuplicateItems", true,
                "允许同一物品进入多个货物栏。暂时默认开。");
            cat = config.Bind("Hidden", "YummiIsAGoodCat", "yes",
                "设成「我是一只猫」时打开隐藏功能入口。功能尚未定义。");
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

        internal static bool EditCapacity => editCapacity.Value;
        internal static bool EditPower => editPower.Value;
        internal static bool EditMiner => editMiner.Value;
        internal static bool BreakLimit => breakLimit.Value;
        internal static bool EditEnergyMax => editEnergyMax.Value;
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
    /// 别的 mod 声明「把这一项当作某个值」。不写 Station Assistant 的配置文件。
    /// </summary>
    public static class Treat
    {
        public static void AutoStellarSlots(int value) => Settings.Treat(TreatKey.AutoStellar, value);
        public static void AutoPlanetarySlots(int value) => Settings.Treat(TreatKey.AutoPlanetary, value);
        public static void ManualSlotButton(bool value) => Settings.Treat(TreatKey.ManualSlotButton, value);
        public static void StationPanelScroll(bool value) => Settings.Treat(TreatKey.StationScroll, value);
        public static void ControlPanelScroll(bool value) => Settings.Treat(TreatKey.ControlScroll, value);
        public static void PageSize(int value) => Settings.Treat(TreatKey.PageSize, value);
    }
}
