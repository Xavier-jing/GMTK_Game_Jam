# Unity 内容接入指南

## 1. 适用对象

本文指导策划和美术在 Unity `2022.3.62f3c1` 中，将人类制作的艺术资产接入现有 C# 框架。

所有 Unity 资产变更必须由人类在编辑器中完成。智能体只能修改 `.md`/`.cs`，不能保存场景、Prefab、材质、动画、AudioMixer、导入设置或 `.meta`。

## 2. 首次打开与基础检查

1. 使用 Unity Hub 确认编辑器版本为 `2022.3.62f3c1`。
2. 打开仓库根目录，等待 Unity 完成导入和 C# 编译。
3. 先处理 Console 中的编译错误；有编译错误时不要继续保存场景或 Prefab。
4. 执行 `Tools > Jam Template > Sync Build Settings`。
5. 在 Build Settings 中确认以下顺序存在并启用：
   - `Assets/_Project/Scenes/Boot.unity`
   - `Assets/_Project/Scenes/MainMenu.unity`
   - `Assets/_Project/Scenes/GamePlay.unity`
   - `Assets/_Project/Scenes/SandBox.unity`
6. 从 `Boot` 场景进入 Play Mode，确认能自动进入主菜单。

`BuildSettingsSceneSync` 通常会自动同步这些场景；菜单命令用于人工确认或修复。

## 3. 当前场景流程

| SceneId | 场景文件 | 责任 |
| --- | --- | --- |
| `Boot` | `Boot.unity` | 创建持久化服务、显示加载遮罩并进入主菜单 |
| `MainMenu` | `MainMenu.unity` | 主菜单、设置、开始游戏和退出 |
| `Gameplay` | `GamePlay.unity` | HUD、暂停、设置和实际游戏内容 |
| `Sandbox` | `SandBox.unity` | 独立调试或玩法验证 |

运行时入口由 `Bootstrap` 和 `AppContext` 自动创建，不需要在每个场景手工放置 `AppContext`。

不要直接重命名上述场景文件。场景路径同时存在于 `SceneLoader` 和 `BuildSettingsSceneSync` 的 C# 映射中；需要改名时先由程序修改映射，再由人类在 Unity 中移动场景。

## 4. 导入人工制作的资产

1. 按 [`ART_ASSET_SPEC.md`](./ART_ASSET_SPEC.md) 确认来源、命名和技术规格。
2. 由人类把文件复制或拖入 `Assets/_Project/Art` 或 `Assets/_Project/Audio` 的合适子目录。
3. 在 Inspector 中配置 Import Settings，点击 `Apply`。
4. 检查 Unity 生成的 `.meta`，不要删除或重新生成已被场景引用的 `.meta`。
5. 先在 `SandBox` 或临时人类测试场景验证显示、动画或声音，再接入正式场景。
6. 完成资产交付记录，连同资产和 `.meta` 一起提交。

## 5. UI 框架挂接

### 5.1 通用结构

每个包含可切换界面的场景需要：

1. 一个 Canvas 和 EventSystem。
2. Canvas 下的 `UIRoot` 或等价根对象。
3. 根对象上的 `UIService`。
4. 每个界面根对象上的 `ScreenBase` 子类，并设置唯一 `ScreenId`。
5. 场景控制器引用该 `UIService`，或保持为空让代码从子对象自动查找。

同一个 `UIService` 下不要出现重复 `ScreenId`。界面可以初始禁用，`UIService` 会在 `Awake` 中扫描包含禁用对象在内的所有 `ScreenBase` 子对象。

### 5.2 MainMenu

`MainMenu.unity` 的关键挂接：

- `MainMenuPanel`：挂 `MainMenuScreen`，`screenId = MainMenu`。
- `MainMenuScreen.startButton`：拖入开始游戏 Button，默认进入 `Gameplay`。
- `MainMenuScreen.sandboxButton`：拖入 SandBox Button，默认进入 `Sandbox`。
- `MainMenuScreen.SettingsButton`：拖入设置 Button。
- `MainMenuScreen.exitButton`：拖入退出 Button。
- `Controller`：挂 `MainMenuSceneController`，`landingScreen = MainMenu`，引用场景中的 `UIService`。
- `SettingsPanel.prefab`：挂 `SettingsScreen`，`screenId = Settings`。

替换 Button 图片或文本时不要删除 Button、`MainMenuScreen` 或已经存在的序列化引用。若必须重建对象，应在 Inspector 中重新拖入四个 Button 引用。

### 5.3 Gameplay、HUD 与暂停

`GamePlay.unity` 的关键挂接：

- `UIRoot`：挂 `UIService`。
- HUD 根对象：挂 `HudScreen`，`screenId = Hud`，把提示文本拖入 `hintText`。
- `PausePanel`：挂 `PauseScreen`，`screenId = Pause`。
- `PauseScreen.resumeButton`：拖入 Resume Button。
- `PauseScreen.mainMenuButton`：拖入返回主菜单 Button。
- `PauseScreen.settingsButton`：拖入 Settings Button。
- `SettingsPanel.prefab` 实例：保持 `screenId = Settings`，并确认三个 Slider 与 Back Button 引用。
- `UIController`：挂 `GameplayUIController`，设置 `gameplayScreen = Hud`、`pauseScreen = Pause`，引用当前 `UIService` 和玩家的 `PlayerInteractor`。
- `GameplayUIController.interactionPromptPrefix`：默认 `Press Enter / Space / A: `；如需改文案只修改此前缀，不要把按键文字写入每个交互物。

运行时 `Escape` 或手柄 Start 会切换暂停。暂停使用 `Time.timeScale = 0`，因此暂停菜单动画若需要继续播放，必须由程序显式使用不受缩放时间影响的实现。

### 5.4 SettingsPanel

`SettingsScreen` 需要以下 Inspector 引用：

- `masterSlider`
- `bgmSlider`
- `sfxSlider`
- `backButton`

三个 Slider 的范围建议保持 `0–1`。如果替换整个设置面板，应重新确认 Slider 引用和 Back Button；不要只检查视觉效果。

### 5.5 LoadingRoot

`LoadingScreen` 必须位于场景根 GameObject；如果有父对象，运行时会被代码移到根节点。

关键引用：

- 同对象的 `CanvasGroup` → `canvasGroup`
- 加载 Slider → `progressSlider`
- 百分比 TextMeshPro 文本 → `progressText`
- `startOpaque` 在 Boot 流程中通常保持启用

可调整的表现参数包括 `fadeInDuration`、`fadeOutDuration`、`minimumVisibleDuration` 和 `progressUnitsPerSecond`。这些参数由人类在 Inspector 调整并通过完整场景切换验证。

`LoadingScreen` 使用 `DontDestroyOnLoad`。各业务场景不要再放置第二个实例。

## 6. Sprite、Prefab 与动画接入

### SpriteRenderer

1. 选中人类维护的目标 GameObject 或 Prefab。
2. 在 `SpriteRenderer.sprite` 中拖入已验证的 Sprite。
3. 检查 Sorting Layer、Order in Layer、Flip、颜色和 Transform。
4. 在目标相机与实际 Game View 分辨率中验证。

### UI Image

1. 把 Sprite 拖入 `Image.Source Image`。
2. 按需求选择 `Simple`、`Sliced` 或 `Filled`。
3. 检查 Preserve Aspect、锚点、Raycast Target 和 Button 的 Target Graphic。
4. 在至少两个宽高比下检查布局。

### Prefab 与动画

- Prefab、Animation Clip、Animator Controller 和动画参数由人类在 Unity 中创建或修改。
- 若动画需要玩法状态或事件，先把参数名、值域、触发时机提交给程序。
- 不在 Animation Event 中调用不稳定的私有实现，也不让动画事件决定伤害、胜负等核心逻辑。

## 7. 音频框架挂接

### 7.1 AudioMixer 固定契约

当前代码通过以下路径加载 Mixer：

`Assets/_Project/Resources/Audio/MasterMixer.mixer`

Mixer 必须包含：

- Group：`BGM`
- Group：`SFX`
- Exposed Parameter：`MasterVolume`
- Exposed Parameter：`BgmVolume`
- Exposed Parameter：`SfxVolume`

名称区分大小写。修改名称前必须先由程序同步 `AudioService` 常量。

### 7.2 播放接口

程序可使用：

```csharp
AppContext.Instance.Audio.PlayBgm(clip, fadeDuration);
AppContext.Instance.Audio.StopBgm(fadeDuration);
AppContext.Instance.Audio.PlaySfx(clip);
AppContext.Instance.Audio.PlaySfx(clip, volume, pitchMin, pitchMax);
```

当前 `AudioService` 不是 `MonoBehaviour`，也没有可供策划直接在 Inspector 拖入 `AudioClip` 的桥接组件。策划或美术不能仅通过 UnityEvent 直接配置播放。

需要新音频时，向程序提交以下信息：

- AudioClip 的项目路径和类型：BGM/SFX。
- 播放触发：进入场景、按钮点击、角色事件或其他状态。
- 是否循环、淡入淡出时间、音量和 Pitch 范围。
- 同一时刻可能的最大并发数。
- 停止或切换条件。

程序提供 `.cs` 挂接点后，由人类在 Unity 中生成 `.meta`、挂组件并拖入 AudioClip。

## 8. 输入框架说明

当前输入由 `InputReader` 在 C# 中创建，没有 `.inputactions` 资产：

- Move：WASD、方向键、Gamepad Left Stick。
- Pause：Escape、Gamepad Start。
- Submit：Enter、Space、Gamepad South Button。
- Cancel：Escape、Gamepad East Button。

策划需要增改按键时应提交给程序修改 `InputReader.cs`。不要新建 Input Actions Asset 形成第二套未同步输入配置。

## 9. 2D 单目标交互系统

### 9.1 代码契约

可交互物由 `MonoBehaviour` 实现 `IInteractable`：

```csharp
public readonly struct InteractionContext
{
    public GameObject Interactor { get; }
    public Inventory Inventory { get; }
}

public interface IInteractable
{
    Transform InteractionPoint { get; }

    string GetInteractionPrompt(InteractionContext context);
    bool CanInteract(InteractionContext context);
    void Interact(InteractionContext context);
}
```

- `GetInteractionPrompt` 根据上下文返回动作或锁定文字。可执行提示只返回动作文字，例如 `Open chest`，HUD 会添加按键前缀；锁定提示直接返回完整文字，例如 `需要地下室钥匙`。
- `InteractionPoint` 必须返回非空 Transform；没有单独交互点时返回组件自身 `transform`。
- `CanInteract` 只判断当前是否可交互，可以通过 `context.Inventory` 查询道具或隐藏条件。
- `Interact` 执行一次即时交互，不读取输入，不访问 HUD。
- 同一个 Collider 层级只应存在一个 `IInteractable` 实现；系统使用遇到的第一个实现。

### 9.2 GamePlay 场景挂接

目标场景：`Assets/_Project/Scenes/GamePlay.unity`。

1. 在人类维护的 `Player` 根 GameObject 上确认存在 `Rigidbody2D` 和移动碰撞体。
2. 在同一个 `Player` 根对象上添加 `PlayerInteractor`。
3. 再添加一个专用于交互的 `CircleCollider2D`：
   - `Is Trigger = true`
   - `Radius = 1.5`，建议按角色尺寸在 `1.0–2.5` 之间调整
   - 不要把角色的移动碰撞体拖入交互字段
4. 把上述 Trigger Collider 拖入 `PlayerInteractor.interactionSensor`。
5. 交互 Sensor 必须和 `PlayerInteractor` 位于同一个 GameObject；不满足时组件会禁用并输出带对象名的错误。
6. 选中场景根对象 `UIController`，把 `Player` 上的 `PlayerInteractor` 拖入 `GameplayUIController.playerInteractor`。
7. 选中现有 `HintPanel`，把负责显示提示的 `TMP_Text` 拖入 `HudScreen.hintText`。当前场景该字段为空；未挂接时交互仍会执行，但不会显示文字。

### 9.3 可交互物挂接

1. 在目标物体根对象或 Collider 的父对象上添加实现 `IInteractable` 的具体 `MonoBehaviour`。
2. 确认目标层级至少有一个启用的 `Collider2D`；可以是实体 Collider 或 Trigger。
3. `InteractionPoint` 推荐引用物体中心附近、玩家可自然接近的位置；没有特殊需求时返回自身 Transform。
4. 进入玩家 Sensor 后，系统优先选择最近的可执行物体；只有范围内没有可执行物体时，才选择最近的锁定物体显示条件提示。
5. 多个 Collider 可以属于同一交互组件，系统会去重；Collider 全部离开后才清除候选。
6. 一次性物体应在 `Interact` 成功后自行记录不可用状态，让后续 `CanInteract` 返回 `false`。
7. 需要背包条件时，在具体交互组件中添加一个序列化 `ItemRequirement` 字段；正常提示和条件提示仍由具体组件决定。

失败表现：

- Sensor 未引用、不在同对象或未启用 `Is Trigger`：`PlayerInteractor` 禁用并记录错误。
- `InteractionPoint` 返回 `null`：该物体被忽略，并只记录一次警告。
- `GameplayUIController.playerInteractor` 未引用：HUD 回退默认提示并记录警告。
- 交互物被禁用或销毁：玩家自动切换到下一个候选或清空提示。

`SandBox.unity` 当前只有 Main Camera。测试前由人类添加测试 Player、HUD 和至少两个交互物；不要由智能体保存场景或创建测试艺术资产。

新增或更新交互 `.cs` 后，必须由 Unity `2022.3.62f3c1` 生成人类检查过的 `.meta`，并与场景引用一并提交。已有具体交互脚本必须迁移到新签名后再进入 Play Mode。

### 9.4 交互冒烟

- [ ] 单个可用物进入范围时显示提示，离开时恢复默认 HUD。
- [ ] 两个可用物同时在范围内时只提示最近者，移动后能稳定切换。
- [ ] Enter、Space、Gamepad South 每次按下只调用当前物体一次。
- [ ] 后续再次按键可以重复与仍然可用的同一物体交互。
- [ ] 范围内同时存在锁定和可执行物体时，始终选择最近的可执行物体。
- [ ] 范围内只有锁定物体时显示最近目标的锁定提示，Submit 不调用 `Interact`。
- [ ] 禁用或销毁的物体不会保留为当前目标。
- [ ] 同一交互物的多个 Collider 不会导致重复调用。
- [ ] 暂停期间 Submit 不执行交互，恢复后提示正确。

## 10. 唯一道具背包

### 10.1 道具资产

每个 `ItemDefinition` 资产代表一种唯一道具，运行时直接用资产引用判断身份。由人类在 Unity 中执行：

1. 在 Project 窗口选择 `Create > Jam > Inventory > Item Definition`。
2. 建议把资产保存到人类维护的 `Assets/_Project/Data/Items/`。
3. 配置以下字段：
   - `Display Name`：背包 UI 展示名。
   - `Description`：道具说明。
   - `Icon`：人类制作或比赛许可的 Sprite；隐藏条件道具可留空。
   - `Show In Managed Inventory`：开启时显示并占一格，关闭时仍属于玩家但不显示、不占格。
   - `Can Discard`：只控制玩家从背包 UI 主动丢弃；系统消费不受影响。
4. 钥匙等普通道具通常开启 `Show In Managed Inventory`；剧情条件标记关闭该项，并建议同时关闭 `Can Discard`。
5. 每一种道具只创建一个定义资产，不要复制出多个内容相同但引用不同的资产。

AI 不创建或修改 `.asset`、图标及其 `.meta`。新增脚本导入和以上资产必须由人类在 Unity `2022.3.62f3c1` 中创建、检查并提交 `.meta`。

### 10.2 背包运行规则

- 权威背包为 `AppContext.Instance.Inventory`，默认可管理容量 20。
- `TryAdd` 返回明确结果；只有 `Added` 表示取得成功。
- `Contains` 可同时查询可见道具与隐藏条件。
- `VisibleItems` 是只读、按取得顺序排列的 UI 数据源。
- `TryDiscard` 只允许可见且 `CanDiscard` 的道具。
- `TryConsume` 供玩法逻辑使用，可以移除不可丢弃或隐藏道具。
- 成功添加、移除或非空清空时，`Changed` 只发布一次；失败操作不发布。
- `AppContext` 跨 `GamePlay` 与 `SandBox` 场景保留背包。MainMenu 成功接受 Start 或 SandBox 加载请求后清空；暂停菜单成功接受返回 MainMenu 请求后清空。加载请求被拒绝时不清空。
- 当前没有磁盘存档；直接从 `GamePlay` 场景启动时由新建的 `AppContext` 提供空背包。

背包 UI 在后续实现时应订阅 `Inventory.Changed`，每次通知重新读取 `VisibleItems`、`ManagedSlotCapacity` 和 `ItemDefinition.CanDiscard`；销毁 UI 控制器时必须退订。

### 10.3 交互条件接入

具体交互组件可按以下模式组合一个 `ItemRequirement`：

```csharp
[SerializeField]
private ItemRequirement requirement;

public string GetInteractionPrompt(InteractionContext context)
{
    return requirement.IsSatisfied(context.Inventory)
        ? "打开"
        : requirement.BlockedPrompt;
}

public bool CanInteract(InteractionContext context)
{
    return requirement.IsSatisfied(context.Inventory);
}

public void Interact(InteractionContext context)
{
    if (!CanInteract(context))
    {
        return;
    }

    // 先确认本次玩法操作确实能够完成，再消费需求道具。
    if (!requirement.TryConsume(context.Inventory))
    {
        return;
    }

    CompleteInteraction();
}
```

- `Required Item` 留空表示没有背包条件。
- `Consume On Success` 关闭时道具/条件永久保留；开启时成功交互消费一次。
- `Blocked Prompt` 应填写玩家可见的完整提示，不要暴露隐藏条件资产的内部名称。
- `TryConsume` 应放在所有非消耗条件确认之后、产生不可逆玩法结果之前；消费失败必须中止本次交互。
- v1 每个交互物只配置一个道具条件；AND/OR 条件需要另行设计，不要在 Inspector 堆叠未约定的规则。

### 10.4 背包与条件冒烟

- [ ] 同一道具首次添加返回 `Added`，再次添加返回 `AlreadyOwned`。
- [ ] 第 20 个可见道具可加入，第 21 个返回 `ManagedSlotsFull`。
- [ ] 可见槽位满时隐藏条件仍可加入；`Contains` 为 true，`VisibleItems` 中不存在。
- [ ] 不可丢弃和隐藏道具不能经 `TryDiscard` 移除，但可以经 `TryConsume` 移除。
- [ ] 可见道具保持取得顺序，消费后从 `VisibleItems` 删除。
- [ ] 每次成功变更只触发一次 `Changed`，失败操作不触发。
- [ ] `GamePlay` 与 `SandBox` 之间由玩法加载时保留；开始新局和成功返回 MainMenu 时清空。
- [ ] 缺少钥匙或隐藏条件时显示锁定提示且 Submit 无效；取得后最迟下一帧切换到正常提示。
- [ ] 开启消费的条件在成功交互后移除；未开启消费的条件保留。

## 11. 程序挂接申请模板

```md
### 内容挂接申请

- 资产路径：
- 类型：Sprite / UI / Animation / BGM / SFX / 其他
- 目标场景或 Prefab：
- 目标 GameObject：
- 触发条件：
- 停止或隐藏条件：
- 需要的 Inspector 字段：
- 参数范围和默认值：
- 失败时预期行为：
- 人工验收步骤：
```

## 12. 人工冒烟清单

每次正式资产接入后，从 `Boot` 开始验证：

- [ ] Boot 加载遮罩正常显示并淡出。
- [ ] 自动进入 MainMenu，无第二个 LoadingScreen 或 EventSystem。
- [ ] MainMenu 的 Start、SandBox、Settings、Exit 引用完整。
- [ ] GamePlay 中 HUD 正常，Escape/Start 能打开和关闭 Pause。
- [ ] Settings 三个 Slider 可修改音量，Back 返回正确界面。
- [ ] 返回 MainMenu 后时间缩放恢复为 `1`。
- [ ] BGM 切换无明显爆音，SFX 路由到正确 Mixer Group。
- [ ] SandBox 可独立打开并用于内容验证。
- [ ] Console 没有 Missing Script、Missing Reference 或重复 ScreenId 警告。
- [ ] 新增资产及 `.meta` 均已由人类检查并纳入提交。
