# NetTask
NetTask是一款基于.net core2.2开发的的通用任务管理系统，将任务逻辑和任务调度彻底分离，并可通过Web界面远程监控和管理任务。

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

下载[nettask_netcore_v101.zip](https://github.com/Mcdull0921/NetTask/releases/download/v1.0.1/nettask_netcore_v101.zip)，解压后将文件拷贝到服务器上，执行命令：

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

任务需自行在本地编写，新建一个.net core类库项目，让项目引用**NetTaskInterface**，一个dll可以包含多个任务，任意类只要继承`NetTaskInterface.ITask`即被识别为一个任务。

将任务程序编译后，打包成zip文件，超级管理员可在程序集模块，点击上传程序集，选择该zip文件将任务添加进系统。
更多介绍，可在启动NetTask后进入系统在**帮助**页面查看。

任务只编写运行一次的逻辑，将任务添加进系统后，可在系统中设置任务的运行频率。

任务也可有配置文件，如果后续配置文件有更改，也无需重新上传任务，可直接在系统中修改。

一个简单的任务代码示例：

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

![demo](https://github.com/Mcdull0921/NetTask/blob/master/demo.gif)

#### 任务的依赖项

如在输出目录没有找到依赖项，可编辑项目csproj文件，添加如下代码，可将所有依赖项生成到输出目录：

```xml
<PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```
