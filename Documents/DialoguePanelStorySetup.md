# DialoguePanel 剧情系统挂接

## 1. 目标与现状

本说明用于把 `Assets/_Project/Prefabs/UI/DialoguePanel.prefab` 的场景实例接入现有剧情链路：

`WorldStoryInteractable` → `StoryController` → `StoryPresenter` → `DialoguePanel`

剧情正文与选项文字继续由
`Assets/_Project/Resources/Story/{ScriptId}.json` 提供，不在 Prefab 的 TMP 默认文本中维护。
剧情 CG 的编号到 Sprite 映射由 `StoryController` 持有，控制器解析后把实际
Sprite 转发给 `StoryPresenter`。

当前 `DialoguePanel` 已有：

- 根对象 `DialoguePanel`：对话框背景。
- 子对象 `Text (TMP)`：正文显示区域。
- 子对象 `Image`：由剧情 `PortraitId` 切换的角色立绘。

完整剧情流程还需要人类在 Unity 中补充选项容器和选项按钮模板。当前为单角色
游戏，角色名文本是可选项，可以不创建。
智能体不得自动保存场景、Prefab、图片或 `.meta`。

## 2. GamePlay 场景挂接

使用 Unity `2022.3.62f3c1` 打开
`Assets/_Project/Scenes/GamePlay.unity`，在 Hierarchy 中确认 `DialoguePanel`
位于 `Canvas` 下。

### 2.1 补充 DialoguePanel 子对象

在 `Canvas/DialoguePanel` 下创建：

1. `ActorText`（可选）
   - 组件：`TextMeshPro - Text (UI)`。
   - 用途：显示 JSON 的 `ActorId`。
   - 文本、字体、位置和颜色由人类按现有 UI 设计配置。
   - 旁白节点的 `ActorId` 为空时，运行时会自动隐藏该对象。
   - 当前单角色 UI 可以跳过此对象，并把 `Actor Text` 留空。
2. `ChoiceContainer`
   - 组件：至少包含 `RectTransform`。
   - 用途：运行时生成剧情选项按钮。
   - 可以由人类添加 `Vertical Layout Group` 等布局组件。
3. `ChoiceButtonTemplate`
   - 必须放在 `ChoiceContainer` 下。
   - 根对象必须有 `Button` 组件。
   - 子对象必须有一个 `TextMeshPro - Text (UI)`，用于接收选项的 `Dialog`。
   - 完成样式与导航配置后，把模板 GameObject 设为不激活。

如果创建 `ActorText`，不要把 `Text (TMP)` 同时用作 `Actor Text` 和
`Dialog Text`；运行时会分别写入两个字段。

### 2.2 创建 StoryCgImage

在 `Canvas` 下创建独立对象 `StoryCgImage`，不要放入 `DialoguePanel`，也不要
复用周目结局流程控制的 `Canvas/EndingRoot`：

- 添加 uGUI `Image`，RectTransform 的四向 Anchor 设为 Stretch，Left、Right、
  Top、Bottom 均为 `0`。
- 把 `StoryCgImage` 的 sibling 顺序放在 `DialoguePanel` 之前，使 CG 覆盖游戏
  世界但不遮挡剧情文字和选项。
- 关闭 `Raycast Target`，避免拦截对话 Submit、选项按钮和鼠标输入。
- 是否启用 `Preserve Aspect` 由人类根据实际 CG 尺寸决定；建议先启用。
- 默认把 GameObject 设为不激活。`StoryPresenter` 启动时也会主动隐藏并清空它。

### 2.3 配置 StoryPresenter

在 `Canvas/DialoguePanel` 上添加 `StoryPresenter`，填写：

| Inspector 字段 | 引用 |
| --- | --- |
| `Panel Root` | `Canvas/DialoguePanel` |
| `Actor Text` | 可留空；需要显示角色名时才引用 `ActorText` |
| `Dialog Text` | `Canvas/DialoguePanel/Text (TMP)` 的 TMP 文本 |
| `Choice Container` | `Canvas/DialoguePanel/ChoiceContainer` |
| `Choice Button Template` | `Canvas/DialoguePanel/ChoiceContainer/ChoiceButtonTemplate` 的 Button |
| `Cg Image` | `Canvas/StoryCgImage` 的 uGUI Image |
| `Portrait Image` | `Canvas/DialoguePanel/Image` 的 uGUI Image |
| `Portrait Bindings` | 大小写敏感的 `PortraitId → Sprite` 映射 |
| `Characters Per Second` | 建议先用 `40`，可在 `30`–`60` 内人工调整 |

展开 `Portrait Bindings`，把 Size 设为 `5`，逐项拖入现有的人工作品：

| Portrait Id | Sprite |
| --- | --- |
| `face01` | `face01` 对应 Sprite |
| `face02` | `face02` 对应 Sprite |
| `face03` | `face03` 对应 Sprite |
| `face04` | `face04` 对应 Sprite |
| `face05` | `face05` 对应 Sprite |

代码解析器会把 JSON 中的数字立绘 ID 映射到上述五个绑定：

| JSON `PortraitId` | 实际绑定 |
| --- | --- |
| `"0"` | `face01`（fail-safe） |
| `"1"` | `face01` |
| `"2"` | `face02` |
| `"3"` | `face03` |
| `"4"` | `face04` |
| `"5"` | `face05` |

`PortraitId` 是字符串，因此 JSON 中必须保留双引号。原有的 `face01` 至
`face05` 写法继续兼容，并按大小写敏感的精确规则匹配。保留
`Image > Source Image` 作为每次新剧情开始时的安全默认图；不要让映射项的
Sprite 留空。

`StoryPresenter.Awake()` 会隐藏 `Panel Root`。这是正常行为：剧情通过前置检查
并建立运行会话后，`StoryController.TryStart()` 会立即显示它；不需要等待首个
Dialogue 或 Choice 节点。

### 2.4 配置 StoryController

当前 `GamePlay` 场景使用场景根对象 `StoryController` 上的同名组件；保留该
组件，不要再向 `Canvas` 添加第二个 `StoryController`。`Canvas` 已带有
`UIInputHandler`，`DialoguePanel` 是它的子对象。

填写：

| Inspector 字段 | 引用 |
| --- | --- |
| `Presenter` | `Canvas/DialoguePanel` 上的 `StoryPresenter` |
| `UI Input` | `Canvas` 上现有的 `UIInputHandler` |
| `Player` | 场景根对象 `Player` |
| `Cg Bindings` | 唯一正整数 `Cg Number → Sprite` 映射 |

不要再新建第二个 `UIInputHandler`。场景中只应由同一个输入组件向
`StoryController` 发送 Submit。

`Presenter` 和 `UI Input` 即使能够由代码在特定层级中查找，也建议显式拖入，
避免以后调整 Hierarchy 后连接失效。

`Cg Bindings` 的编号不限制数量，但每项必须大于 `0`、不可重复且必须引用由
人类制作或提供的 Sprite。JSON 中使用字符串编号，例如：

```json
{
  "Id": "show_cg",
  "Type": "Dialogue",
  "PortraitId": "1",
  "CgId": "1",
  "Dialog": "这一幕会显示编号 1 的 CG。",
  "Next": "hide_cg"
}
```

后续 Dialogue 省略 `CgId` 会保持当前 CG；填写 `"0"` 会隐藏。`"01"`、负数、
名称和超出 Int32 范围的编号会被剧情校验拒绝。

若 CG 应在最后一句对白完成后立即收回，在该 Dialogue 中添加：

```json
"AfterActions": [
  {
    "Id": "HideCg"
  }
]
```

`HideCg` 不需要 `Params`，也可以放在独立 Action 节点，或放在未填写正数
`CgId` 的 Dialogue `BeforeActions`。同一 Dialogue 如果仍填写正数 `CgId`，
会在 `BeforeActions` 之后重新显示对应 CG。到达 End、取消或故障时仍会自动
执行清理，因此显式指令和会话清理共同防止 CG 残留。

## 3. 剧情触发对象

需要启动剧情的对象使用 `WorldStoryInteractable`：

- `Story Controller`：建议显式指向场景根对象 `StoryController` 上的组件；
  留空时运行时会在场景中查找一个。
- `First Script Id`：填写
  `Assets/_Project/Resources/Story/` 下的 JSON 文件名，不带 `.json`。
  例如 `prop_wrench.json` 填写 `prop_wrench`。
- `Repeat Script Id`：需要重复剧情时填写；留空时继续使用首次脚本。

JSON 中 Dialogue 节点的 `Dialog` 会写入 `Dialog Text`，`PortraitId` 会从
`Portrait Bindings` 查找 Sprite 并写入 `Portrait Image`。`ActorId` 仅在
`Actor Text` 已配置时显示；Choice 节点的每个 `Dialog` 会写入运行时复制出的
按钮文字。

`CgId` 由 `StoryController.Cg Bindings` 解析为实际 Sprite，再传给
`StoryPresenter.Cg Image`。CG 会跨省略该字段的 Dialogue 和 Choice 保持；
`"0"`、剧情结束、取消、故障或启动新剧情时会隐藏并清空。

对话面板从 `TryStart()` 成功开始到剧情终止期间保持激活。Dialogue 后的 Action
和节点切换会保留上一句正文；选项一经提交就会保持显示但立即锁定，直到下一
节点刷新。正常 End、主动取消、运行时故障或启动失败都会在完成/失败回调前
隐藏面板并清除临时文字、选项和回调。

同一段剧情内省略 `PortraitId` 会保持上一张立绘。未知 ID、空 Sprite 或
未配置 `Portrait Image` 会在 Console 记录包含 ID 与 Presenter 对象的警告，
保持当前立绘并继续剧情。每次新剧情启动先恢复 `Portrait Image` 的初始
`Source Image`，随后由新剧情首句的合法 ID 覆盖。

## 4. 常见失败表现

| 表现或 Console 信息 | 检查项 |
| --- | --- |
| `StoryPresenter is missing or has unassigned required references` | 检查 `Panel Root`、`Dialog Text`、`Choice Container`、`Choice Button Template`；`Actor Text` 可留空 |
| `StoryController is missing its UIInputHandler reference` | 把 `Canvas/UIInputHandler` 拖入 `UI Input` |
| `No Player was found when the story started` | 把场景根对象 `Player` 拖入 `Player` |
| 面板一直显示 `New Text` | 剧情没有成功启动，或 `Dialog Text` 没有指向 `Text (TMP)` |
| 正文能显示，但进入选项后没有按钮 | 检查 `Choice Container`、模板 `Button`、模板子 TMP 和模板默认非激活状态 |
| 立绘不切换并出现 `no portrait mapping` | 检查 JSON 与 `Portrait Bindings` 的 ID 大小写是否完全一致 |
| 出现 `no Sprite assigned` | 对应映射项没有拖入 Sprite；补齐后重新进入 Play Mode |
| 出现 `Portrait Image is not assigned` | 把 `Canvas/DialoguePanel/Image` 拖入 `Portrait Image` |
| 出现 `CG number ... has no binding` | 在场景 `StoryController.Cg Bindings` 添加对应正整数编号 |
| 出现 `duplicate bindings` 或 `no Sprite assigned` | 删除重复 CG 编号或为该编号拖入 Sprite |
| 出现 `Cg Image is not assigned` | 把 `Canvas/StoryCgImage` 拖入 `StoryPresenter.Cg Image` |
| 第一次 Submit 没有进入下一句 | 打字机仍在播放；第一次只补全本句，第二次才推进 |
| 选项可见但键盘/手柄不能选择 | 检查场景 `EventSystem`、按钮 Navigation 和 `UIInputHandler` |

## 5. Play Mode 冒烟验证

1. 保存 `GamePlay.unity`，确认 Console 没有编译错误。
2. 执行 `Tools > Jam Template > Validate Story Scripts`。每个脚本的所有可能
   首句路径（含跨脚本跳转）必须有 `PortraitId`；JSON 迁移线程完成前出现此类
   缺失错误是预期状态。
3. 进入 Play Mode，靠近一个已配置的 `WorldStoryInteractable`，确认无需按 F
   就会启动其剧情；如果目标在靠近后才由不可交互变为可交互，使用 F 作为兜底。
4. 对当前以 Choice 为起始节点的 `prop_*.json`，确认 `DialoguePanel` 从隐藏
   变为显示并直接生成选项列表。
5. 第一次 Submit 应立即补全打字效果，第二次 Submit 应进入下一节点。
6. 到达 Choice 节点时，确认按钮数量和 JSON 中可见选项一致。
7. 使用键盘/手柄导航和 Submit，再用鼠标点击验证同一选项。
8. 到达 End 节点后，确认面板隐藏且玩家重新获得控制。
9. 用测试剧情依次播放 `0 → 1 → 3 → 省略 → 未知 ID`，确认结果为
   `face01 → face01 → face03 → face03 → face03`，未知 ID 只产生警告。
10. 启动另一段剧情，确认先恢复 `Image.Source Image`，再由首句
    `PortraitId` 切换；结束后玩家控制正常恢复。
11. 重复打开剧情，确认没有重复按钮、重复回调或残留的上一句文字。
12. 用测试剧情播放 CG `1 → 省略 → 0`，确认显示编号 1、跨下一句和 Choice
    保持、随后隐藏；再在最后一句的 `AfterActions` 添加 `HideCg`，确认完成
    该句后、进入下一节点前 CG 已隐藏并清空 Sprite。
13. 请求不存在、重复或 Sprite 为空的 CG 编号，确认只记录含脚本、节点和编号
    的警告，剧情继续且当前 CG 不变。
14. 分别正常结束、取消、触发故障并启动另一段剧情，确认
    `Canvas/StoryCgImage` 都不会残留；周目结束的 `Canvas/EndingRoot` 仍按原
    流程工作。
15. 使用以 Action 开头的测试剧情，确认 `TryStart()` 成功后
    `Canvas/DialoguePanel` 立即出现，并在 Dialogue、Action 和 Choice 节点间
    持续显示。
16. 提交选项后确认全部选项按钮立即变为不可交互；到达下一节点后内容正常
    刷新。正常结束、取消或故障后确认面板隐藏，再次启动时没有旧文字或按钮。

## 6. 保存与提交

- 这些人工挂接会修改 `Assets/_Project/Scenes/GamePlay.unity`。
- 如果选择把新增子对象应用回 `DialoguePanel.prefab`，还会修改该 Prefab。
- 仅给现有场景对象添加组件和引用时通常不产生新 `.meta`。
- 新建 `Canvas/StoryCgImage` 和填写 `Cg Bindings` 都需要人类保存
  `GamePlay.unity` 并审查序列化差异。
- 若人类新建脚本、Prefab 或导入艺术资产，必须让 Unity 生成并提交对应 `.meta`。
- 所有字体、立绘、按钮样式及其他表现资源必须由人类制作或提供，遵守比赛的无 AI 艺术资产规则。
