# NetTask
NetTask是一款基于.net core3.0开发的的通用任务管理系统，将任务逻辑和任务调度彻底分离，并可通过Web界面远程监控和管理任务。

.Net Core 3.0可回收程序集加载上下文AssemblyLoadContext新增了Unload方法，真正实现了程序集的热插拔。

## 用这个有什么好处
- [x] 1. 不需再关注任务调度，只需编写任务执行的逻辑代码，由任务管理器统一调度；
- [x] 2. 在任务中打印日志，可通过Web界面中实时查看任务执行情况；
- [x] 3. 无需再登录服务器，可直接将任务上传至服务器，随时启动和关闭任务；
- [x] 4. 多种任务调度方案，一般任务、定时任务，循环任务、定时循环任务；
- [x] 5. 灵活的任务配置，可随时远控修改配置，如数据库连接字符串更改;
- [x] 6. 拥有多种角色控制，满足各类人员需求，避免无关人员误操作。

## 项目说明

1. **NetTaskManager**是整个任务调度的核心；
2. **NetTaskInterface**提供任务接口，编写的所有任务都必须实现该接口，才可被任务管理器识别，是**NetTaskManager**和**Task**的桥梁；
3. **NetTaskServer**是NetTask的主程序，提供Web服务，通过HTTP协议操控**NetTaskManager**。

## 安装部署

### 直接运行

下载[nettask_netcore_v120.zip](https://github.com/Mcdull0921/NetTask/releases/download/v1.2.0/nettask_netcore_v120.zip)，解压后将文件拷贝到服务器上，执行命令：

```bash
dotnet NetTaskServer.dll
```

不加参数，采用**12315**的默认端口号，如需指定端口，比如8888，执行命令：

```bash
dotnet NetTaskServer.dll 8888
```

### 注册为Windows服务

只需在运行命令后面跟上`action:install`，即可注册成为Windows服务

如需卸载，和安装服务一样，把命令改成`action:uninstall`

完整命令：

```bash
dotnet NetTaskServer.dll 8888 action:install     #安装
dotnet NetTaskServer.dll action:uninstall        #卸载
```

## 使用说明

启动程序后，在浏览器输入服务器IP以及设定的或者默认端口号访问系统，比如：http://127.0.0.1:12315

进入系统需要登录，系统首次启动默认会生成一个账号名和密码都为admin的超级管理员账号，进入系统后可在用户管理中重置密码，或者创建新账号。

### 角色

系统分为3种角色：

1. 普通用户：仅可查看任务运行状态和日志；
2. 管理员：可修改任务配置，任务执行参数，以及启停所有任务；
3. 超级管理员：拥有最高权限，可管理用户、重置登录密码、上传程序集等。

### 任务

#### 编写任务

新建一个.net core类库项目，让项目引用**NetTaskInterface.dll**，一个dll可以包含多个任务，任意类只要继承`NetTaskInterface.ITask`即被识别为一个任务。
该类需实现2个抽象方法`name`和`process`，`name`是任务用于显示的友好名称，`process`是任务运行一次的逻辑。
实现该抽象类，默认会获得`logger`和`configuration`2个对象： `logger`用于日志输出，`configuration`用于读取配置文件，一个简单的任务代码示例：

```C#
public class Class1 : ITask
{
    public override string name => "Test1";

    public override void process()
    {
        logger.Info("Info Test");
        Console.WriteLine(configuration["a"]);
        Console.WriteLine(configuration.GetIntValue("b"));
    }
}
```

任务只编写运行一次的逻辑，将任务添加进系统后，可在系统中设置任务的运行频率。
任务也可有配置文件，如果后续配置文件有更改，也无需重新上传任务，可直接在系统中修改。

#### 日志

使用`logger`的输出会展现在管理界面中，日志包含`Info`和`Error` 2个方法，`Info`用来记录普通的信息，`Error`接收`Exception`类型用以记录异常信息。
任何未捕获或未处理的异常将导致该任务停止运行，无论该任务是否循环。

#### 配置文件

配置文件必须以**main.xml**命名并且和dll放置在同一目录，无论是否需要读取配置文件，都需在本地创建**main.xml**，在打包程序集时需要包含此文件，配置文件示例：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<task entrypoint="TestTask1.dll">
    <add key="a" value="你好" />
    <add key="b" value="1" />
</task>
```

其中**entrypoint**指定了任务所在的dll，任务管理器将会在此dll中查找任务。可以不限制添加任意数量的键值对，通过`configuration`对象来获取值。

#### 打包

将类库编译生成好后，连同所有的依赖项dll(不需包括**NetTaskInterface.dll**)和配置文件**main.xml**一同打包成zip文件。在程序集模块中点击上传程序集将任务添加进系统。
zip包中必须包含所有的依赖dll文件，可编辑项目csproj文件，添加如下代码，将所有依赖项生成到输出目录。

```xml
<PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

#### 演示

![demo](https://github.com/Mcdull0921/NetTask/blob/master/demo.gif)

### 任务运行参数说明

#### 非循环任务

非循环任务只执行一次，如果勾选了启动时立即执行，则任务开始时立即执行，或者设置了开始时间，则任务开始时会处于等待状态，直到到达开始时间才会执行。

#### 循环任务
可设置五种循环：秒、分钟、小时、天、月，间隔值依据循环类型确定时间跨度，比如设置为5，循环类型选择的秒，则代表5秒一循环。
勾选启动时立即执行，则任务开始时立即开始循环。

#### 定时循环任务

给循环任务设置开始时间，则任务以开始时间作为循环的起始点，任务启动时如果当前时间超过设定时间，则会根据间隔自动累加到下一次执行的时间点，否则就一直等待到设定时间。
比如要让任务在每天早上7时执行，可以设置成天循环，间隔设置为1，开始时间的时间部分设置为7时，日期部分设置为任意小于当天的日期，启动任务时，如果当天没到7点则等到7点执行，过了7点则第二天7点才会执行。

### 任务配置

任务在停止状态下，可通过点击修改任务配置来调整main.xml中已设定好的值。任务再次运行时，读取的将是设置后的新值。

如果对你有帮助给我点一个Star可好，同时也欢迎大神朋友提交PR，你的鼓励就是我的最大动力。
