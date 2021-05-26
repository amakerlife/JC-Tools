欢迎使用 `JC-Tools` ，这是一个方便简易的 JC 小工具。

`JC-Tools` 由 `C#` 开发，基于 `.net 5.0` 框架，因此该项目可以在`Windows`、`Linux`和`MacOS`中运行，但由于该项目采用部分的API和库仅支持 `Windows` 系统，如果您要在其他系统中编译，请使用等效的API或库代替。

`JC-Tools` 欢迎各种形式的贡献。

# 如何安装
`JC-Tools` 提供了多种安装方法：
1. 下载右侧的发行版并解压
2. 使用 `git clone git@gitee.com:graph-lc/jc-tools.git` 将本仓库克隆下来，并通过 `dotnet publish` 命令编译，这也是您在其他系统中使用 `JC-Tools` 的唯一途径。

# 如何使用

_注: <>中的表示可选参数_

### 配置被JC端
1. 将 `JCServer` 或 `JCServerLight` 文件夹里面的文件解压到要JC的电脑上。
2. 双击运行 `JCServer.exe` 或 `JCServerLight.exe` 或在 `PowerShell` 中输入 `JCServer <port>`  或 `JCServerLight <port>` 来指定 `JCServer` 监听的端口
3. 编辑防火墙的入站规则，将 `5800` 或 `[port]` 端口打开（也可以直接关闭防火墙）。

#### _JCServer与JCServerLight的区别_

* JCServer 不需要额外安装任何框架（已经嵌在dll里面了），但由于低版本Windows（包括部分`Windows7`）不支持 `.net 5` ，需要额外安装补丁和 `Visual C++ Redistributable 2015` （见FAQs）。

* JCServerLight 基于 `.net framework 3.5` (`Windows7` 自带)，不需要额外的dll，体积小巧。

两者在使用方面没有差异，您可以根据您的系统选择合适的版本。

### 连接到被JC端
在自己的设备上运行 `JCClient`，通过输入 `list add [ip] <port>`（如果您没有指定监听端口，则不需要填写端口） 来连接到要JC的设备（您也可以输入 `list add-host [name] <port>` 来连接）。
 
_其中，[ip] 为要JC的设备的 Ip Address，[name] 为要 JC 的设备的主机名_

### 控制被JC端
您可以通过以下命令来控制被JC端，`JC-Tools` 支持简写命令。

| 命令           | 解释                     | 示例                    |
|--------------------------|---------------------------------|-----------------------------------------|
| list 或 l | 查看已经连接的被JC端 | list |
| list add [ip] 或 l a [ip] | 连接到 _ip_ | list add 127.0.0.1 |
| list add-host [name] 或 l ah [name] | 解析 _name（主机名称）_ 并连接 | list add-host XUE001 |
| list remove [name] 或 l r| 从连接列表中删除 _name_ | `list remove XUE001` 或  `list remove 127.0.0.1` |
| list clear 或 l c | 清空连接列表 | list clear |
| send [command] 或 s [command]| 向所有连接发送 _command_ | send mouse move 1 1 |
| send-server [name] [command] 或 sh [name] [command]| 向连接列表中的 _name_ 发送 _command_ | send-server XUE001 mouse move 1 1 |
| clear 或 c | 清空屏幕 | clear |
| quit 或 q | 退出控制（服务端不退出） | quit |

其中，`send [command]` 中的 _command_ 可以为：
| 命令 | 解释 | 示例 |
|------|-----|-----|
| mouse move x y 或 m m x y| 移动鼠标到(x,y)                      | mouse move 640 480                      |
| mouse click left/middle/right 或 m c l/m/r | 单击鼠标左/中/右键 | mouse click left                        |
| mouse double-click left/middle/right 或 m dc l/m/r | 双击鼠标左/中/右键 | mouse-double click left                        |
| key down value 或 k d value           | 模拟键盘按下value（value的表见keybd_event键码，但也可以为ctrl/shift/alt/esc/back/delete/tab等）                     | key down 17 或 key down ctrl                            |
| key up value 或 k u value            | 模拟键盘抬起                          | key up 17 或 key up ctrl                               |
| key push value 或 k p value           | 模拟键盘按下一连串字符串                    | key push "You AK IOI"                    |
| shell path <command> 或 s path <command>               | 执行path                          | shell notepad D:\x.txt                               |
| shell-hide path <command> 或 sh path <command>          | 隐式执行path（不显示窗口）        | shell-hide taskkill -f -im XXX.exe |
| upload path1 path2 或u path1 path2 | 将path1（本地）传送到path2（被控制端） | download D:\test.exe C:\test.exe |
| download path1 path2 或 d path1 path2            | 将path1（被控制端）传送到path2（本地）              | download C:\test.exe D:\test.exe                     |
| background path 或 b path | 设置桌面背景图片 | background D:\back.png |
| screen mode path 或 sc mode path | 以指定模式截取屏幕并传回path(控制端) ，其中，mode可以为`jpg`或`png` | screen png D:\screen.png |
| protect path 或 p path | 将path设置为保护进程（path必须在被控制端） | protect D:\wininit.exe |
| dir [dir] | 查看被控制端的dir目录下的所有文件和文件夹 | dir D:\ |
| media create name path   | 创建一个名为name、路径为path的Sound（仅支持`.wav`）     | media create m D:\test.wav |
| media load name          | 加载名为name的Sound   | media load m |
| media play name          | 播放名为name的Sound（如未加载则先加载该Sound） | media play m |
| media stop name          | 停止播放名为name的Sound | media stop m |
| media delete name        | 删除名为name的Sound | media delete m |

# FAQs

Q1: 运行 `JC-Tools` 提示 `failed to load the dll from [...] HRESULT 0x00000001` 并崩溃？

A1: 安装 _KB2533623 补丁_ 即可。

Q2: 运行 `JC-Tools` 提示 `丢失xxx.dll` ？

A2: 安装 _Visual C++ Redistributable 2015_ 即可。