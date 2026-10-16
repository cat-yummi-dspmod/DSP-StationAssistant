# Station Assistant（未完成，不建议使用）

物流塔槽位、翻页，以及默认关闭的双击数值。详细行为见 `需求.md`。

- 只处理行星物流运输站（2103）和星际物流运输站（2104）。
- 自动扩容默认 -1（不改），只增不减。手动「设置槽位数目」默认开。货物栏上限 32。
- 物流塔面板和总控面板默认可翻页，单页 6 格。最后一页凑满一页。只有一页时不翻。
- 双击物品容量、充电功率、采集效率、破限、充电上限默认关。
- 超过 6 格的货物栏在安装了 DSPModSave 和 `StationAssistant.ModSave` 时写入 mod 存档，原版存档只留 6 格。
- 别的 mod 用 `StationAssistant.Treat` 声明运行时取值，不写本 mod 的配置文件。
- 与 LogisticsStationSlots 互斥。需要 YummiHarness。

Logistics station slots and paging. See `需求.md` for the full behavior. Requires YummiHarness. Incompatible with LogisticsStationSlots.
