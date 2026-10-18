# VRC_FANUC

[中文](#中文说明) | [English](#english)

---

## 中文说明

### 项目简介

`VRC_FANUC` 是一个基于 **.NET Framework 4.8**、**Tecnomatix Engineering** 和 **RoboGuide SNPX** 通信的 Tecnomatix 扩展项目。

项目通过 RoboGuide 的 SNPX 服务读取 FANUC 机器人的关节轴值、DI 和 DO 信号，并将数据同步到 Tecnomatix 中；同时支持将 Tecnomatix 的 PLC-to-Robot 信号写回 RoboGuide 的 DI。

### 主要功能

- 在 Tecnomatix 中选择目标机器人。
- 自动查找本机 TCP `60008` 监听端点。
- 将监听到的 IP 地址和端口保存到：
  - `VRC_ip`
  - `VOR_port`
- 通过 RJi.FanucRobot.Interface 连接 RoboGuide SNPX 服务。
- 初始化读取机器人当前关节轴值。
- 初始化读取 RoboGuide DI/DO 信号的：
  - 地址
  - 名称
  - Comment
  - 当前值
- 将 RoboGuide 轴值写入 Tecnomatix 机器人姿态。
- RoboGuide 返回的轴值为角度，写入 Tecnomatix 前转换为弧度。
- 创建或复用 Tecnomatix PLC 信号，避免重复创建。
- 将 RoboGuide DO 映射到 Tecnomatix Robot-to-PLC / FromRobot 信号。
- 将 RoboGuide DI 映射到 Tecnomatix PLC-to-Robot / ToRobot 信号。
- 初始化时使用 RoboGuide 当前 DI/DO 值初始化 Tecnomatix 信号。
- 使用 Connect / Disconnect 按钮控制监听。
- 监听期间按照约 `100 ms` 周期同步：
  1. 检查 Tecnomatix PLC-to-Robot 信号变化。
  2. 将变化值写入 RoboGuide DI。
  3. 读取 RoboGuide DI/DO。
  4. 将变化的 RoboGuide DO 更新到 Tecnomatix Robot-to-PLC 信号。
  5. 轴值按照约 `100 ms` 周期更新到 Tecnomatix。
- 只有值发生变化的信号才会被更新。
- 监听在后台任务中执行，不直接阻塞窗体 UI 线程。
- 关闭窗体或点击 Disconnect 时取消监听并断开 SNPX 连接。

### 信号映射

| RoboGuide 信号 | Tecnomatix 信号 | 方向 | 说明 |
|---|---|---|---|
| `DO[index]` | Robot-to-PLC / FromRobot | RoboGuide → Tecnomatix | RoboGuide DO 变化后更新 Tecnomatix 信号 |
| `DI[index]` | PLC-to-Robot / ToRobot | Tecnomatix → RoboGuide | Tecnomatix 值变化后写入 RoboGuide DI |

> 本项目按照 RoboGuide 的实际信号方向实现：RoboGuide DO 是机器人输出，RoboGuide DI 是机器人输入。

### 监听流程

监听期间每一轮的逻辑顺序如下：

```text
读取 Tecnomatix PLC-to-Robot 信号
		↓
检测值是否变化
		↓
将变化值写入 RoboGuide DI
		↓
写入完成
		↓
读取 RoboGuide DI / DO
		↓
检测 RoboGuide DO 是否变化
		↓
将变化的 DO 更新到 Tecnomatix Robot-to-PLC
		↓
必要时更新机器人轴姿态并刷新 Tecnomatix 显示
```

轴值和信号的目标调度周期约为 `100 ms`。实际周期会受到以下因素影响：

- RoboGuide 网络通信耗时；
- DI/DO 读取点位数量；
- Tecnomatix API 执行耗时；
- 当前机器性能和线程调度。

因此该周期不是硬实时周期。

### 技术栈

- Visual Studio 2026
- C#
- .NET Framework 4.8
- Windows Forms
- Tecnomatix Engineering API
- Tecnomatix Engineering UI API
- RoboGuide SNPX
- `RJi.FanucRobot.Interface` 1.2.2

### 目录结构

```text
VRC_FANUC/
├── VRC_FANUC.slnx
├── VRC_FANUC/
│   ├── VRC_FANUC.csproj
│   ├── TxVRCRoboGuideComm.cs
│   ├── TxVRCRoboGuideComForm.cs
│   ├── TxVRCRoboGuideComForm.Designer.cs
│   ├── TxVRCRoboGuideComForm.resx
│   ├── StringTable.resx
│   ├── StringTable.Designer.cs
│   ├── Properties/
│   └── vrc_png.png
└── README.md
```

### 环境要求

1. Windows 操作系统。
2. Visual Studio 2026 或兼容版本。
3. .NET Framework 4.8 Developer Pack。
4. Tecnomatix Engineering，当前项目使用的默认安装目录为：

```text
C:\Program Files\Tecnomatix_2402\eMPower\
```

5. RoboGuide，并启用可用的 SNPX 服务。
6. RoboGuide 服务监听 TCP `60008` 端口。
7. `RJi.FanucRobot.Interface.dll`，当前使用 `netstandard2.0` 版本。

### 依赖程序集

当前项目中的引用包括：

- `Tecnomatix.Engineering.dll`
- `Tecnomatix.Engineering.Ui.dll`
- `RJi.FanucRobot.Interface.dll`

当前项目文件中的引用路径是本机绝对路径。克隆仓库到其他电脑后，需要根据本机安装位置修改 `VRC_FANUC.csproj`。

建议不要在公开 GitHub 仓库中上传受 Tecnomatix 许可限制的商业程序集。可以通过以下方式处理：

- 在 README 中说明安装要求；
- 使用本机安装目录引用 Tecnomatix 程序集；
- 将 RJi 程序集放在经过许可确认的 `libs/` 目录；
- 或使用团队内部 NuGet 源管理依赖。

### 编译步骤

1. 安装上述环境和依赖。
2. 使用 Visual Studio 打开 `VRC_FANUC.slnx`。
3. 检查 `VRC_FANUC.csproj` 中的程序集路径。
4. 执行 __生成 > 生成解决方案__。
5. 编译输出默认位于：

```text
VRC_FANUC/bin/Debug/
VRC_FANUC/bin/Release/
```

### 在 Tecnomatix 中使用

1. 编译项目生成扩展程序集。
2. 按照 Tecnomatix 的扩展部署方式加载 `VRC_FANUC.dll`。
3. 在 Tecnomatix 命令菜单中执行 RoboGuide 通信命令。
4. 在窗口中选择目标机器人。
5. 等待轴值、DI/DO 和信号表初始化完成。
6. 初始化成功后，`Connect` 按钮变为可用。
7. 点击 `Connect` 开始监听。
8. 监听开始后按钮显示为 `Disconnect`。
9. 点击 `Disconnect` 停止监听并断开 RoboGuide 连接。

### 重要注意事项

- 初始化阶段会读取 DI/DO 各最多 `4096` 个点位。
- 点位数量较大时，初始化和一次读取可能需要较长时间。
- 信号初始化前会查询 Tecnomatix PLC 信号表。
- 已存在的信号不会重复创建。
- 已存在信号的监听过程只更新值，不重复创建信号。
- 机器人轴值保存在 `List<double> RobotAxisValues` 中，内容为 RoboGuide 原始角度值。
- 写入 Tecnomatix 姿态时会将角度转换为弧度。
- `DI` 和 `DO` 当前由 RJi API 作为布尔信号读取。
- RoboGuide DI 是否允许写入取决于 RoboGuide/SNPX 配置。
- 不建议在监听过程中重新选择机器人。
- 不要在多个线程中同时操作同一个 `FanucRobotClient` 实例。

### 常见问题

#### 1. 找不到监听地址

确认 RoboGuide 已启动 SNPX 服务，并监听 TCP `60008`。可以在 PowerShell 中执行：

```powershell
netstat -ano | findstr 60008
```

期望看到类似结果：

```text
TCP    127.0.0.2:60008        0.0.0.0:0              LISTENING       22248
```

#### 2. 无法连接 RoboGuide

检查：

- RoboGuide 是否启用 SNPX；
- IP 地址是否正确；
- 端口是否为 `60008`；
- Windows 防火墙是否阻止连接；
- RoboGuide 是否允许目标 DI/DO 操作。

#### 3. 找不到 Tecnomatix PLC 程序

初始化信号需要当前 Tecnomatix 文档中已经存在 PLC 程序。如果不存在，信号创建会失败。

#### 4. 克隆后项目无法生成

重点检查：

- `Tecnomatix.Engineering.dll` 路径；
- `Tecnomatix.Engineering.Ui.dll` 路径；
- `RJi.FanucRobot.Interface.dll` 路径；
- .NET Framework 4.8 Developer Pack 是否安装；
- Tecnomatix 版本是否兼容。

### GitHub 发布建议

提交前建议创建 `.gitignore`，至少忽略：

```gitignore
.vs/
bin/
obj/
TestResults/
*.user
*.suo
*.pdb
packages/
```

提交前请确认没有上传：

- 个人访问令牌；
- 密码；
- 公司内部服务器地址；
- 未授权分发的商业 DLL；
- 本机专用绝对路径之外的敏感信息。

### 测试状态

当前项目没有独立的自动化测试项目。已验证项目可以通过 Visual Studio 编译，但 RoboGuide 与 Tecnomatix 的实时通信、信号权限和 100 ms 调度需要在实际运行环境中验证。

### 贡献代码

欢迎提交 Issue 或 Pull Request。建议提交前：

1. 确认代码可以编译；
2. 不提交 `bin/`、`obj/` 和 `.vs/`；
3. 说明 Tecnomatix 和 RoboGuide 版本；
4. 说明复现步骤和错误信息；
5. 不提交商业软件程序集或敏感配置。

### 许可证

本项目当前未声明开源许可证。除非仓库中添加明确的 `LICENSE` 文件，否则请不要默认复制、修改或商业分发本项目代码。

Tecnomatix、RoboGuide、FANUC 和相关程序集的商标及知识产权归其各自所有者所有。

---

## English

### Overview

`VRC_FANUC` is a Tecnomatix extension project based on **.NET Framework 4.8**, **Tecnomatix Engineering**, and **RoboGuide SNPX** communication.

The project reads FANUC robot joint values, DI signals, and DO signals from RoboGuide, synchronizes them with Tecnomatix, and writes Tecnomatix PLC-to-Robot changes back to RoboGuide DI signals.

### Features

- Select a target robot in Tecnomatix.
- Detect the local TCP listener on port `60008`.
- Store the detected endpoint in:
  - `VRC_ip`
  - `VOR_port`
- Connect to the RoboGuide SNPX service through `RJi.FanucRobot.Interface`.
- Read the current robot joint values.
- Read RoboGuide DI/DO signal information:
  - Address
  - Name
  - Comment
  - Current value
- Apply RoboGuide joint values to the Tecnomatix robot pose.
- Convert RoboGuide joint angles from degrees to radians before updating Tecnomatix.
- Create or reuse Tecnomatix PLC signals without creating duplicates.
- Map RoboGuide DO signals to Tecnomatix Robot-to-PLC / FromRobot signals.
- Map RoboGuide DI signals to Tecnomatix PLC-to-Robot / ToRobot signals.
- Initialize Tecnomatix signal values from the current RoboGuide values.
- Start and stop monitoring through the Connect / Disconnect button.
- During monitoring, synchronize signals approximately every `100 ms`:
  1. Detect Tecnomatix PLC-to-Robot changes.
  2. Write changed values to RoboGuide DI.
  3. Read RoboGuide DI/DO values.
  4. Update changed RoboGuide DO values in Tecnomatix Robot-to-PLC signals.
  5. Update robot joint values approximately every `100 ms`.
- Update only changed signal values.
- Run monitoring in a background task so the UI thread is not directly blocked.
- Cancel monitoring and disconnect the SNPX client when Disconnect is clicked or the form is closed.

### Signal Mapping

| RoboGuide signal | Tecnomatix signal | Direction | Description |
|---|---|---|---|
| `DO[index]` | Robot-to-PLC / FromRobot | RoboGuide → Tecnomatix | Changed RoboGuide DO values are copied to Tecnomatix |
| `DI[index]` | PLC-to-Robot / ToRobot | Tecnomatix → RoboGuide | Changed Tecnomatix values are written to RoboGuide DI |

> The implementation follows the actual robot signal direction: RoboGuide DO is a robot output, while RoboGuide DI is a robot input.

### Monitoring Sequence

```text
Read Tecnomatix PLC-to-Robot signals
		↓
Detect changed values
		↓
Write changed values to RoboGuide DI
		↓
Wait for the write operation to complete
		↓
Read RoboGuide DI / DO values
		↓
Detect changed RoboGuide DO values
		↓
Update changed Tecnomatix Robot-to-PLC signals
		↓
Update the robot pose when a new joint snapshot is available
```

The target synchronization interval is approximately `100 ms`. The actual interval depends on network communication, the number of signals, Tecnomatix API execution time, and system scheduling. This is not a hard real-time loop.

### Technology Stack

- Visual Studio 2026
- C#
- .NET Framework 4.8
- Windows Forms
- Tecnomatix Engineering API
- Tecnomatix Engineering UI API
- RoboGuide SNPX
- `RJi.FanucRobot.Interface` 1.2.2

### Repository Structure

```text
VRC_FANUC/
├── VRC_FANUC.slnx
├── VRC_FANUC/
│   ├── VRC_FANUC.csproj
│   ├── TxVRCRoboGuideComm.cs
│   ├── TxVRCRoboGuideComForm.cs
│   ├── TxVRCRoboGuideComForm.Designer.cs
│   ├── TxVRCRoboGuideComForm.resx
│   ├── StringTable.resx
│   ├── StringTable.Designer.cs
│   ├── Properties/
│   └── vrc_png.png
└── README.md
```

### Requirements

1. Windows.
2. Visual Studio 2026 or a compatible version.
3. .NET Framework 4.8 Developer Pack.
4. Tecnomatix Engineering. The current project uses this default installation path:

```text
C:\Program Files\Tecnomatix_2402\eMPower\
```

5. RoboGuide with the SNPX service enabled.
6. RoboGuide listening on TCP port `60008`.
7. The `netstandard2.0` version of `RJi.FanucRobot.Interface.dll`.

### Assembly Dependencies

The project references:

- `Tecnomatix.Engineering.dll`
- `Tecnomatix.Engineering.Ui.dll`
- `RJi.FanucRobot.Interface.dll`

The project currently uses machine-specific absolute paths. After cloning the repository on another computer, update `VRC_FANUC.csproj` to match the local installation paths.

Do not upload proprietary Tecnomatix assemblies to a public GitHub repository unless redistribution is explicitly permitted. Use a local installation reference, an approved internal package feed, or a properly licensed private dependency repository.

### Build

1. Install the required software and assemblies.
2. Open `VRC_FANUC.slnx` in Visual Studio.
3. Check the assembly paths in `VRC_FANUC.csproj`.
4. Select __Build > Build Solution__.
5. Build output is normally located under:

```text
VRC_FANUC/bin/Debug/
VRC_FANUC/bin/Release/
```

### Usage in Tecnomatix

1. Build the extension assembly.
2. Deploy `VRC_FANUC.dll` using the Tecnomatix extension deployment process.
3. Execute the RoboGuide communication command from Tecnomatix.
4. Select the target robot.
5. Wait for the joint values, DI/DO values, and signal table initialization to complete.
6. The `Connect` button becomes enabled after successful initialization.
7. Click `Connect` to start monitoring.
8. The button changes to `Disconnect` while monitoring is active.
9. Click `Disconnect` to stop monitoring and close the RoboGuide connection.

### Important Notes

- The initialization currently reads up to `4096` DI points and `4096` DO points.
- Reading a large number of points may take time.
- The Tecnomatix PLC signal table is checked before creating signals.
- Existing signals are reused instead of being duplicated.
- Existing signals are updated by value during monitoring; they are not recreated.
- `RobotAxisValues` is a `List<double>` containing the original RoboGuide joint angles.
- Joint angles are converted from degrees to radians before being assigned to Tecnomatix.
- DI and DO are currently read as Boolean values through the RJi API.
- Whether RoboGuide DI points can be written depends on the RoboGuide/SNPX configuration.
- Avoid selecting another robot while monitoring is active.
- Do not concurrently access the same `FanucRobotClient` instance from multiple threads.

### Troubleshooting

#### Listener not found

Make sure RoboGuide has started its SNPX service and is listening on TCP port `60008`:

```powershell
netstat -ano | findstr 60008
```

Example:

```text
TCP    127.0.0.2:60008        0.0.0.0:0              LISTENING       22248
```

#### Cannot connect to RoboGuide

Check the following:

- SNPX is enabled in RoboGuide;
- the IP address is correct;
- port `60008` is available;
- Windows Firewall allows the connection;
- RoboGuide permits the required DI/DO operations.

#### No PLC program in Tecnomatix

A PLC program must already exist in the active Tecnomatix document before robot signals can be created.

#### Build fails after cloning

Check:

- `Tecnomatix.Engineering.dll` path;
- `Tecnomatix.Engineering.Ui.dll` path;
- `RJi.FanucRobot.Interface.dll` path;
- .NET Framework 4.8 Developer Pack installation;
- compatibility between the installed Tecnomatix version and the project.

### GitHub Publishing Recommendations

Add a `.gitignore` before the first commit:

```gitignore
.vs/
bin/
obj/
TestResults/
*.user
*.suo
*.pdb
packages/
```

Do not commit:

- personal access tokens;
- passwords;
- internal server addresses;
- proprietary assemblies without redistribution permission;
- sensitive local configuration data.

### Testing Status

The repository currently does not contain a dedicated automated test project. The project has been verified to compile in Visual Studio. Real-time communication, DI write permissions, Tecnomatix signal behavior, and the approximately 100 ms synchronization interval must be verified in an actual RoboGuide and Tecnomatix environment.

### Contributing

Issues and pull requests are welcome. Before submitting a change:

1. Confirm that the project builds successfully.
2. Do not commit `bin/`, `obj/`, or `.vs/` files.
3. Include the Tecnomatix and RoboGuide versions.
4. Include reproduction steps and relevant error messages.
5. Do not commit proprietary assemblies or sensitive configuration data.

### License

This project currently does not declare an open-source license. Unless a `LICENSE` file is added to the repository, do not assume that the code may be copied, modified, or redistributed.

Tecnomatix, RoboGuide, FANUC, and the related assemblies are trademarks and intellectual property of their respective owners.
