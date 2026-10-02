# AdofaiPro

一个 *A Dance of Fire and Ice* (ADOFAI) 的 Unity Mod Manager 模组，演示**如何给编辑器新增一种关卡事件**。

新增的事件叫 **`SetTileTint`**（界面上显示为 *Tile Tint*）：把事件所在砖（以及后续若干砖）染成指定颜色。
它和 `Twirl` / `SetSpeed` 一样，是能出现在事件面板、能放到砖上、能编辑属性、能读写 `.adofai` 的**一等事件**。

> 目标游戏版本：ADOFAI v3.3.1 (r148)。开发环境：Linux + .NET SDK（SDK 风格工程，`dotnet build` 即可）。

---

## 1. 它是怎么实现的（ADOFAI 事件系统要点）

ADOFAI 的事件系统是**数据驱动**的，没有 per-event 的 C# 类：

| 概念 | 类型 | 说明 |
|---|---|---|
| 事件类型 | `ADOFAI.LevelEventType`（enum） | 编译期固定 0..65，**Mod 无法新增枚举成员** |
| 类型元数据 | `ADOFAI.LevelEventInfo` | `name / propertiesInfo / categories / executionTime / isDecoration / …` |
| 字段元数据 | `ADOFAI.PropertyInfo` | 每个可编辑字段（`type`、`default`、`min/max`、`canBeDisabled`、`encode`、`affectsFloors`…） |
| 事件实例 | `ADOFAI.LevelEvent` | `floor` + `eventType` + `data` 字典 + `disabled` 字典 |

启动时 `ADOStartup.SetupLevelEventsInfo()` 从 Unity 资源 `LevelEditorProperties` 解码出所有
`LevelEventInfo`，存进 `GCS.levelEventsInfo`；图标存 `GCS.levelEventIcons`；enum↔字符串存 `GCS.levelEventTypeString`。

### 本模组的做法

因为枚举不能扩展，`SetTileTint` 使用枚举外的数值 **900** 作为 `LevelEventType`，并：

1. **注册元数据** —— `CustomEvent.BuildInfo()` 建一条 `LevelEventInfo`，塞进 `GCS.levelEventsInfo["900"]`，
   同时 `GCS.levelEventTypeString[900] = "900"`、`GCS.levelEventIcons[900] = <运行时生成的图标>`。
2. **认识事件名** —— `RDUtils.ParseEnum<LevelEventType>` 是解析事件名到类型的唯一入口
   （`scnEditor.LoadEditorProperties` 与 `LevelEvent.Decode` 都用它），补丁把
   `"SetTileTint"` / `"900"` 映射到 `(LevelEventType)900`。
3. **读档** —— `LevelEvent.Decode` 前缀把文件里的 `"SetTileTint"` 换成内部键 `"900"`
   （`text = explicitEventType ?? dict["eventType"]`，所以只改 `explicitEventType` 即可，其余原版逻辑照跑）。
4. **存档** —— `LevelEvent.Encode` 后置补丁把 `"900"` 换回可读的 `"SetTileTint"`，让 `.adofai` 可读、可分享。
5. **编辑器出现按钮** —— `scnEditor.LoadEditorProperties` 前缀在游戏遍历 `GCS.levelEventsInfo`
   生成按钮**之前**完成注册与图标注入。类别是 `LevelEventCategory.VisualFx`，所以它会出现在 Visual FX 页签。
6. **编辑器编辑属性** —— 不需要任何补丁：原版 `PropertiesPanel` 会遍历 `info.propertiesInfo`
   自动渲染 `Color` / `Float` / `Int` 控件，改值写进 `LevelEvent.data`，保存时由原版 `Encode` 落盘。
7. **运行时效果** —— 原版 `scnGame.ApplyEventsToFloors` 的 `switch (eventType)` 不认识 900，
   会静默忽略；本模组在其**后置补丁**里读事件并染色。编辑器（`affectsFloors: true`）与游玩时都会重新应用。

### 文件

| 文件 | 作用 |
|---|---|
| `src/CustomEvent.cs` | 事件元数据定义、注册/注销、运行时生成图标 |
| `src/HexColor.cs` | 颜色字段（`"RRGGBB"`）解析 |
| `src/Patching/ParseEnumPatch.cs` | 让 `ParseEnum<LevelEventType>` 认识事件名 |
| `src/Patching/DecodeEncodePatches.cs` | 读档翻译 / 存档写回可读事件名 |
| `src/Patching/EditorPatches.cs` | 注册时机、图标兜底、事件标题本地化 |
| `src/Patching/RuntimePatches.cs` | `ApplyEventsToFloors` 后置补丁：染色效果 |

---

## 2. 事件字段

| 字段 | 类型 | 默认 | 说明 |
|---|---|---|---|
| `floor` | Int | 0 | 砖序号（原版提供“跳转到砖”按钮） |
| `tintColor` | Color | `ffffff` | 染色（不含 alpha，`usesAlpha: false`） |
| `tintOpacity` | Float | 100 | 0..100，乘到颜色 alpha 上 |
| `tilesAhead` | Int | 0 | 额外影响后面多少块砖 |

`.adofai` 里长这样：

```json
{ "floor": 12, "eventType": "SetTileTint", "tintColor": "ff3c50", "tintOpacity": 80, "tilesAhead": 3 }
```

---

## 3. 构建与部署

```bash
make                 # Debug 构建
make release         # Release 构建
make deploy          # 构建并复制到游戏 Mods/AdofaiPro/
make package         # 生成 dist/AdofaiPro/（含 Info.json + dll，可直接 zip 安装）
make clean
```

可覆盖的变量：

```bash
make MANAGED_DIR=/path/to/Managed MODS_DIR=/path/to/game/Mods
```

默认值：

- `MANAGED_DIR = /home/argvchs/workspace/Managed`
- `MODS_DIR    = ~/.steam/steam/steamapps/common/A Dance of Fire and Ice/Mods`

工程直接引用游戏 Managed 目录里的 `Assembly-CSharp.dll`、`UnityEngine*.dll`、
`UnityModManager/*.dll`、`DOTween.dll`、`RDTools.dll`，不复制、不提交到仓库。

---

## 4. 编辑器集成细节（底部栏按钮 / 属性面板）

原版编辑器有两处逻辑是「按 `LevelEventType` 枚举硬编码」的，本事件（枚举外的 900）需要额外照顾：

1. **底部添加栏的按钮位置** —— `scnEditor.LoadEditorProperties` 按 `GCS.levelEventsInfo` 的
   **枚举顺序**给每个类别的按钮编号：`page = 序号 / 11`、`keyCode = 序号 % 11 + 1`。
   注册项若排在末尾，按钮会落到该类别页签的最后一页（默认看不到）。
   本模组注册后会把 `SetTileTint` 就地在字典里**挪到最前**（不替换字典对象，避免影响别的 mod），
   于是它固定出现在 **Visual FX 页签第 0 页的第一格**。
   每次构建按钮后会打一行诊断日志，方便确认：
   `[diag] bottom bar: VisualFx has N buttons, custom at page=0 keyCode=1 pageSize=11.`

2. **首砖可用性** —— `LevelEventInfo.allowFirstFloorCheck` 在 `allowFirstFloor == null` 时
   退化为「是否含 angleOffset 属性」。本事件没有该属性，会被判定为「不能放在第一块砖」
   （按钮变灰，甚至整类页签被隐藏）。所以元数据里显式设了 `allowFirstFloor = true`。

3. **选中砖时自动打开属性面板** —— `InspectorPanel.ShowTabsForFloor` 用
   `Enum.GetValues(typeof(LevelEventType))` 决定默认打开哪个事件的面板，我们的 900 不在其中，
   于是「砖上只有本事件」时原版会选中 `None`（面板不开）。后置补丁在方法返回后兜底：
   若砖上确实有本事件而原版什么也没选中，就打开它——与原版「砖上只有一个事件会自动打开」一致。
   开关：`CustomEvent.AutoOpenOnTileSelect`（默认 `true`）。

---

## 5. 注意事项 / 已知限制

- **需要本模组才能读档**：`.adofai` 里出现 `SetTileTint` 时，没装模组的游戏无法解析该事件，
  加载会抛异常。发布关卡时可在关卡设置里声明 `requiredMods`。
- **类型 ID 900** 是自选的，已避开游戏 0..65 与已知的 801/802/810/812。
  若与其它模组冲突，改 `CustomEvent.TypeId` / `DictKey` 即可（同时要确保 `DictKey` 唯一）。
- **底部栏按钮在编辑器启动时构建**（`scnEditor.Start` → `LoadEditorProperties`）。
  若是在编辑器已经打开之后才启用本模组，需要退出并重新进入编辑器，按钮才会出现。
  按钮是否成功创建/在第几页，可看 UMM 日志里的 `[diag] bottom bar: …` 一行。
- 属性标签用 `customLabel` 写字面量（`Tile` / `Tint color` / …）。要做多语言，可改用 `key`
  并像 `ADOFAIEditorExtension` 那样补 `RDString.GetWithCheck`。
- 运行时效果挂在 `scnGame.ApplyEventsToFloors` 之后，会在 `ColorTrack` 等原版轨道变色之后覆盖颜色；
  这是“染色事件”的预期语义。若只想轻微叠加，可改为与现有颜色混合。
- `TargetMethod()` 形式的补丁（`ParseEnumPatch`）与编译器生成方法名无关，但也因此对游戏版本较敏感；
  升级游戏后请重新验证（见下）。

### 补丁目标自检

仓库里所有 Harmony 目标（方法名 / 形参名 / 参数类型）都是对 `Assembly-CSharp.dll` 的真实元数据核对过的，
共 40 项检查全部通过。升级游戏版本后，建议重新核对一次：

- `RDUtils::ParseEnum<T>(string, T)`
- `ADOFAI.LevelEvent::Decode(dict, explicitEventType, isGlobal)` / `Encode(settings)`
- `ADOStartup::SetupLevelEventsInfo()` / `scnEditor::LoadEditorProperties()`
- `ADOFAI.InspectorPanel::ShowTabsForFloor(floorID)`
- `ADOFAI.LevelEventButton::Init(...)`（必须只有一个重载）
- `RDString::GetWithCheck(key, exists, parameters)`
- `scnGame::ApplyEventsToFloors(List<scrFloor>, LevelData, scrLevelMaker, List<LevelEvent>)`
