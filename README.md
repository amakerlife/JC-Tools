欢迎使用JC-Tools，这是一个方便简易的 JC 小工具。

# 如何使用
下载右侧的发行版，解压，将 `JCServer` 里面的文件解压到要JC的电脑上，双击 `Host.exe` ，将输出的 `Address` 的最后一行记下来，然后关闭防火墙或者编辑 `入站规则` ，添加5800端口。打开JCServer.exe，然后回到自己的电脑，用 `Powershell` 或 `cmd` 打开 `JCCilent.exe`，输入 `ip`，然后使用命令进行控制。

| 命令           | 解释                     | 示例                    |
|--------------------------|---------------------------------|-----------------------------------------|
| mouse move x y           | 移动鼠标到(x,y)                      | mouse move 640 480                      |
| key down value           | 模拟键盘按下value                     | key down 67                             |
| key up value             | 模拟键盘抬起                          | key up 67                               |
| key push value           | 模拟键盘按下一连串字符串                    | key up "HELLO WORLD"                    |
| shell path               | 执行path                          | shell cmd                               |
| download len path1 path2 | 将path1（本地）传送到path2（被控制端），长度为len | download 279528 D:\test.exe C:\test.exe |
| filelen path             | 查看path（本地）的大小（通常用于              | filelen D:\test.exe                     |
| media create name path   | 创建一个名为name、路径为path的Sound（仅支持`.wav`）     | media create m D:\test.wav |
| media load name          | 加载名为name的Sound   | media load m |
| media play name          | 播放名为name的Sound（如未加载则先加载该Sound） | media play m |
| media stop name          | 停止播放名为name的Sound | media stop m |
| media delete name        | 删除名为name的Sound | media delete m |
| quit                     | 退出控制（服务端不退出） | quit |