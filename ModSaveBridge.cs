using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace StationAssistant
{
    /// <summary>
    /// 存档前把超过 6 格的货物栏从原版数组里摘下来，存完再装回去。
    /// 摘下来的部分由 StationAssistant.ModSave 写入 DSPModSave。没装那个库时这里不运行。
    /// </summary>
    [HarmonyPatch]
    public static class ModSaveBridge
    {
        internal const string DspModSaveGuid = "crecheng.DSPModSave";

        private struct TailSlot
        {
            public int ItemId;
            public int Count;
            public int Inc;
            public int LocalLogic;
            public int RemoteLogic;
            public int Max;
            public int KeepMode;
            public int KeepInc;
            public byte PriorityIndex;
            public byte LockTick;
        }

        private sealed class Held
        {
            public StationComponent Station;
            public StationStore[] Storage;
            public StationPriorityLock[] Locks;
            public TailSlot[] Tail;
        }

        private static readonly List<Held> held = new List<Held>();

        internal const string CompanionGuid = "com.yummi.dsp.stationassistant.modsave";

        internal static bool Active =>
            Settings.SaveExtraSlots
            && Chainloader.PluginInfos.ContainsKey(DspModSaveGuid)
            && Chainloader.PluginInfos.ContainsKey(CompanionGuid);

        public static void Export(BinaryWriter writer)
        {
            writer.Write(1);
            int count = 0;
            for (int i = 0; i < held.Count; i++)
            {
                if (held[i].Tail != null && held[i].Tail.Length > 0)
                {
                    count++;
                }
            }
            writer.Write(count);
            for (int i = 0; i < held.Count; i++)
            {
                Held item = held[i];
                if (item.Tail == null || item.Tail.Length == 0 || item.Station == null)
                {
                    continue;
                }
                writer.Write(item.Station.planetId);
                writer.Write(item.Station.id);
                writer.Write(item.Station.entityId);
                writer.Write(item.Tail.Length);
                for (int s = 0; s < item.Tail.Length; s++)
                {
                    TailSlot slot = item.Tail[s];
                    writer.Write(slot.ItemId);
                    writer.Write(slot.Count);
                    writer.Write(slot.Inc);
                    writer.Write(slot.LocalLogic);
                    writer.Write(slot.RemoteLogic);
                    writer.Write(slot.Max);
                    writer.Write(slot.KeepMode);
                    writer.Write(slot.KeepInc);
                    writer.Write(slot.PriorityIndex);
                    writer.Write(slot.LockTick);
                }
            }
        }

        public static void Import(BinaryReader reader)
        {
            if (!Active)
            {
                return;
            }
            int version = reader.ReadInt32();
            if (version != 1)
            {
                return;
            }
            int count = reader.ReadInt32();
            GameData data = GameMain.data;
            for (int i = 0; i < count; i++)
            {
                int planetId = reader.ReadInt32();
                int stationId = reader.ReadInt32();
                int entityId = reader.ReadInt32();
                int tailLength = reader.ReadInt32();
                TailSlot[] tail = new TailSlot[tailLength];
                for (int s = 0; s < tailLength; s++)
                {
                    tail[s] = new TailSlot
                    {
                        ItemId = reader.ReadInt32(),
                        Count = reader.ReadInt32(),
                        Inc = reader.ReadInt32(),
                        LocalLogic = reader.ReadInt32(),
                        RemoteLogic = reader.ReadInt32(),
                        Max = reader.ReadInt32(),
                        KeepMode = reader.ReadInt32(),
                        KeepInc = reader.ReadInt32(),
                        PriorityIndex = reader.ReadByte(),
                        LockTick = reader.ReadByte()
                    };
                }
                Restore(data, planetId, stationId, entityId, tail);
            }
        }

        public static void IntoOtherSave()
        {
            held.Clear();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), "SaveCurrentGame")]
        [YummiHarness.Guard("642a7f58031c1335")]
        public static void SaveCurrentGame_Prefix()
        {
            Shrink();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSave), "SaveCurrentGame")]
        [HarmonyPriority(Priority.Last)]
        [YummiHarness.Guard("642a7f58031c1335")]
        public static void SaveCurrentGame_Postfix()
        {
            RestoreLive();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), "AutoSave")]
        [YummiHarness.Guard("1b2c479f0b09b60d")]
        public static void AutoSave_Prefix()
        {
            Shrink();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSave), "AutoSave")]
        [HarmonyPriority(Priority.Last)]
        [YummiHarness.Guard("1b2c479f0b09b60d")]
        public static void AutoSave_Postfix()
        {
            RestoreLive();
        }

        private static void Shrink()
        {
            held.Clear();
            if (!Active || GameMain.data?.factories == null)
            {
                return;
            }
            GameData data = GameMain.data;
            for (int f = 0; f < data.factoryCount; f++)
            {
                PlanetFactory factory = data.factories[f];
                PlanetTransport transport = factory?.transport;
                if (transport?.stationPool == null)
                {
                    continue;
                }
                for (int i = 1; i < transport.stationCursor; i++)
                {
                    StationComponent station = transport.stationPool[i];
                    if (station == null || station.id != i || station.storage == null || station.storage.Length <= SlotStorage.VanillaKeep)
                    {
                        continue;
                    }
                    if (!SlotStorage.IsTower(station))
                    {
                        continue;
                    }
                    int tailLength = station.storage.Length - SlotStorage.VanillaKeep;
                    TailSlot[] tail = new TailSlot[tailLength];
                    for (int s = 0; s < tailLength; s++)
                    {
                        StationStore store = station.storage[SlotStorage.VanillaKeep + s];
                        StationPriorityLock priority = station.priorityLocks != null && SlotStorage.VanillaKeep + s < station.priorityLocks.Length
                            ? station.priorityLocks[SlotStorage.VanillaKeep + s]
                            : default;
                        tail[s] = new TailSlot
                        {
                            ItemId = store.itemId,
                            Count = store.count,
                            Inc = store.inc,
                            LocalLogic = (int)store.localLogic,
                            RemoteLogic = (int)store.remoteLogic,
                            Max = store.max,
                            KeepMode = store.keepMode,
                            KeepInc = (int)(store.keepIncRatio * 100000f),
                            PriorityIndex = priority.priorityIndex,
                            LockTick = priority.lockTick
                        };
                    }
                    held.Add(new Held
                    {
                        Station = station,
                        Storage = station.storage,
                        Locks = station.priorityLocks,
                        Tail = tail
                    });
                    StationStore[] head = new StationStore[SlotStorage.VanillaKeep];
                    Array.Copy(station.storage, head, SlotStorage.VanillaKeep);
                    StationPriorityLock[] headLocks = new StationPriorityLock[SlotStorage.VanillaKeep];
                    if (station.priorityLocks != null)
                    {
                        Array.Copy(station.priorityLocks, headLocks, Math.Min(station.priorityLocks.Length, SlotStorage.VanillaKeep));
                    }
                    station.storage = head;
                    station.priorityLocks = headLocks;
                }
            }
        }

        private static void RestoreLive()
        {
            for (int i = 0; i < held.Count; i++)
            {
                Held item = held[i];
                if (item.Station == null)
                {
                    continue;
                }
                item.Station.storage = item.Storage;
                item.Station.priorityLocks = item.Locks;
            }
        }

        private static void Restore(GameData data, int planetId, int stationId, int entityId, TailSlot[] tail)
        {
            if (data?.galaxy == null || tail == null || tail.Length == 0)
            {
                return;
            }
            PlanetData planet = data.galaxy.PlanetById(planetId);
            PlanetFactory factory = planet?.factory;
            if (factory?.transport?.stationPool == null || stationId <= 0 || stationId >= factory.transport.stationPool.Length)
            {
                return;
            }
            StationComponent station = factory.transport.stationPool[stationId];
            if (station == null || station.id != stationId || station.entityId != entityId || station.storage == null)
            {
                return;
            }
            if (station.storage.Length != SlotStorage.VanillaKeep)
            {
                return;
            }
            int count = SlotStorage.VanillaKeep + tail.Length;
            if (count > Settings.MaxSlots)
            {
                count = Settings.MaxSlots;
            }
            StationStore[] storage = new StationStore[count];
            Array.Copy(station.storage, storage, SlotStorage.VanillaKeep);
            StationPriorityLock[] locks = new StationPriorityLock[count];
            if (station.priorityLocks != null)
            {
                Array.Copy(station.priorityLocks, locks, Math.Min(station.priorityLocks.Length, SlotStorage.VanillaKeep));
            }
            int usable = count - SlotStorage.VanillaKeep;
            for (int i = 0; i < usable && i < tail.Length; i++)
            {
                TailSlot slot = tail[i];
                storage[SlotStorage.VanillaKeep + i] = new StationStore
                {
                    itemId = slot.ItemId,
                    count = slot.Count,
                    inc = slot.Inc,
                    localLogic = (ELogisticStorage)slot.LocalLogic,
                    remoteLogic = (ELogisticStorage)slot.RemoteLogic,
                    max = slot.Max,
                    keepMode = slot.KeepMode,
                    keepIncRatio = slot.KeepInc * 1E-05f
                };
                locks[SlotStorage.VanillaKeep + i] = new StationPriorityLock
                {
                    priorityIndex = slot.PriorityIndex,
                    lockTick = slot.LockTick
                };
            }
            station.storage = storage;
            station.priorityLocks = locks;
        }
    }
}
