欢迎使用 `JC-Tools` ，这是一个方便简易的 JC 小工具。

`JC-Tools` 由C#开发，采用 `.net 5.0`，因此该项目可以在`Windows`、`Linux`和`MacOS`中运行，但由于该项目采用部分的API和库仅支持 `Windows` 系统，如果您要在其他系统中编译，请使用等效的API或库代替。

`JC-Tools` 欢迎各种形式的贡献。

# 如何安装
`JC-Tools` 提供了多种安装方法：
1. 下载右侧的发行版并解压
2. 使用 `git clone git@gitee.com:graph-lc/jc-tools.git` 将本仓库克隆下来，并通过 `dotnet publish` 命令编译，这也是您在其他系统中使用 `JC-Tools` 的唯一途径。

# 如何使用

### 配置被JC端
将 `JCServer` 文件夹里面的文件解压到要JC的电脑上，运行 `JCServer.exe`，编辑防火墙的入站规则，将5800端口打开（或者直接关闭防火墙）。

### 连接到被JC端
在自己的设备上运行 `JCClient`，通过输入 `list add [ip]` 来连接到要JC的设备（您也可以输入 `list add-host [name]` 来连接）。
 
_其中，[ip]为要JC的设备的Ip Address，[name]为要JC的设备的主机名_

### 控制被JC端
您可以通过以下命令来控制被JC端：

| 命令           | 解释                     | 示例                    |
|--------------------------|---------------------------------|-----------------------------------------|
| list | 查看已经连接的被JC端 | list |
| list add [ip] | 连接到 _ip_ | list add 127.0.0.1 |
| list add-host [name] | 解析 _name（主机名称）_ 并连接 | list add XUE001 |
| list remove [name] | 从连接列表中删除 _name_ | `list remove XUE001` 或  `list remove 127.0.0.1` |
| list clear | 清空连接列表 | list clear |
| send [command] | 向所有连接发送 _command_ | send mouse move 1 1 |
| send-server [name] [command] | 向连接列表中的 _name_ 发送 _command_ | send mouse move 1 1 |
| clear | 清空屏幕 | clear |
| quit | 退出控制（服务端不退出） | quit |

其中，`send [command]` 中的 _command_ 可以为：
| 命令 | 解释 | 示例 |
|------|-----|-----|
| mouse move x y           | 移动鼠标到(x,y)                      | mouse move 640 480                      |
| key down value           | 模拟键盘按下value                     | key down 67                             |
| key up value             | 模拟键盘抬起                          | key up 67                               |
| key push value           | 模拟键盘按下一连串字符串                    | key push "HELLO WORLD"                    |
| shell path [command]               | 执行path                          | shell notepad D:\x.txt                               |
| upload path1 path2 | 将path1（本地）传送到path2（被控制端） | download D:\test.exe C:\test.exe |
| download path1 path2             | 将path1（被控制端）传送到path2（本地）              | download C:\test.exe D:\test.exe                     |
| media create name path   | 创建一个名为name、路径为path的Sound（仅支持`.wav`）     | media create m D:\test.wav |
| media load name          | 加载名为name的Sound   | media load m |
| media play name          | 播放名为name的Sound（如未加载则先加载该Sound） | media play m |
| media stop name          | 停止播放名为name的Sound | media stop m |
| media delete name        | 删除名为name的Sound | media delete m |