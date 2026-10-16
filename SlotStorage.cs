using System;

namespace StationAssistant
{
    internal static class SlotStorage
    {
        internal const int PlanetaryProto = 2103;
        internal const int StellarProto = 2104;
        internal const int VanillaKeep = 6;

        internal static bool IsTower(StationComponent station)
        {
            if (station == null || station.storage == null || station.isCollector || station.isVeinCollector)
            {
                return false;
            }
            PlanetData planet = GameMain.data?.galaxy?.PlanetById(station.planetId);
            PlanetFactory factory = planet?.factory;
            if (factory == null || station.entityId <= 0 || station.entityId >= factory.entityPool.Length)
            {
                return false;
            }
            int protoId = factory.entityPool[station.entityId].protoId;
            return protoId == PlanetaryProto || protoId == StellarProto;
        }

        internal static int AutoTarget(StationComponent station)
        {
            return station.isStellar ? Settings.AutoStellar : Settings.AutoPlanetary;
        }

        internal static void ExpandIfNeeded(StationComponent station)
        {
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
