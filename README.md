# SureType

SureType 是一个轻量的 Windows 输入状态提示工具。它常驻系统托盘，在你进入输入框准备输入时，用屏幕右上角的一枚低打扰小图标提示当前输入状态，减少“先打几个字才发现中英文或大小写错了”的情况。

## 功能

- 识别当前前台窗口的键盘布局/输入源。
- 支持中文 IME，并尽量区分中文输入法内部的“中文模式 / 英文模式”。
- 支持英文小写 / CapsLock 大写提示。
- 只在一段时间内第一次进入输入框时显示提示，连续打字不会反复弹窗。
- 输入法状态或 CapsLock 状态变化时会短暂提示。
- 右上角浮层不抢焦点，不影响正常输入。
- 系统托盘菜单支持暂停/恢复、手动显示当前状态、退出。

## 状态图标

第一版内置 5 种极简矢量状态图标：

- 中文输入
- 中文输入法内英文模式
- 英文小写
- 英文大写
- 未知/检测失败

图标资源位于 `src/SureType/Resources/StatusAssets.xaml`，后续可以替换为自定义 PNG/SVG 或新的 WPF 矢量图。

## 系统要求

- Windows 10/11 x64
- 开发构建需要 .NET 8 SDK
- 使用自包含发布包时，目标电脑不需要预装 .NET

## 构建

```powershell
dotnet build
```

## 运行测试

```powershell
dotnet run --project tests/SureType.Tests/SureType.Tests.csproj -c Release
```

当前测试覆盖状态到图标资源的映射，以及输入框焦点提示的冷却策略。

## 打包为单文件 EXE

```powershell
.\publish-self-contained.ps1
```

输出文件：

```text
artifacts\SureType-win-x64-single-exe\SureType.exe
```

发布配置会生成自包含、单文件、压缩后的 Windows x64 EXE。当前体积约 `62.78 MB`。重新打包前请先从系统托盘退出正在运行的 SureType，否则旧的 `SureType.exe` 可能因被占用而无法覆盖。

## 当前局限

- 中文 IME 内部中英文模式依赖 Windows IMM/IME conversion mode。微软拼音等常见输入法通常可用，但部分第三方输入法可能不暴露标准状态，此时会回退到未知状态。
- 输入框焦点识别基于 Windows UI Automation。大部分原生和现代应用可识别，少数自绘控件可能无法准确上报输入焦点。
- 第一版只支持 Windows x64。

## 开发结构

```text
src/SureType/              WPF 托盘应用
src/SureType/Services/     输入状态检测、焦点监听、托盘服务
src/SureType/Windows/      右上角浮层窗口
src/SureType/Resources/    内置状态图标资源
tests/SureType.Tests/      轻量测试入口
```

## License

暂未指定许可证。