# SureType

SureType 是一个在 Windows 上提示输入状态的小工具：在切换中英文或进入输入框时，让你先确认状态，再开始输入。

**当前版本：0.2.1 候选版。** 新增实时预览、简洁界面与轻量动效；完整输入法兼容性和长期稳定性仍在验证，详见 [验证记录](docs/validation.md)。

## 下载与启动

仓库里的 [SureType 0.2.1](download/SureType-0.2.1.exe) 是 Windows 10/11 x64 自包含单文件程序，不要求另装 .NET。

1. 升级前从托盘退出旧版，再运行新版。
2. 首次运行可在“试一下”页试输入，切换中英文和 CapsLock，点击“完成”。
3. 关闭设置页不会退出；双击托盘图标或再次双击程序可重新打开设置。
4. 右键托盘图标可暂停提示、预览当前状态或退出。

如需自启动，在“设置”中开启。之后登录 Windows 会静默运行。移动程序后，应在新位置重新开启自启动；旧路径不会被当作正常启用。启动参数 `--startup` 用于静默启动。

## 提示含义

| 标记 | 含义 |
| --- | --- |
| 中 | 中文输入法处于中文模式 |
| 英 | 中文输入法处于英文模式 |
| EN | 英文键盘 |
| ⇪ | CapsLock 已开启，独立于中英文标记 |
| ? | 当前输入模式无法读取 |

CapsLock 指示的是开关状态，不保证实际输入字符的大小写；Shift、应用和输入法规则仍可能影响结果。

## 位置和免打扰

“外观”提供屏幕四角与**鼠标旁边**。鼠标旁模式在提示出现时定位，不持续跟随指针；靠近边缘时自动换侧，保持在当前屏幕工作区内。位置默认右上角，大小默认 56，显示时长默认 1.5 秒。

“设置”可分别关闭状态变化提示、输入框焦点提示，并调整焦点提示间隔（默认 8 秒）。全屏免打扰默认开启，也可选择程序的完整 exe 路径加入排除列表。

暂停会立即隐藏当前提示并停止自动提示。手动“预览”仍可使用。恢复默认提示设置会重置位置、样式、大小、提示时机和排除列表，保留界面语言、自启动与首次引导状态。

设置页提供位置和样式预览。切页、按钮和提示有轻量动效；关闭 Windows 界面动画后，SureType 也会停用这些动效。

## 设置与隐私

设置保存在 `%LocalAppData%\SureType\settings.json`，修改后自动保存。损坏文件会被保留为同目录下的 `settings.json.corrupt-*`；保存失败会在设置页或托盘中提示。

程序本地运行，不上传数据，不记录按键或输入文本，不自动切换输入法。焦点识别只检查控件类型、可编辑属性和所属进程；应用排除使用可执行文件路径。本地试输入文字只存在于窗口内，关闭即清空。

## 兼容性限制

部分第三方输入法、自绘控件、远程桌面、管理员应用和安全桌面可能不提供可读状态。读取失败或超时返回未知，不强制提升权限。成功返回但语义不同的第三方 IME 仍需要逐项实测，不能仅凭 API 调用成功认定支持。

当前仅识别中文输入法和英文键盘；其他语言显示未知。完整兼容性矩阵、混合 DPI 和连续切换验证状态见 [验证记录](docs/validation.md)。

## 开发和验证

需要 .NET 8 Windows SDK；仓库本地 SDK 可用时，将下列 `dotnet` 替换为 `.\.dotnet\dotnet.exe`。

```powershell
dotnet run --project tests/SureType.Tests/SureType.Tests.csproj
dotnet run --project tests/SureType.Tests/SureType.Tests.csproj -- --native
dotnet run --project tests/SureType.Tests/SureType.Tests.csproj -- --render artifacts/ui-checks
.\publish-self-contained.ps1
```

发布脚本检查退出码与版本，在独立暂存目录构建，生成带版本号的 ZIP、自包含 exe 和 SHA-256 文件；不会在构建失败时发布旧产物，也不会自动覆盖仓库下载文件。首次发布前需完成 `dotnet restore SureType.sln`，脚本复用已还原依赖。

验证发布包的单实例与启动流程：

```powershell
dotnet run --project tests/SureType.Tests/SureType.Tests.csproj -- --lifecycle artifacts/lifecycle-release artifacts/SureType-0.2.1-win-x64/SureType.exe
```

运行命令时使用绝对路径可避免工作目录差异。离屏渲染和原生测试不代替真实输入法、无 .NET 系统与长期运行验证。
