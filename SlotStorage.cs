using System;

namespace StationAssistant
{
    internal static class SlotStorage
    {
        internal const int PlanetaryItemId = 2103;
        internal const int StellarItemId = 2104;
        internal const int VanillaKeep = 6;

        /// <summary>轨道采集器、大矿机等：不参与槽位扩容、翻页、手动改槽位。</summary>
        internal static bool IsCollectorOrVeinCollector(StationComponent station)
        {
            return station != null && (station.isCollector || station.isVeinCollector);
        }

        /// <summary>
        /// 按实体上的 modelIndex（模型）判定，不用 entity.protoId（物品 ID），避免蓝图 bug 导致物品与模型分离。
        /// 是否「原版行星/星际物流塔」：模型索引落在 LDB 物品 2103/2104 注册的 ModelIndex 范围内。
        /// </summary>
        internal static bool TryGetLogisticsTowerKind(StationComponent station, out bool stellar)
        {
            stellar = false;
            if (station == null || station.storage == null || IsCollectorOrVeinCollector(station))
            {
                return false;
            }
            PlanetFactory factory = FindFactory(station);
            if (factory == null || station.entityId <= 0 || station.entityId >= factory.entityPool.Length)
            {
                return Fallback(station, out stellar);
            }
            int modelIndex = factory.entityPool[station.entityId].modelIndex;
            ModelProto model = LDB.models.Select(modelIndex);
            PrefabDesc desc = model?.prefabDesc;
            if (desc != null && (desc.isCollectStation || desc.isVeinCollector))
            {
                return false;
            }
            if (IsVanillaItemModel(modelIndex, StellarItemId))
            {
                stellar = true;
                return true;
            }
            if (IsVanillaItemModel(modelIndex, PlanetaryItemId))
            {
                stellar = false;
                return true;
            }
            // 兜底：模型范围未命中、或被其他 mod 改动而扫不出时，仍视为物流塔（排除采集器后），
            // 以保证至少能手动设置槽位数目。
            if (desc != null && desc.isStation)
            {
                stellar = desc.isStellarStation;
                return true;
            }
            return Fallback(station, out stellar);
        }

        private static bool Fallback(StationComponent station, out bool stellar)
        {
            stellar = station.isStellar;
            return true;
        }

        /// <summary>行星内物流塔的 planetId 恒为 0（只有星际塔会写入），所以要靠 stationPool 反查所在工厂。</summary>
        private static PlanetFactory FindFactory(StationComponent station)
        {
            GameData data = GameMain.data;
            if (data == null)
            {
                return null;
            }
            if (station.planetId > 0)
            {
                PlanetFactory byPlanet = data.galaxy?.PlanetById(station.planetId)?.factory;
                if (byPlanet != null)
                {
                    return byPlanet;
                }
            }
            PlanetFactory local = data.localPlanet?.factory;
            if (Owns(local, station))
            {
                return local;
            }
            if (data.factories != null)
            {
                for (int i = 0; i < data.factoryCount && i < data.factories.Length; i++)
                {
                    if (Owns(data.factories[i], station))
                    {
                        return data.factories[i];
                    }
                }
            }
            return null;
        }

        private static bool Owns(PlanetFactory factory, StationComponent station)
        {
            if (factory == null || factory.transport == null || factory.transport.stationPool == null)
            {
                return false;
            }
            StationComponent[] pool = factory.transport.stationPool;
            return station.id > 0 && station.id < pool.Length && ReferenceEquals(pool[station.id], station);
        }

        private static bool IsVanillaItemModel(int modelIndex, int itemId)
        {
            ItemProto item = LDB.items.Select(itemId);
            if (item == null || item.ModelCount < 1)
            {
                return false;
            }
            return modelIndex >= item.ModelIndex && modelIndex < item.ModelIndex + item.ModelCount;
        }

        internal static bool IsTower(StationComponent station)
        {
            return TryGetLogisticsTowerKind(station, out _);
        }

        internal static int AutoTarget(StationComponent station)
        {
            if (!TryGetLogisticsTowerKind(station, out bool stellar))
            {
                return -1;
            }
            return stellar ? Settings.AutoStellar : Settings.AutoPlanetary;
        }

        internal static void ExpandIfNeeded(StationComponent station, bool onBuild)
        {
            if (onBuild ? !Settings.ExpandOnBuild : !Settings.ExpandOnOpen)
            {
                return;
            }
            if (!IsTower(station))
            {
                return;
            }
            int target = AutoTarget(station);
            if (target < 0 || station.storage.Length >= target)
            {
                return;
            }
            Resize(station, target);
        }

        internal static void SetCount(PlanetFactory factory, StationComponent station, int count)
        {
            if (!IsTower(station) || factory == null)
            {
                return;
            }
            if (count < 1)
            {
                count = 1;
            }
            if (count > Settings.MaxSlots)
            {
                count = Settings.MaxSlots;
            }
            if (count == station.storage.Length)
            {
                return;
            }
            if (count < station.storage.Length)
            {
                for (int i = station.storage.Length - 1; i >= count; i--)
                {
                    factory.transport.SetStationStorage(
                        station.id, i, 0, 0, ELogisticStorage.None, ELogisticStorage.None, GameMain.mainPlayer);
                }
            }
            Resize(station, count);
        }

        internal static void Resize(StationComponent station, int count)
        {
            StationStore[] storage = new StationStore[count];
            Array.Copy(station.storage, storage, Math.Min(station.storage.Length, count));
            StationPriorityLock[] locks = new StationPriorityLock[count];
            if (station.priorityLocks != null)
            {
                Array.Copy(station.priorityLocks, locks, Math.Min(station.priorityLocks.Length, count));
            }
            lock (station.storage)
            {
                station.priorityLocks = locks;
                station.storage = storage;
            }
        }
    }

    internal static class Pages
    {
        internal static int Count(int slots, int pageSize)
        {
            if (pageSize < 1)
            {
                pageSize = 1;
            }
            if (slots <= pageSize)
            {
                return 1;
            }
            return (slots + pageSize - 1) / pageSize;
        }

        internal static int Start(int page, int slots, int pageSize)
        {
            int pages = Count(slots, pageSize);
            if (page < 0)
            {
                page = 0;
            }
            if (page >= pages)
            {
                page = pages - 1;
            }
            if (pages <= 1)
            {
                return 0;
            }
            if (page >= pages - 1)
            {
                return slots - pageSize;
            }
            return page * pageSize;
        }
    }
}
