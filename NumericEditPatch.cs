using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class NumericEditPatch
    {
        private static readonly ConditionalWeakTable<Component, object> hooked = new ConditionalWeakTable<Component, object>();
        private static readonly object mark = new object();
        private static int refreshDepth;
        private static int pendingMax = int.MinValue;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        [YummiHarness.Guard("1eb74a6188c74249")]
        public static void SetStationStorage_Prefix(int itemCountMax)
        {
            pendingMax = Settings.BreakLimit ? itemCountMax : int.MinValue;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        [YummiHarness.Guard("1eb74a6188c74249")]
        public static void SetStationStorage_Postfix(PlanetTransport __instance, int stationId, int storageIdx)
        {
            if (pendingMax == int.MinValue)
            {
                return;
            }
            int value = pendingMax;
            pendingMax = int.MinValue;
            StationComponent station = __instance.GetStationComponent(stationId);
            if (station?.storage == null || storageIdx < 0 || storageIdx >= station.storage.Length)
            {
                return;
            }
            station.storage[storageIdx].max = value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), "_OnUpdate")]
        [YummiHarness.Guard("957c0ae7be9f0a0b")]
        public static void Storage_OnUpdate(UIStationStorage __instance)
        {
            if (!Settings.EditCapacity || __instance.station == null || __instance.maxValueText == null)
            {
                return;
            }
            Attach(__instance, __instance.maxValueText, () => EditCapacity(__instance.station, __instance.index, __instance.stationWindow.transport));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "_OnUpdate")]
        [YummiHarness.Guard("5fb4930620470ae4")]
        public static void PanelStorage_OnUpdate(UIControlPanelStationStorage __instance)
        {
            if (!Settings.EditCapacity || !__instance.isStationDataValid || __instance.maxValueText == null)
            {
                return;
            }
            Attach(__instance, __instance.maxValueText, () => EditCapacity(__instance.station, __instance.index, __instance.masterInspector.transport));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        [YummiHarness.Guard("94abf08893f0dfc3")]
        public static void Refresh_Prefix()
        {
            if (Settings.EditCapacity)
            {
                refreshDepth++;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        [YummiHarness.Guard("94abf08893f0dfc3")]
        public static void Refresh_Postfix()
        {
            if (Settings.EditCapacity)
            {
                refreshDepth--;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "RefreshValues")]
        [YummiHarness.Guard("dd4005879a70d9e4")]
        public static void PanelRefresh_Prefix()
        {
            if (Settings.EditCapacity)
            {
                refreshDepth++;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "RefreshValues")]
        [YummiHarness.Guard("dd4005879a70d9e4")]
        public static void PanelRefresh_Postfix()
        {
            if (Settings.EditCapacity)
            {
                refreshDepth--;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), "OnMaxSliderValueChange")]
        [YummiHarness.Guard("a168bac3333c06e9")]
        public static bool Slider_Prefix()
        {
            return !Settings.EditCapacity || refreshDepth <= 0;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "OnMaxSliderValueChange")]
        [YummiHarness.Guard("7aa31034fac15357")]
        public static bool PanelSlider_Prefix()
        {
            return !Settings.EditCapacity || refreshDepth <= 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        [YummiHarness.Guard("6c606ffa0548b6f2")]
        public static void Window_OnUpdate(UIStationWindow __instance)
        {
            if (__instance.stationId == 0 || __instance.transport == null)
            {
                return;
            }
            StationComponent station = __instance.transport.stationPool[__instance.stationId];
            if (station == null || station.id != __instance.stationId)
            {
                return;
            }
            if (Settings.EditPower && __instance.maxChargePowerValue != null)
            {
                Attach(__instance, __instance.maxChargePowerValue, () => EditPower(__instance.factory, station, __instance.maxChargePowerSlider));
            }
            if (Settings.EditMiner && __instance.maxMiningSpeedValue != null)
            {
                Attach(__instance.maxMiningSpeedValue, __instance.maxMiningSpeedValue, () => EditMiner(__instance.factory, station, __instance.maxMiningSpeedSlider, __instance.maxMiningSpeedValue));
            }
            if (Settings.EditEnergyMax && __instance.energyText != null)
            {
                Attach(__instance.energyText, __instance.energyText, () => EditEnergy(station));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "_OnUpdate")]
        [YummiHarness.Guard("b89aade680ae7db9")]
        public static void Inspector_OnUpdate(UIControlPanelStationInspector __instance)
        {
            if (__instance.stationId == 0 || __instance.factory == null || __instance.transport == null)
            {
                return;
            }
            StationComponent station = __instance.transport.stationPool[__instance.stationId];
            if (station == null || station.id != __instance.stationId)
            {
                return;
            }
            if (Settings.EditPower && __instance.maxChargePowerValue != null)
            {
                Attach(__instance, __instance.maxChargePowerValue, () => EditPower(__instance.factory, station, __instance.maxChargePowerSlider));
            }
            if (Settings.EditMiner && __instance.maxMiningSpeedValue != null)
            {
                Attach(__instance.maxMiningSpeedValue, __instance.maxMiningSpeedValue, () => EditMiner(__instance.factory, station, __instance.maxMiningSpeedSlider, __instance.maxMiningSpeedValue));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMinerWindow), "_OnUpdate")]
        [YummiHarness.Guard("58681ea0364989f7")]
        public static void Miner_OnUpdate(UIMinerWindow __instance)
        {
            if (!Settings.EditMiner || __instance.minerId == 0 || __instance.factory == null || __instance.speedText == null)
            {
                return;
            }
            Attach(__instance, __instance.speedText, () =>
            {
                int speed = __instance.factorySystem.minerPool[__instance.minerId].speed;
                NumericEditDialog.Show("采集效率", speed.ToString(), "MinerComponent.speed", InputField.ContentType.IntegerNumber, text =>
                {
                    if (!int.TryParse(text.Trim(), out int value))
                    {
                        return;
                    }
                    MinerComponent miner = __instance.factorySystem.minerPool[__instance.minerId];
                    miner.speed = ClampMiner(value, null);
                    __instance.factorySystem.minerPool[__instance.minerId] = miner;
                });
            });
        }

        private static void EditCapacity(StationComponent station, int index, PlanetTransport transport)
        {
            if (station == null || index < 0 || index >= station.storage.Length)
            {
                return;
            }
            int current = station.storage[index].max;
            NumericEditDialog.Show("物品容量", current.ToString(), "StationStore.max", InputField.ContentType.IntegerNumber, text =>
            {
                if (!int.TryParse(text.Trim(), out int value) || value < 0)
                {
                    return;
                }
                StationStore store = station.storage[index];
                transport.SetStationStorage(station.id, index, store.itemId, value, store.localLogic, store.remoteLogic, GameMain.mainPlayer);
            });
        }

        private static void EditPower(PlanetFactory factory, StationComponent station, Slider slider)
        {
            if (factory == null || station.pcId <= 0)
            {
                return;
            }
            long current = factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick;
            NumericEditDialog.Show("充电功率", current.ToString(), "workEnergyPerTick，单位 J/tick", InputField.ContentType.IntegerNumber, text =>
            {
                if (!long.TryParse(text.Trim(), out long value) || value < 0)
                {
                    return;
                }
                if (!Settings.BreakLimit && slider != null)
                {
                    long min = (long)(slider.minValue * 50000.0);
                    long max = (long)(slider.maxValue * 50000.0);
                    if (value < min)
                    {
                        value = min;
                    }
                    if (value > max)
                    {
                        value = max;
                    }
                }
                factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick = value;
                if (slider != null)
                {
                    float shown = (float)(value / 50000.0);
                    if (shown >= slider.minValue && shown <= slider.maxValue)
                    {
                        slider.SetValueWithoutNotify(shown);
                    }
                }
            });
        }

        private static void EditMiner(PlanetFactory factory, StationComponent station, Slider slider, Text label)
        {
            if (factory == null || !station.isVeinCollector || station.minerId <= 0 || station.minerId >= factory.factorySystem.minerCursor)
            {
                return;
            }
            int current = factory.factorySystem.minerPool[station.minerId].speed;
            NumericEditDialog.Show("采集效率", current.ToString(), "MinerComponent.speed", InputField.ContentType.IntegerNumber, text =>
            {
                if (!int.TryParse(text.Trim(), out int value))
                {
                    return;
                }
                MinerComponent miner = factory.factorySystem.minerPool[station.minerId];
                miner.speed = ClampMiner(value, slider);
                factory.factorySystem.minerPool[station.minerId] = miner;
                if (label != null)
                {
                    label.text = (miner.speed / 100f).ToString("0") + " %";
                }
            });
        }

        private static int ClampMiner(int value, Slider slider)
        {
            if (Settings.BreakLimit || slider == null)
            {
                return value;
            }
            int min = Mathf.RoundToInt(slider.minValue * 1000f + 10000f);
            int max = Mathf.RoundToInt(slider.maxValue * 1000f + 10000f);
            if (value < min)
            {
                return min;
            }
            return value > max ? max : value;
        }

        private static void EditEnergy(StationComponent station)
        {
            NumericEditDialog.Show("充电上限", station.energyMax.ToString(), "StationComponent.energyMax。不走破限开关。", InputField.ContentType.IntegerNumber, text =>
            {
                if (!long.TryParse(text.Trim(), out long value) || value < 1)
                {
                    return;
                }
                station.energyMax = value;
                if (station.energy > station.energyMax)
                {
                    station.energy = station.energyMax;
                }
            });
        }

        private static void Attach(Component owner, Text label, System.Action action)
        {
            if (label == null || hooked.TryGetValue(label, out _))
            {
                return;
            }
            if (owner != label)
            {
                hooked.Add(label, mark);
            }
            else if (hooked.TryGetValue(owner, out _))
            {
                return;
            }
            else
            {
                hooked.Add(owner, mark);
            }
            GameObject go = new GameObject("station-assistant-dbl", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(label.transform, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            float last = 0f;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                float now = Time.unscaledTime;
                if (now - last <= 0.35f)
                {
                    last = 0f;
                    action();
                }
                else
                {
                    last = now;
                }
            });
        }
    }
}
