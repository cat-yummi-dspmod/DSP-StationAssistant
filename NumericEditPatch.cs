using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class NumericEditPatch
    {
        private static int refreshDepth;
        private static int pendingMax = int.MinValue;
        // 只有双击输入容量时才破限；其他 mod（如塔厂传 int.MaxValue 期望被原版钳制）不受影响。
        private static bool userEdit;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        [CatAPI.Guard("1eb74a6188c74249")]
        public static void SetStationStorage_Prefix(int itemCountMax)
        {
            pendingMax = Settings.BreakLimit && userEdit ? itemCountMax : int.MinValue;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        [CatAPI.Guard("1eb74a6188c74249")]
        public static void SetStationStorage_Postfix(PlanetTransport __instance, int stationId, int storageIdx)
        {
            if (pendingMax == int.MinValue)
            {
                return;
            }
            int value = pendingMax;
            pendingMax = int.MinValue;
            StationComponent station = __instance.GetStationComponent(stationId);
            if (station == null || station.storage == null || storageIdx < 0 || storageIdx >= station.storage.Length)
            {
                return;
            }
            station.storage[storageIdx].max = value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), "_OnUpdate")]
        [CatAPI.Guard("957c0ae7be9f0a0b")]
        public static void Storage_OnUpdate(UIStationStorage __instance)
        {
            if (!Settings.ChangeCapacityUI || __instance.station == null || __instance.maxValueText == null)
            {
                return;
            }
            Attach(__instance, __instance.maxValueText, () => EditCapacity(__instance.station, __instance.index, __instance.stationWindow.transport));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "_OnUpdate")]
        [CatAPI.Guard("5fb4930620470ae4")]
        public static void PanelStorage_OnUpdate(UIControlPanelStationStorage __instance)
        {
            if (!Settings.ChangeCapacityUI || !__instance.isStationDataValid || __instance.maxValueText == null
                || __instance.station == null)
            {
                return;
            }
            Attach(__instance, __instance.maxValueText, () => EditCapacity(__instance.station, __instance.index, __instance.masterInspector.transport));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        [CatAPI.Guard("94abf08893f0dfc3")]
        public static void Refresh_Prefix()
        {
            if (Settings.ChangeCapacityUI)
            {
                refreshDepth++;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        [CatAPI.Guard("94abf08893f0dfc3")]
        public static void Refresh_Postfix()
        {
            if (Settings.ChangeCapacityUI)
            {
                refreshDepth--;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "RefreshValues")]
        [CatAPI.Guard("dd4005879a70d9e4")]
        public static void PanelRefresh_Prefix()
        {
            if (Settings.ChangeCapacityUI)
            {
                refreshDepth++;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "RefreshValues")]
        [CatAPI.Guard("dd4005879a70d9e4")]
        public static void PanelRefresh_Postfix()
        {
            if (Settings.ChangeCapacityUI)
            {
                refreshDepth--;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), "OnMaxSliderValueChange")]
        [CatAPI.Guard("a168bac3333c06e9")]
        public static bool Slider_Prefix()
        {
            return !Settings.ChangeCapacityUI || refreshDepth <= 0;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "OnMaxSliderValueChange")]
        [CatAPI.Guard("7aa31034fac15357")]
        public static bool PanelSlider_Prefix()
        {
            return !Settings.ChangeCapacityUI || refreshDepth <= 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        [CatAPI.Guard("6c606ffa0548b6f2")]
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
            // 覆盖层每个窗口只建一次，回调里必须重新取当前站，不能捕获建层时的 station。
            if (Settings.ChangePowerUI && __instance.maxChargePowerValue != null)
            {
                Attach(__instance, __instance.maxChargePowerValue, () =>
                {
                    StationComponent s = Current(__instance.transport, __instance.stationId);
                    if (s != null)
                    {
                        EditPower(__instance.factory, s, __instance.maxChargePowerSlider, __instance.maxChargePowerValue);
                    }
                });
            }
            if (Settings.ChangeMinerSpeedUI && __instance.maxMiningSpeedValue != null)
            {
                Attach(__instance.maxMiningSpeedValue, __instance.maxMiningSpeedValue, () =>
                {
                    StationComponent s = Current(__instance.transport, __instance.stationId);
                    if (s != null)
                    {
                        EditMiner(__instance.factory, s, __instance.maxMiningSpeedSlider, __instance.maxMiningSpeedValue);
                    }
                });
            }
            if (Settings.ChangeEnergyMaxUI && __instance.energyText != null)
            {
                Attach(__instance.energyText, __instance.energyText, () =>
                {
                    StationComponent s = Current(__instance.transport, __instance.stationId);
                    if (s != null)
                    {
                        EditEnergy(s);
                    }
                });
            }
        }

        private static StationComponent Current(PlanetTransport transport, int stationId)
        {
            if (transport == null || stationId <= 0 || stationId >= transport.stationPool.Length)
            {
                return null;
            }
            StationComponent s = transport.stationPool[stationId];
            return s != null && s.id == stationId ? s : null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "_OnUpdate")]
        [CatAPI.Guard("b89aade680ae7db9")]
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
            if (Settings.ChangePowerUI && __instance.maxChargePowerValue != null)
            {
                Attach(__instance, __instance.maxChargePowerValue, () =>
                {
                    StationComponent s = Current(__instance.transport, __instance.stationId);
                    if (s != null)
                    {
                        EditPower(__instance.factory, s, __instance.maxChargePowerSlider, __instance.maxChargePowerValue);
                    }
                });
            }
            if (Settings.ChangeMinerSpeedUI && __instance.maxMiningSpeedValue != null)
            {
                Attach(__instance.maxMiningSpeedValue, __instance.maxMiningSpeedValue, () =>
                {
                    StationComponent s = Current(__instance.transport, __instance.stationId);
                    if (s != null)
                    {
                        EditMiner(__instance.factory, s, __instance.maxMiningSpeedSlider, __instance.maxMiningSpeedValue);
                    }
                });
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMinerWindow), "_OnUpdate")]
        [CatAPI.Guard("58681ea0364989f7")]
        public static void Miner_OnUpdate(UIMinerWindow __instance)
        {
            if (!Settings.ChangeMinerSpeedUI || __instance.minerId == 0 || __instance.factory == null || __instance.speedText == null)
            {
                return;
            }
            Attach(__instance, __instance.speedText, () =>
            {
                int speed = __instance.factorySystem.minerPool[__instance.minerId].speed;
                PopNumber.Show(CatAPI.Lang.En("Mining speed").ZhCN("采集效率"), speed.ToString(), FieldValue("MinerComponent.speed", "速度值（界面百分比 = 该值 / 100）", "speed (shown percent = value / 100)", speed), InputField.ContentType.IntegerNumber, text =>
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
            if (station == null || station.storage == null || index < 0 || index >= station.storage.Length)
            {
                return;
            }
            int current = station.storage[index].max;
            PopNumber.Show(CatAPI.Lang.En("Item capacity").ZhCN("物品容量"), current.ToString(), FieldValue("StationStore.max", "个", "items", current), InputField.ContentType.IntegerNumber, text =>
            {
                if (!int.TryParse(text.Trim(), out int value) || value < 0)
                {
                    return;
                }
                StationStore store = station.storage[index];
                userEdit = true;
                try
                {
                    transport.SetStationStorage(station.id, index, store.itemId, value, store.localLogic, store.remoteLogic, GameMain.mainPlayer);
                }
                finally
                {
                    userEdit = false;
                }
            });
        }

        private static void EditPower(PlanetFactory factory, StationComponent station, Slider slider, Text label)
        {
            if (factory == null || station.pcId <= 0 || station.pcId >= factory.powerSystem.consumerCursor)
            {
                return;
            }
            long current = factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick;
            PopNumber.Show(CatAPI.Lang.En("Charge power").ZhCN("充电功率"), current.ToString(), FieldValue("workEnergyPerTick", "焦耳每帧", "joules per tick", current), InputField.ContentType.IntegerNumber, text =>
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
                if (label != null)
                {
                    label.text = FormatChargePowerLabel(value);
                }
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
            PopNumber.Show(CatAPI.Lang.En("Mining speed").ZhCN("采集效率"), current.ToString(), FieldValue("MinerComponent.speed", "速度值（界面百分比 = 该值 / 100）", "speed (shown percent = value / 100)", current), InputField.ContentType.IntegerNumber, text =>
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
                if (slider != null)
                {
                    float shown = (miner.speed - 10000) / 1000f;
                    if (shown >= slider.minValue && shown <= slider.maxValue)
                    {
                        slider.SetValueWithoutNotify(shown);
                    }
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
            PopNumber.Show(CatAPI.Lang.En("Charge cap").ZhCN("充电上限"), station.energyMax.ToString(), FieldValue("energyMax", "焦耳", "joules", station.energyMax), InputField.ContentType.IntegerNumber, text =>
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

        private static string FormatChargePowerLabel(long workEnergyPerTick)
        {
            // Same template as UIStationWindow.powerServedSB — WriteKMG writes backward from lastIndex.
            System.Text.StringBuilder sb = new System.Text.StringBuilder("         W", 12);
            StringBuilderUtility.WriteKMG(sb, 8, workEnergyPerTick * 60, true);
            return sb.ToString();
        }

        private static string FieldValue(string variable, string unitZh, string unitEn, long current)
        {
            return CatAPI.Lang.En($"In-game variable: {variable}\nUnit: {unitEn}\nCurrent value: {current}")
                .ZhCN($"游戏内的变量名为 {variable}\n单位是{unitZh}\n目前的值为 {current}");
        }

        private static void Attach(Component owner, Text label, System.Action action)
        {
            if (label == null)
            {
                return;
            }
            // UI refresh can destroy the overlay; only skip when the child still exists.
            if (label.transform.Find("station-assistant-dbl") != null)
            {
                return;
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
