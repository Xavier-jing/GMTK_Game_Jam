# 剧情解释器 v1

## 1. 范围

剧情解释器从 `Resources/Story/{ScriptId}.json` 加载人类编写的 JSON，以单实例、顺序方式执行剧情。它支持：

- 对话和旁白。
- 纯动作节点。
- 条件选择与跨脚本跳转。
- 显式结束节点。
- 场景对象显隐、剧情标记、道具交互、动作回合和已有玩家语义动作。
- 一次性全局 2D 音效和 BGM 交叉切换。
- 取消、错误终止、玩家输入锁定和运行期剧情进度。

首版不支持动作并行、通用表达式、反射调用、跨场景继续、自动播放、历史记录、磁盘存档、剧情停止 BGM 或空间音效。

## 2. 文件与标识规则

- JSON 文件放入 `Assets/_Project/Resources/Story/`。
- 文件名必须与文档中的 `ScriptId` 完全一致。例如 `gameplay_intro.json` 对应 `"ScriptId": "gameplay_intro"`。
- `ScriptId`、节点 `Id`、`ActorId`、剧情标记 Key 和场景 `TargetId` 只能包含英文字母、数字、下划线和连字符，并且区分大小写。
- 当前格式版本固定为 `"Version": 1`。
- 所有脚本必须使用 Unity 菜单 `Tools/Jam Template/Validate Story Scripts` 校验后再提交。

## 3. JSON 格式

完整示例：

```json
{
  "Version": 1,
  "ScriptId": "gameplay_intro",
  "StartNodeId": "setup",
  "Nodes": [
    {
      "Id": "setup",
      "Type": "Action",
      "Actions": [
        {
          "Id": "SetSceneObjectActive",
          "Params": {
            "TargetId": "intro_prop",
            "BoolValue": true
          }
        },
        {
          "Id": "SetStoryFlag",
          "Params": {
            "Key": "intro_started",
            "BoolValue": true
          }
        }
      ],
      "Next": "line_01"
    },
    {
      "Id": "line_01",
      "Type": "Dialogue",
      "ActorId": "captain",
      "Dialog": "我们得先把轨道拆掉。",
      "BeforeActions": [],
      "AfterActions": [],
      "Next": "decision_01"
    },
    {
      "Id": "decision_01",
      "Type": "Choice",
      "Choices": [
        {
          "Dialog": "拆除轨道",
          "TargetScriptId": "gameplay_intro",
          "TargetNodeId": "remove_rail",
          "Condition": {
            "Id": "PlayerHasWrench",
            "UnavailableMode": "Disabled"
          }
        },
        {
          "Dialog": "暂时离开",
          "TargetScriptId": "gameplay_intro",
          "TargetNodeId": "leave"
        }
      ]
    },
    {
      "Id": "remove_rail",
      "Type": "Action",
      "Actions": [
        {
          "Id": "RemoveRailAndAscend"
        }
      ],
      "Next": "complete"
    },
    {
      "Id": "leave",
      "Type": "End",
      "Result": "Left"
    },
    {
      "Id": "complete",
      "Type": "End",
      "Result": "Completed"
    }
  ]
}
```

### Dialogue

必填字段：

- `Id`
- `Type: "Dialogue"`
- `Dialog`
- `Next`

可选字段：

- `ActorId`：为空或省略时作为旁白显示。
- `BeforeActions`：显示文字前顺序执行。
- `AfterActions`：玩家完成本句后、进入 `Next` 前顺序执行。

第一次 Submit 会立即补全打字效果，第二次 Submit 才进入下一节点。

### Action

- `Actions` 至少包含一项。
- 动作按数组顺序执行，任一动作失败都会终止剧情。
- `Next` 必须指向当前脚本中的节点。
- 运行时连续经过超过 100 个无交互节点会终止剧情，避免死循环。

### Choice

- `Choices` 至少包含一项。
- 每项必须包含 `Dialog`、`TargetScriptId` 和 `TargetNodeId`。
- `Condition` 可省略；省略时选项始终可用。
- 条件不满足时，`UnavailableMode` 为 `Hidden` 则隐藏，为 `Disabled` 则显示但不可选；省略时默认 `Disabled`。
- 选择目标可以位于另一个 JSON，但目标脚本和节点必须存在。

### End

- 结束当前剧情并恢复玩家输入。
- `Result` 会通过 `StoryController.Completed` 事件返回。
- 根脚本和终点所在脚本会记录为当前应用生命周期内已完成。

## 4. Params

所有动作和条件共用固定 `Params` 对象：

| 字段 | 类型 | 用途 |
| --- | --- | --- |
| `Key` | String | 剧情标记 Key |
| `StringValue` | String | 枚举或字符串值 |
| `BoolValue` | Bool | 开关或期望布尔值 |
| `IntValue` | Int | 正整数动作回合消耗 |
| `FloatValue` | Float | 音效音量、BGM 淡入淡出秒数或其他浮点参数 |
| `TargetId` | String | `StoryTarget` 标识 |

未被当前处理器使用的字段会被忽略。

## 5. 动作白名单

| Action Id | 必需 Params | 行为 |
| --- | --- | --- |
| `SetSceneObjectActive` | `TargetId`, `BoolValue` | 设置登记场景对象的激活状态 |
| `SetStoryFlag` | `Key`, `BoolValue` | 修改运行期剧情 Bool 标记 |
| `AcquireWrench` | 无 | 调用 `PlayerGameplayStatus.AcquireWrench()` |
| `RemoveRailAndAscend` | 无 | 调用 `Player.TryStartRailRemovedAscend()` |
| `ReleaseFloatingItemAndRise` | 无 | 调用 `Player.TryReleaseFloatingItemAndRise()` |
| `PlaySfx` | `StringValue`, 可选 `FloatValue` | 播放一次全局 2D 音效；`FloatValue` 为 `0` 时使用音量 `1` |
| `SwitchBgm` | `StringValue`, 可选 `FloatValue` | 交叉切换 BGM；`FloatValue` 为 `0` 时使用 `1` 秒过渡 |
| `WorldPropCommand` | `TargetId`, `StringValue` | 对目标道具执行经过 C# 前置条件复检的语义命令 |
| `SpendTurns` | `IntValue > 0` | 扣除指定动作回合；归零时在剧情正常结束后进入结局一 |
| `ChangeTurns` | `IntValue != 0` | 按有符号数值改变回合；正数增加稳定时间，负数减少，归零进入结局一 |
| `RequestRunEnd` | `StringValue` | 请求 `EndingTwo` 或 `EndingThree`，在剧情正常结束后切周目 |

### 剧情音频动作

音频动作的 `StringValue` 是稳定的 `AudioId`，不含目录和扩展名，只允许英文字母、数字、下划线和连字符。资源由人类放入固定目录：

- SFX：`Assets/_Project/Resources/Audio/SFX/{AudioId}.*`
- BGM：`Assets/_Project/Resources/Audio/BGM/{AudioId}.*`

播放开门音效：

```json
{
  "Id": "PlaySfx",
  "Params": {
    "StringValue": "door_open",
    "FloatValue": 0.8
  }
}
```

`PlaySfx.FloatValue` 必须为 `0` 到 `1`。`0` 表示使用默认音量 `1`。动作使用现有 SFX 池播放，不等待音效结束，也不受后续剧情推进或取消影响。

切换游戏 BGM：

```json
{
  "Id": "SwitchBgm",
  "Params": {
    "StringValue": "gameplay",
    "FloatValue": 1.5
  }
}
```

`SwitchBgm.FloatValue` 必须为非负数。`0` 表示使用默认 `1` 秒交叉淡入淡出。切换不会等待过渡结束；目标与当前 BGM 相同时不会重新播放。BGM 由常驻 `AudioService` 持有，会持续到下一次切换或其他代码显式调用 `AudioService.StopBgm()`。

两个动作都可以放入 Action 节点的 `Actions`，或 Dialogue 节点的 `BeforeActions`、`AfterActions`。Unity 编辑器校验会确认对应 `AudioClip` 已导入。运行时资源缺失或音频服务不可用时会记录包含 Action Id 和 Audio Id 的错误，但不会中止剧情。

剧情不允许直接调用 `PlayerStateMachine.ChangeState()`。玩家状态变化必须经过现有语义接口，以保留轨道、物品和世界层前置条件。

`WorldPropCommand` 的 `StringValue` 支持：

| 命令 | 用途 |
| --- | --- |
| `Inspect` | 仅完成查看分支，不改变状态 |
| `OpenDresser` | 打开梳妆台并显示其中道具 |
| `TakeIntoCarrySlot` | 把负重物放入唯一携带槽 |
| `DropFromCarrySlot` | 在玩家当前位置放下当前负重物 |
| `AcquireInventoryItem` | 把剪刀、扳手或绳子加入背包 |
| `FirstWallStrike` | 第一次砸墙；剧情结束后记录永久进度并结束本周目 |
| `SecondWallStrike` | 第二次砸墙；剧情结束后揭示真相并结束本周目 |
| `CutBlanket` | 使用配置的剪刀处理床被 |
| `TriggerBedSwitch` | 触发床开关 |
| `StartSteeringWheel` | 启动方向盘 |
| `UnplugRefrigerator` | 关闭冰箱电源 |
| `ConnectBedPower` | 把携带槽中的带线床接到电源处 |
| `InstallPlank` | 把携带槽中的木板安装到墙洞 |
| `AttachRopeToFabric` | 真相知晓后，把本周目背包中的绳子固定到处理好的布料 |
| `AnchorParachute` | 真相知晓后，用携带槽中的梳妆台锚定降落伞绳索 |
| `DeployParachute` | 布料、绳索和锚点全部完成后触发结局三 |

选择菜单中的条件只决定“显示/灰显”。执行 `WorldPropCommand` 时会在修改状态前再次检查同一条件，因此玩家状态在菜单打开后发生变化也不会绕过限制。

需要消耗动作回合时，由 JSON 把 `SpendTurns` 放在成功的语义动作之后。查看、无状态文本和直接结束周目的动作不要添加 `SpendTurns`。推荐写法：

```json
{
  "Id": "take_tea_set",
  "Type": "Action",
  "Actions": [
    {
      "Id": "WorldPropCommand",
      "Params": {
        "TargetId": "prop_tea_set",
        "StringValue": "TakeIntoCarrySlot"
      }
    },
    {
      "Id": "SpendTurns",
      "Params": {
        "IntValue": 1
      }
    }
  ],
  "Next": "complete"
}
```

如果第一项因为携带槽已满等原因失败，第二项不会执行，因此不会误扣回合。

## 6. 条件白名单

| Condition Id | 必需 Params | 行为 |
| --- | --- | --- |
| `StoryFlagEquals` | `Key`, `BoolValue` | 比较剧情 Bool 标记 |
| `PlayerHasWrench` | 可选 `BoolValue` | 比较玩家是否有扳手 |
| `PlayerRailRemoved` | 可选 `BoolValue` | 比较轨道是否已移除 |
| `PlayerHasSlotItem` | 可选 `BoolValue` | 比较物品槽是否有物品 |
| `PlayerIsWorldLayer` | `StringValue` | 比较 `Lower` 或 `Upper` |
| `WorldPropCommandAvailable` | `TargetId`, `StringValue` | 使用与执行动作相同的道具前置条件检查 |
| `RunFlagEquals` | `StringValue`, `BoolValue` | 比较单周目 `RunFlagId` |
| `LoopProgressFlagEquals` | `StringValue`, `BoolValue` | 比较跨周目 `LoopProgressFlag` |

对三个玩家 Bool 条件，完全省略 `Params` 表示期望为 `true`；提供 `Params` 后使用其中的 `BoolValue`，可用 `false` 表示反向条件。

`RunFlagEquals.StringValue` 支持 `DresserOpened`、`WallRepaired`、`SteeringWheelRaised`、`FridgeUnplugged`、`BedConnected`、`FabricPrepared`、`BedSwitchTriggered`、`RopeAttached`、`ParachuteAnchored`。

`LoopProgressFlagEquals.StringValue` 支持 `TruthKnown`、`EndingTwoReached`、`EndingThreeReached`。

墙面第一次击打属于 `RunState`，只在当前周目保留；同一周目第二次击打揭晓真相并结束。
新周目墙洞物理状态会重置，但 `TruthKnown` 继续保留。所有物品在新周目重新拾取，
仍按 JSON 中的 `SpendTurns` 扣除回合。

道具选择示例：

```json
{
  "Id": "tea_set_menu",
  "Type": "Choice",
  "Choices": [
    {
      "Dialog": "放入携带槽",
      "TargetScriptId": "prop_tea_set",
      "TargetNodeId": "take_tea_set",
      "Condition": {
        "Id": "WorldPropCommandAvailable",
        "UnavailableMode": "Disabled",
        "Params": {
          "TargetId": "prop_tea_set",
          "StringValue": "TakeIntoCarrySlot"
        }
      }
    },
    {
      "Dialog": "丢弃",
      "TargetScriptId": "prop_tea_set",
      "TargetNodeId": "drop_tea_set",
      "Condition": {
        "Id": "WorldPropCommandAvailable",
        "UnavailableMode": "Disabled",
        "Params": {
          "TargetId": "prop_tea_set",
          "StringValue": "DropFromCarrySlot"
        }
      }
    }
  ]
}
```

### 物理物品槽与玩家状态机

影响玩家浮沉的世界道具不要加入普通 `Inventory`。它们必须使用
`WorldPropCommand` 的 `TakeIntoCarrySlot` 和 `DropFromCarrySlot` 命令：

- 轨道未拆除时，携带槽不会改变玩家的轨道运动。
- 轨道拆除后，玩家在上层把道具放入槽中会进入 `PlayerSinkingState`。
- 下坠过程不接收 WASD；接触下层地面后恢复 WASD。
- 玩家位于下层且槽中仍有道具时，Space 才能触发跳跃。
- 在下层执行 `DropFromCarrySlot` 会同时释放世界道具、清空槽位并进入
  `PlayerAscendState`，不能只直接清空 `PlayerGameplayStatus`。

物理槽与普通背包是两套数据：

- `AppContext.Inventory` 保存剪刀、扳手、绳子等剧情背包道具。
- `PlayerCarrySlot.CurrentProp` 保存当前影响物理状态的唯一世界道具。
- 物理槽 UI 应订阅 `PlayerCarrySlot.Changed`，并以事件参数更新图标或名称；
  参数为 `null` 时清空显示。不要从 `Inventory.VisibleItems` 推断物理槽状态。

需要扣回合的进槽、出槽或场景改造命令，应在同一个 Action 节点中先执行
`WorldPropCommand`，成功后再执行 `SpendTurns`。查看、离开、选择物品和取消菜单
不添加 `SpendTurns`。

#### Unity 人工挂接

1. 在玩家 Prefab 或场景中的玩家 GameObject 上确认存在 `Player`、
   `PlayerGameplayStatus`、`PlayerCarrySlot`、`PlayerInteractor` 和
   `PlayerInteractionDetector`。`Player` 的 `Initial Rail` 必须指向出生轨道。
2. `PlayerCarrySlot.Drop Anchor` 指向玩家附近用于放回世界道具的 Transform；
   建议位于玩家脚边、避开玩家 Collider。未配置时会使用玩家当前位置。
3. 每个可携带道具添加并配置 `WorldStoryInteractable`，其 `Prop Id` 必须是
   `Dresser`、`CableBed`、`TeaSet`、`Vase`、`Plank` 或 `Refrigerator` 之一；
   `First Script Id` 指向包含进槽/出槽选项的对应 `prop_*.json`。
4. 对物品槽 UI，由人类把显示脚本接到玩家的 `PlayerCarrySlot.Changed`：
   非空时显示 `CurrentProp.PropId` 对应的人类制作图标，空值时隐藏图标。
5. 进入 Play Mode 后依次验证：轨道移动 → 获得扳手 → 拆轨上升 →
   上层进槽后下沉 → 下层 WASD/Space → F 菜单出槽后重新上升。

这些组件与脚本已存在，不需要新增 `.meta`。若人类新增物品槽 UI 脚本或美术资源，
应由 Unity `2022.3.62f3c1` 导入并提交其生成的 `.meta`；AI 不创建或修改该元数据。

## 7. 运行时 API

`StoryController` 提供：

```csharp
bool TryStart(string scriptId, string startNodeId = null);
bool TryAdvance();
bool TrySelectChoice(int choiceIndex);
void Cancel();
```

状态查询：

```csharp
StoryRunnerState State
string CurrentScriptId
string CurrentNodeId
bool IsRunning
```

事件：

```csharp
event Action<StoryNodeInfo> NodeEntered;
event Action<StoryCompletion> Completed;
event Action<StoryError> Failed;
```

启动新剧情时，如果已有剧情正在运行，旧剧情会先取消和清理。

## 8. 人工 Unity 挂接

智能体不得修改场景、Prefab、`.meta`、字体、图像、声音或其他艺术资产。以下步骤必须由人类在 Unity `2022.3.62f3c1` 中完成。

### GamePlay 场景

1. 打开 `Assets/_Project/Scenes/GamePlay.unity`。
2. 在 `Canvas/UIRoot/HintPanel` 下创建 `StoryOverlay`。
3. 使用现有 uGUI 和 TextMeshPro 创建：
   - Panel 根节点。
   - Actor TMP Text。
   - Dialog TMP Text。
   - Choice Container。
   - 一个 Button 模板，模板下包含 TMP Text，并默认设为不激活。
4. 给 `StoryOverlay` 添加 `StoryPresenter`：
   - `Panel Root`：Panel 根节点。
   - `Actor Text`：Actor TMP Text。
   - `Dialog Text`：Dialog TMP Text。
   - `Choice Container`：选择按钮父节点。
   - `Choice Button Template`：默认不激活的按钮模板。
   - `Characters Per Second`：推荐 `30` 到 `60`。
5. 给根对象 `UIController` 添加 `StoryController`：
   - `Presenter`：刚创建的 `StoryPresenter`。
   - `UI Input`：同对象现有的 `UIInputHandler`。
   - `Player`：如果 Player 已存在则直接引用；如果运行时生成可留空，首次启动剧情时会查找一次。

### SandBox 场景

执行相同步骤，但把 `StoryOverlay` 创建在 `Canvas/Image` 下。`Player` 字段引用场景根对象 `Player`。

### 场景目标

1. 给需要被剧情控制显隐的对象添加 `StoryTarget`。
2. 填写全场景唯一 `Target Id`。
3. `Target Id` 必须与 JSON `Params.TargetId` 完全一致。
4. 运行校验并在 Play Mode 中实际触发对应动作。

### 15 个道具对象

每个可交互对象都添加：

1. 一个 3D `Collider`；可以是 Trigger，所在 Layer 必须被 Player 的 `PlayerInteractionDetector.Interactable Layers` 包含。
2. `StoryTarget`，填写下表建议的稳定 `Target Id`。
3. `WorldStoryInteractable`：
   - `Prop Id`：按下表填写。
   - `Interaction Point`：玩家距离判定点；可留空使用对象 Transform。
   - `Story Controller`：场景中的同一 `StoryController`。
   - `First Script Id` / `Repeat Script Id`：人类创建的首次和重复剧情文件名，不带 `.json`。
   - `Interaction Prompt`：推荐 `F` 或本地化后的提示。
   - `Presentation Root`：只挂道具美术子节点，不要填写组件自身 GameObject。
   - `Presentation Renderers`：该道具的 SpriteRenderer/MeshRenderer；留空时运行时收集子节点 Renderer。
   - `Interaction Colliders`：道具的 3D Collider；留空时运行时收集子节点 Collider。
4. 对剪刀、扳手、绳子设置 `Inventory Item`。由人类通过 `Create > Jam > Inventory > Item Definition` 创建三个 `.asset` 并配置名称、图标、是否显示、能否丢弃。
5. 床被的 `Required Item` 指向剪刀的 `ItemDefinition`。

| 编号 | Prop Id | 建议 Target Id | 主要 JSON 命令 | 特殊挂接 |
| --- | --- | --- | --- | --- |
| 1 | `Dresser` | `prop_dresser` | `Inspect`, `OpenDresser`, `TakeIntoCarrySlot`, `DropFromCarrySlot` | 无 |
| 2 | `CableBed` | `prop_cable_bed` | `Inspect`, `TakeIntoCarrySlot`, `DropFromCarrySlot` | 勾选 `Auto Start First Story`；出生点放在附近 |
| 3 | `TeaSet` | `prop_tea_set` | `TakeIntoCarrySlot`, `DropFromCarrySlot` | 无 |
| 4 | `Vase` | `prop_vase` | `TakeIntoCarrySlot`, `DropFromCarrySlot` | 无 |
| 5 | `Plank` | `prop_plank` | `TakeIntoCarrySlot`, `DropFromCarrySlot` | 第一次砸墙后自动显示 |
| 6 | `Refrigerator` | `prop_refrigerator` | `Inspect`, `TakeIntoCarrySlot`, `DropFromCarrySlot`, `UnplugRefrigerator` | 无 |
| 7 | `SmallWallHole` | `prop_small_wall_hole` | `Inspect`, `FirstWallStrike`, `InstallPlank` | 第一次砸墙或真相后显示规则由代码处理 |
| 8 | `Scissors` | `prop_scissors` | `Inspect`, `AcquireInventoryItem` | `Inventory Item = Scissors` |
| 9 | `Wrench` | `prop_wrench` | `Inspect`, `AcquireInventoryItem` | `Inventory Item = Wrench` |
| 10 | `LargeWallHole` | `prop_large_wall_hole` | `SecondWallStrike` | 第一次砸墙后显示，真相后隐藏 |
| 11 | `BedBlanket` | `prop_bed_blanket` | `CutBlanket` | `Required Item = Scissors` |
| 12 | `BedSwitch` | `prop_bed_switch` | `TriggerBedSwitch` | 可以没有可见美术；仍需独立目标对象 |
| 13 | `SteeringWheel` | `prop_steering_wheel` | `StartSteeringWheel` | 床开关触发后显示 |
| 14 | `PowerConnector` | `prop_power_connector` | `ConnectBedPower` | 可以没有可见美术；冰箱断电后显示 |
| 15 | `Rope` | `prop_rope` | `Inspect`, `AcquireInventoryItem` | `Inventory Item = Rope` |

Player 上的 `PlayerCarrySlot` 和 `PlayerInteractor` 在缺失时会由 `Player.Awake()` 补上，Unity 导入脚本后仍建议由人类在 Inspector 中确认：

- `PlayerInteractionDetector.Detection Radius` 推荐先使用 `1.5`，按人物尺寸人工微调。
- `PlayerCarrySlot.Drop Anchor` 可指向 Player 脚边的空 Transform；留空时使用 Player 位置。
- 所有物品使用 3D Collider。原先未接入主循环的 2D `CircleCollider2D` 传感器不再需要。
- `PlayerInteractor` 会在选择目标时检查一次 `CanInteract`，按下 F 真正调用前再检查一次。

### 道具剧情文件

仓库已经在 `Assets/_Project/Resources/Story/` 提供 15 个 `prop_*.json` 初稿。每个道具的 `First Script Id` 直接填写与 `Target Id` 相同的值，例如梳妆台填写 `prop_dresser`。初稿中的 `【占位】` 文本需要策划替换为正式剧情。

需要重复文本时再创建 `Repeat Script Id`；不填写重复脚本时会继续使用首次脚本。首次标记只在该脚本正常到达 `End` 后设置，取消或失败不会误记。

JSON 的选择项使用 `WorldPropCommandAvailable`，实际结果节点使用相同 `TargetId` 和命令的 `WorldPropCommand`。所有具体台词、选项措辞、声音和表现资源均由人类提供。

### 剧情音频

1. 由人类制作或取得符合比赛规则的音频。
2. 在 Unity `2022.3.62f3c1` 中创建 `Assets/_Project/Resources/Audio/SFX/` 和 `Assets/_Project/Resources/Audio/BGM/`。
3. 把一次性音效导入 `SFX`，把循环音乐导入 `BGM`；文件名必须与剧情中的 `StringValue` 完全一致。
4. 音频 ID 不写扩展名。例如 `door_open.wav` 在剧情中填写 `door_open`。
5. 运行 `Tools/Jam Template/Validate Story Scripts`；资源缺失或资源类型不是 `AudioClip` 时，Console 会显示 JSON 文件、节点和动作位置。
6. 无需修改场景、Prefab 或 Inspector；播放继续使用现有 `MasterMixer` 的 `SFX` 与 `BGM` 分组。

### 导入与提交

- 让 Unity 为所有新增 `.cs` 和 `.json` 生成 `.meta`。
- 让 Unity 为人类导入的音频和新音频目录生成 `.meta`。
- 检查 Console 无编译错误。
- 提交新增 `.meta`，不要复用其他文件的 GUID。
- 所有 UI、字体、声音和剧情内容必须由人类制作或提供，遵守比赛的无 AI 艺术资产规则。

## 9. 错误格式

运行时错误统一包含：

```text
ScriptId/NodeId/HandlerId: 原因
```

未知节点、动作、条件、无效参数、缺失跳转目标、重复 ID、无可见选项和动作失败都会进入 `Faulted`，触发 `Failed`，隐藏 UI 并恢复玩家输入。`PlaySfx` 和 `SwitchBgm` 的运行时播放失败是唯一例外：它们记录错误后继续剧情，避免表现资源缺失阻断关键流程。

## 10. 验证

编辑器纯代码测试：

- `Tools/Jam Template/Tests/Run Story Validator Unit Test`
- `Tools/Jam Template/Tests/Run World Story Rules Unit Test`
- `Tools/Jam Template/Validate Story Scripts`

Play Mode 最小冒烟：

1. `Boot → MainMenu → GamePlay`，确认 Player 的 3D F 探测有提示且暂停时不触发。
2. 打开梳妆台，确认剪刀/扳手从隐藏变为可交互；拾取后进入背包，下一周目重新出现。
3. 携带任一六种负重物，确认第二件选项灰显；上层携带时进入下沉状态，放下后槽位清空。
4. 保持选择菜单打开并改变背包或携带槽，再选择旧选项，确认动作失败且不误扣 `SpendTurns`。
5. 第一次砸墙后结束本周目；下一周目出现大洞和木板；第二次砸墙后记录真相并结束本周目。
6. 没有剪刀时床被选项灰显，有剪刀后可执行；按剧本顺序检查开关、方向盘、冰箱电源和床电源。
7. 让一次成功动作扣到 0 回合，确认剧情先正常结束，再进入结局一和下一周目。
8. 取消或故意触发错误，确认玩家输入恢复，且未提交待处理的砸墙/结局请求。
9. 连续执行多个 `PlaySfx`，确认音效可重叠、遵守 SFX 音量，且剧情不会等待音效结束。
10. 执行 `SwitchBgm`，确认 BGM 平滑切换；再次切换同一 Audio Id 不会重启，切换场景后仍继续播放。
11. 临时在测试剧情中填写不存在的 Audio Id，确认编辑器校验失败；若绕过校验进入运行时，确认只记录错误而不中止剧情。
