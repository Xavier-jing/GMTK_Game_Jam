# 剧情解释器 v1

## 1. 范围

剧情解释器从 `Resources/Story/{ScriptId}.json` 加载人类编写的 JSON，以单实例、顺序方式执行剧情。它支持：

- 对话和旁白。
- 纯动作节点。
- 条件选择与跨脚本跳转。
- 显式结束节点。
- 场景对象显隐、剧情标记和已有玩家语义动作。
- 取消、错误终止、玩家输入锁定和运行期剧情进度。

首版不支持动作并行、通用表达式、反射调用、跨场景继续、自动播放、历史记录或磁盘存档。

## 2. 文件与标识规则

- JSON 文件由人类放入 `Assets/_Project/Resources/Story/`。
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
| `IntValue` | Int | 预留整数参数 |
| `FloatValue` | Float | 预留浮点参数 |
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

剧情不允许直接调用 `PlayerStateMachine.ChangeState()`。玩家状态变化必须经过现有语义接口，以保留轨道、物品和世界层前置条件。

## 6. 条件白名单

| Condition Id | 必需 Params | 行为 |
| --- | --- | --- |
| `StoryFlagEquals` | `Key`, `BoolValue` | 比较剧情 Bool 标记 |
| `PlayerHasWrench` | 可选 `BoolValue` | 比较玩家是否有扳手 |
| `PlayerRailRemoved` | 可选 `BoolValue` | 比较轨道是否已移除 |
| `PlayerHasSlotItem` | 可选 `BoolValue` | 比较物品槽是否有物品 |
| `PlayerIsWorldLayer` | `StringValue` | 比较 `Lower` 或 `Upper` |

对三个玩家 Bool 条件，完全省略 `Params` 表示期望为 `true`；提供 `Params` 后使用其中的 `BoolValue`，可用 `false` 表示反向条件。

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

### 导入与提交

- 让 Unity 为所有新增 `.cs` 和人类创建的 `.json` 生成 `.meta`。
- 检查 Console 无编译错误。
- 提交新增 `.meta`，不要复用其他文件的 GUID。
- 所有 UI、字体、声音和剧情内容必须由人类制作或提供，遵守比赛的无 AI 艺术资产规则。

## 9. 错误格式

运行时错误统一包含：

```text
ScriptId/NodeId/HandlerId: 原因
```

未知节点、动作、条件、无效参数、缺失跳转目标、重复 ID、无可见选项和动作失败都会进入 `Faulted`，触发 `Failed`，隐藏 UI 并恢复玩家输入。
