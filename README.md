# Station Assistant

https://github.com/cat-yummi-dspmod/DSP-StationAssistant

<details>
<summary>English</summary>

Logistics station slots, paging, and optional double-click numeric editing. Full behavior: `需求.md` (Chinese).

- **Scope**: vanilla planetary (2103) and interstellar (2104) logistics towers only, detected by **entity `modelIndex`** (not item `protoId`). Orbital collectors and advanced miners are excluded.
- **SlotsChange**: auto targets default `-1`; `ExpandOnBuild` / `ExpandOnOpen`; manual **Slots: N** button. Cap 32.
- **SlotsScroll**: `StationPanelScroll` / `ControlPanelScroll` are independent. If station paging is off, the tower window shows **all** slots (e.g. 8 rows) even when `PageSize` is 6. `PageSize` applies only when station paging is on.
- **Numeric UI**: `ChangeCapacityUI`, `ChangePowerUI`, `ChangeMinerSpeedUI` default on; `ChangeEnergyMaxUI` and `BreakLimit` default off.
- **Save**: slots beyond 6 go to DSPModSave when `StationAssistant.ModSave` is installed; vanilla save keeps 6.
- **Treat API**: other mods call `StationAssistant.Treat` at runtime without editing this mod’s config.
- **Requires** YummiHarness. **Incompatible** with LogisticsStationSlots.

</details>

<details>
<summary>中文</summary>

物流塔槽位、翻页，以及双击修改数值。详细行为见 `需求.md`。

- **范围**：仅原版行星物流塔（2103）与星际物流塔（2104），按实体 **`modelIndex`（模型）** 判定，不用 `protoId`（物品 ID）。轨道采集器、大矿机不参与。
- **SlotsChange**：自动目标数默认 `-1`；`ExpandOnBuild` / `ExpandOnOpen`；「槽位数目:N」按钮；上限 32。
- **SlotsScroll**：物流塔面板与总控面板互不影响。关闭 `StationPanelScroll` 时塔窗口**一次显示全部槽位**（例如 8 格全显示），不受 `PageSize=6` 限制；`PageSize` 仅在开启塔面板翻页时生效。
- **双击改数**：`ChangeCapacityUI` / `ChangePowerUI` / `ChangeMinerSpeedUI` 默认开；`ChangeEnergyMaxUI` 与 `BreakLimit` 默认关。
- **存档**：超过 6 格在安装 DSPModSave 与 `StationAssistant.ModSave` 时写入 mod 存档，原版存档只留 6 格。
- 别的 mod 用 `StationAssistant.Treat` 声明运行时取值，不写本 mod 的配置文件。
- 需要 **YummiHarness**。与 **LogisticsStationSlots** 互斥。

</details>
