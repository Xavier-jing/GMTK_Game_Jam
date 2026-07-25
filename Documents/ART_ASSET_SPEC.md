# 美术与音频资产技术规格

## 1. 用途与责任边界

本文供美术、音频和负责内容接入的策划使用。项目使用 Unity `2022.3.62f3c1`、2D URP。

所有游戏艺术资产必须由人类创作或来自比赛规则允许、许可证明确的非 AI 来源。禁止使用生成式 AI 制作或修改图像、模型、字体、动画、音乐、音效、配音和视频。

- 美术/音频负责人：创作、导出、确认来源与许可证。
- 策划：说明用途、出现位置、触发条件和验收标准。
- 程序：提供 C# 挂接点，不创建或修改艺术内容。
- 负责接入的人类成员：在 Unity Editor 中导入、配置并提交资产及 `.meta`。

任何来源或制作过程不确定的资产不得进入提交候选版本。

## 2. 项目级风格基线

下表必须由人类负责人确认。未确认前不要为整个项目批量修改导入设置。

| 项目 | 团队决定 | 负责人 |
| --- | --- | --- |
| 目标画面分辨率与宽高比 | 待策划/美术确认 | 策划 |
| 像素画或非像素画 | 待美术确认 | 美术 |
| 世界 Sprite 的 Pixels Per Unit | 待美术确认并保持统一 | 美术 |
| UI 参考分辨率与 Canvas Scaler 规则 | 待 UI 负责人确认 | 美术/程序 |
| Sprite Atlas 分组与最大尺寸 | 资产数量稳定后确认 | 美术/程序 |
| 目标发布平台与音频内存预算 | 待团队确认 | 程序 |

不要用单个资产的局部效果反向改变项目级 PPU、分辨率或颜色规范。

## 3. 建议目录与命名

所有实际资产由人类放入现有项目目录：

```text
Assets/_Project/
├── Art/          # Sprite、UI、字体、动画源内容
├── Audio/        # BGM、SFX、环境音、配音
├── Materials/    # 人类在 Unity 中创建的材质
├── Prefabs/      # 人类维护的 Prefab
├── Resources/    # 仅限代码明确通过 Resources.Load 使用的内容
└── Scenes/       # 人类维护的场景
```

命名建议：

- 使用英文、数字和下划线，不使用空格或临时后缀。
- 名称表达内容和用途，例如 `Player_Idle_01`、`UI_Button_Primary`、`SFX_Player_Hit_01`。
- 类型前缀保持团队一致，不在同一目录混用多套规则。
- 迭代版本由 Git 管理，不使用 `final_final_v2` 一类文件名。
- 同一 Sprite Sheet 的帧保持统一尺寸、透明边界和 Pivot 约定。

除非代码明确要求，不要把资产放入 `Resources`。当前音频混音器是必要例外：

`Assets/_Project/Resources/Audio/MasterMixer.mixer`

## 4. 2D 图像导入决策表

以下设置由人类在 Inspector 的 Import Settings 中完成。

| 内容类型 | Texture Type | Filter Mode | Compression/Mipmap | 关键检查 |
| --- | --- | --- | --- | --- |
| 像素画世界 Sprite | `Sprite (2D and UI)` | `Point (no filter)` | 通常关闭 Mip Maps；压缩需逐平台检查失真 | PPU 统一、像素边缘清晰、Pivot 正确 |
| 非像素世界 Sprite | `Sprite (2D and UI)` | 通常 `Bilinear` | 根据目标平台测试压缩；2D 正交视角通常无需 Mip Maps | Alpha、边缘、最大尺寸和颜色正确 |
| 单张 UI 图 | `Sprite (2D and UI)` | 按画风选 Point/Bilinear | 通常关闭 Mip Maps | Border、Mesh Type、透明边缘 |
| 可九宫格 UI | `Sprite (2D and UI)` | 按画风选择 | 通常关闭 Mip Maps | 在 Sprite Editor 中设置 Border，并实际拉伸测试 |
| Sprite Sheet | `Sprite (2D and UI)` + `Multiple` | 与同类单帧一致 | 与同类单帧一致 | Slice、帧序、命名、Pivot、透明边界 |

注意：

- 不为尚未确定的画风擅自选择固定 PPU。
- 像素画必须检查相机缩放和移动是否产生像素抖动。
- 半透明边缘在目标背景色和 Game View 缩放下都要检查。
- 修改已有 Sprite 的切片、Pivot 或 PPU 可能影响场景和动画，修改前先确认引用范围。

## 5. UI 资产检查

- 优先让 `Image` 引用 Sprite；不要在场景中保存重复纹理副本。
- 需要拉伸的面板和按钮背景使用九宫格，并在极端尺寸下检查四角。
- 文本优先使用项目现有 TextMeshPro 体系；新增字体及 Font Asset 必须由人类确认许可证并在 Unity 中创建。
- 图标在正常、Hover、Pressed、Disabled 状态下均应清晰。
- Canvas 中检查锚点、不同宽高比、安全区域和 UI 缩放。
- 替换 UI Sprite 后，确认 Button 的 Target Graphic、颜色过渡和 Raycast Target。

## 6. 动画与 Sprite Sheet

- 美术提交时附上帧率、循环方式、Pivot、帧序和事件帧需求。
- 角色同组动画保持画布尺寸和 Pivot 一致，避免播放时跳动。
- Animation Clip、Animator Controller、Override Controller 和 Prefab 均由人类在 Unity 中创建或修改。
- 程序只提供状态、参数或事件接口；参数名由程序和动画负责人共同确认后再挂接。
- 动画事件只调用稳定、明确的公共 C# 方法，不直接承担核心玩法判定。

## 7. 音频导入决策表

| 内容类型 | 常用 Load Type 起点 | 常用压缩起点 | 验证重点 |
| --- | --- | --- | --- |
| 短 SFX | `Decompress On Load` | PCM 或 ADPCM，按内存与质量测试 | 延迟、同时播放、峰值、噪声 |
| 较长 BGM | `Streaming` | Vorbis，质量由目标平台试听决定 | 循环点、切换、加载尖峰、文件大小 |
| 中短环境音 | `Compressed In Memory` 或按实测调整 | Vorbis/ADPCM | 循环接缝、CPU、内存 |
| 语音 | 根据长度和数量选择 | Vorbis，保留清晰度 | 响度、底噪、字幕同步 |

音频负责人还需确认：

- 源文件没有削波，响度在同类资产间一致。
- 是否保留立体声；只有经过试听确认后才启用 `Force To Mono`。
- Loop Clip 的首尾没有明显爆音或空白。
- BGM 与 SFX 分别路由到 `BGM` 和 `SFX` Mixer Group。
- 设置界面能控制 `MasterVolume`、`BgmVolume`、`SfxVolume` 三个 Exposed Parameter。

## 8. 单项资产交付标准

资产交给接入人员前必须同时提供：

1. 最终导出文件和可追溯的人类源文件位置。
2. 作者、来源、许可证和无 AI 生成/修改声明。
3. 游戏用途、目标场景或 Prefab、期望显示/播放条件。
4. 导入类型、PPU/Pivot/切片或音频 Load Type 等设置。
5. 已知限制，以及需要程序提供的字段、事件或状态。
6. 完成的 [`ASSET_HANDOFF_CHECKLIST.md`](./ASSET_HANDOFF_CHECKLIST.md) 记录。

