﻿using NetTaskManager;
using NetTaskServer.Common;
using NetTaskServer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NetTaskServer.HttpServer
{
    partial class HttpServer
    {
        #region HTTPServer


        private const string INDEX_PAGE = "/main.html";
        private const string BASE_FILE_PATH = "./Web/";
        private const string FILE_UPLOAD_PATH = "./temp";

        public Dictionary<string, MemoryStream> FilesCache = new Dictionary<string, MemoryStream>(20);
        private Logger Logger { get; set; }
        private HttpServerAPIs ControllerInstance { get; set; }

        public HttpServer(Logger logger, HttpServerAPIs httpServerAPIs)
        {
            //第一次加载所有mime类型
            PopulateMappings();
            this.Logger = logger;
            this.ControllerInstance = httpServerAPIs;
        }



        /// <summary>
        /// http服务启动，初始化代码写在这里
        /// </summary>
        /// <param name="ctsHttp"></param>
        /// <param name="WebManagementPort"></param>
        /// <returns></returns>
        public async Task StartHttpService(CancellationTokenSource ctsHttp, int WebManagementPort)
        {
            try
            {
                HttpListener listener = new HttpListener();
                //缓存所有文件
                var dir = new DirectoryInfo(BASE_FILE_PATH);
                var files = dir.GetFiles("*.*");
                foreach (var file in files)
                {
                    using (var fs = file.OpenRead())
                    {
                        var mms = new MemoryStream();
                        fs.CopyTo(mms);
                        FilesCache.Add(file.Name, mms);
                    }
                }
                Logger.Debug($"{files.Length} files cached.");

                listener.Prefixes.Add($"http://+:{WebManagementPort}/");
                Logger.Debug("Listening HTTP request on port " + WebManagementPort.ToString() + "...");
                await AcceptHttpRequest(listener, ctsHttp);
            }
            catch (HttpListenerException ex)
            {
                Logger.Debug("Please run this program in administrator mode." + ex);
                Logger.Error(ex.ToString(), ex);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex.Message);
                Logger.Error(ex.ToString(), ex);
            }
            Logger.Debug("Http服务结束。");
        }

        private async Task AcceptHttpRequest(HttpListener httpService, CancellationTokenSource ctsHttp)
        {
            httpService.Start();
            while (true)
            {
                var client = await httpService.GetContextAsync();
                _ = ProcessHttpRequestAsync(client);
            }
        }

        /// <summary>
        /// HttpListener接收post请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private Dictionary<string, string> PostInput(HttpListenerRequest request)
        {
            try
            {
                Stream s = request.InputStream;
                int count = 0;
                byte[] buffer = new byte[1024];
                StringBuilder builder = new StringBuilder();
                while ((count = s.Read(buffer, 0, 1024)) > 0)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, count));
                }
                s.Flush();
                s.Close();
                s.Dispose();
                Dictionary<string, string> res = new Dictionary<string, string>();
                var data = builder.ToString();
                foreach (var d in data.Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = d.Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    res.Add(kv[0], HttpUtility.UrlDecode(kv[1]));
                }
                return res;
            }
            catch (Exception ex)
            { throw ex; }
        }

        private async Task ProcessHttpRequestAsync(HttpListenerContext context)
        {

            var request = context.Request;
            var response = context.Response;
            //TODO XX 设置该同源策略为了方便调试，真实项目请确保同源

            //禁止跨站访问
            //response.AddHeader("Access-Control-Allow-Origin", "*");

            response.Headers["Server"] = "";
            try
            {
                //通过request来的值进行接口调用
                string unit = request.RawUrl.Replace("//", "");

                if (unit == "/") unit = INDEX_PAGE;

                int idx1 = unit.LastIndexOf("#");
                if (idx1 > 0) unit = unit.Substring(0, idx1);
                int idx2 = unit.LastIndexOf("?");
                if (idx2 > 0) unit = unit.Substring(0, idx2);
                int idx3 = unit.LastIndexOf(".");

                //通过后缀获取不同的文件，若无后缀，则调用接口
                if (idx3 > 0)
                {

                    if (!File.Exists(BASE_FILE_PATH + unit))
                    {
                        Logger.Debug($"未找到文件{BASE_FILE_PATH + unit}");
                        response.StatusCode = 404;
                        return;

                    }
                    //mime类型
                    ProcessMIME(response, unit.Substring(idx3));
                    //TODO 权限控制（只是控制html权限而已）

                    //读文件优先去缓存读
                    if (FilesCache.TryGetValue(unit.TrimStart('/'), out MemoryStream memoryStream))
                    {
                        memoryStream.Position = 0;
                        await memoryStream.CopyToAsync(response.OutputStream);
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(BASE_FILE_PATH + unit, FileMode.Open))
                        {
                            await fs.CopyToAsync(response.OutputStream);
                        }
                    }
                }
                else  //url中没有小数点则是接口
                {
                    unit = unit.Replace("/", "");
                    response.ContentEncoding = Encoding.UTF8;

                    //调用接口 用分布类隔离并且用API特性限定安全
                    object jsonObj;
                    //List<string> qsStrList;
                    int qsCount = request.QueryString.Count;
                    List<object> parameters = new List<object>();
                    if (qsCount > 0)
                    {
                        for (int i = 0; i < request.QueryString.Count; i++)
                        {
                            parameters.Add(request.QueryString[i]);
                        }
                    }
                    var postDatas = PostInput(request); // new HttpListenerPostParaHelper(context).GetHttpListenerPostValue();
                    foreach (var kv in postDatas)
                    {
                        parameters.Add(kv.Value);
                    }

                    //反射调用API方法
                    MethodInfo method = null;
                    try
                    {
                        method = ControllerInstance.GetType().GetMethod(unit);
                        if (method == null)
                        {
                            //Server.Logger.Debug($"无效的方法名{unit}");
                            throw new Exception($"无效的方法名{unit}");
                        }
                        LoginInfo loginInfo = null;
                        if (method.GetCustomAttribute<SecureAttribute>() != null)
                        {
                            if (request.Cookies["NSPTK"] == null)
                                throw new Exception("用户未登录。");
                            //TODO cookie，根据不同的用户角色分配权限。
                            var UserClaims = StringUtil.ConvertStringToTokenClaims(request.Cookies["NSPTK"].Value);
                            if (string.IsNullOrEmpty(UserClaims.UserKey))
                            {
                                throw new Exception("登录信息异常。");
                            }
                            var secure = method.GetCustomAttribute<SecureAttribute>();
                            if (UserClaims.Role < secure.min_role)
                            {
                                throw new Exception("没有权限。");
                            }
                            loginInfo = new LoginInfo
                            {
                                username = UserClaims.UserKey,
                                role = UserClaims.Role,
                                ip = request.RemoteEndPoint.Address
                            };
                        }
                        if (method.GetCustomAttribute<LoginInfoAttribute>() != null)
                        {
                            parameters.Add(loginInfo);
                        }
                        if (method.GetCustomAttribute<APIAttribute>() != null)
                        {
                            //返回json，类似WebAPI
                            response.ContentType = "application/json";
                            jsonObj = method.Invoke(ControllerInstance, parameters.ToArray());
                            await response.OutputStream.WriteAsync(HtmlUtil.GetContent(jsonObj.Wrap().ToJsonString()));
                        }
                        else if (method.GetCustomAttribute<FormAPIAttribute>() != null)
                        {
                            //返回表单页面
                            response.ContentType = "text/html;charset=utf-8";
                            if (unit.ToUpper().Equals("LOGIN"))
                            {
                                parameters.Add(request.RemoteEndPoint.Address.ToString());
                            }
                            jsonObj = method.Invoke(ControllerInstance, parameters.ToArray());
                            await response.OutputStream.WriteAsync(HtmlUtil.GetContent(jsonObj.ToString()));
                        }
                        else if (method.GetCustomAttribute<ValidateAPIAttribute>() != null)
                        {
                            //验证模式
                            response.ContentType = "application/json";
                            bool validateResult = (bool)method.Invoke(ControllerInstance, parameters.ToArray());
                            if (validateResult == true)
                            {
                                await response.OutputStream.WriteAsync(HtmlUtil.GetContent("{\"valid\":true}"));
                            }
                            else
                            {
                                await response.OutputStream.WriteAsync(HtmlUtil.GetContent("{\"valid\":false}"));
                            }
                        }
                        else if (method.GetCustomAttribute<FileAPIAttribute>() != null)
                        {
                            //文件下载
                            response.ContentType = "application/octet-stream";
                            if (!(method.Invoke(ControllerInstance, parameters.ToArray()) is FileDTO fileDto))
                            {
                                throw new Exception("文件返回失败，请查看错误日志。");
                            }

                            response.Headers.Add("Content-Disposition", "attachment;filename=" + fileDto.FileName);
                            //response.OutputStream.(stream);
                            using (fileDto.FileStream)
                            {
                                await fileDto.FileStream.CopyToAsync(response.OutputStream);
                            }
                        }
                        else if (method.GetCustomAttribute<FileUploadAttribute>() != null)
                        {
                            //文件上传
                            response.ContentType = "application/json";
                            if (request.HttpMethod.ToUpper() == "POST")
                            {
                                var file = SaveFile(request.ContentEncoding, request.ContentType, request.InputStream);
                                parameters.Insert(0, file);
                                jsonObj = method.Invoke(ControllerInstance, parameters.ToArray());
                                await response.OutputStream.WriteAsync(HtmlUtil.GetContent(jsonObj.Wrap().ToJsonString()));
                            }
                            else
                            {
                                await response.OutputStream.WriteAsync(HtmlUtil.GetContent(HttpResult<object>.NullSuccessResult.ToJsonString()));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if ((ex is TargetInvocationException) && ex.InnerException != null) ex = ex.InnerException;
                        Logger.Error(ex.Message, ex);
                        jsonObj = new Exception(ex.Message);// + "---" + ex.StackTrace
                        response.ContentType = "application/json";
                        await response.OutputStream.WriteAsync(HtmlUtil.GetContent(jsonObj.Wrap().ToJsonString()));
                    }
                    finally
                    {

                    }
                }

            }
            catch (Exception e)
            {
                Logger.Error(e.Message, e);
                throw;
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        private void ProcessMIME(HttpListenerResponse response, string suffix)
        {
            if (suffix == ".html" || suffix == ".js")
            {
                response.ContentEncoding = Encoding.UTF8;
            }

            if (_mimeMappings.TryGetValue(suffix, out string val))
            {
                // found!
                response.ContentType = val;
            }
            else
            {
                response.ContentType = "application/octet-stream";
            }

        }



        #region 文件读取
        private String GetBoundary(String ctype)
        {
            return "--" + ctype.Split(';')[1].Split('=')[1];
        }

        private FileInfo SaveFile(Encoding enc, String contentType, Stream input)
        {

            Byte[] boundaryBytes = enc.GetBytes(GetBoundary(contentType));
            Int32 boundaryLen = boundaryBytes.Length;
            string fileName = Guid.NewGuid().ToString("N") + ".temp";
            DirectoryInfo dir = new DirectoryInfo(FILE_UPLOAD_PATH);
            if (!dir.Exists)
            {
                dir.Create();
            }
            FileInfo res = new FileInfo(Path.Combine(FILE_UPLOAD_PATH, fileName));
            using (FileStream output = new FileStream(res.FullName, FileMode.Create, FileAccess.Write))
            {
                Byte[] buffer = new Byte[1024];
                Int32 len = input.Read(buffer, 0, 1024);
                Int32 startPos = -1;

                // Find start boundary
                while (true)
                {
                    if (len == 0)
                    {
                        throw new Exception("Start Boundaray Not Found");
                    }

                    startPos = IndexOf(buffer, len, boundaryBytes);
                    if (startPos >= 0)
                    {
                        break;
                    }
                    else
                    {
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen);
                    }
                }

                // Skip four lines (Boundary, Content-Disposition, Content-Type, and a blank)
                for (Int32 i = 0; i < 4; i++)
                {
                    while (true)
                    {
                        if (len == 0)
                        {
                            throw new Exception("Preamble not Found.");
                        }

                        startPos = Array.IndexOf(buffer, enc.GetBytes("\n")[0], startPos);
                        if (startPos >= 0)
                        {
                            startPos++;
                            break;
                        }
                        else
                        {
                            len = input.Read(buffer, 0, 1024);
                        }
                    }
                }

                Array.Copy(buffer, startPos, buffer, 0, len - startPos);
                len = len - startPos;

                while (true)
                {
                    Int32 endPos = IndexOf(buffer, len, boundaryBytes);
                    if (endPos >= 0)
                    {
                        if (endPos > 0) output.Write(buffer, 0, endPos - 2);
                        break;
                    }
                    else if (len <= boundaryLen)
                    {
                        throw new Exception("End Boundaray Not Found");
                    }
                    else
                    {
                        output.Write(buffer, 0, len - boundaryLen);
                        //每次放置后40个字节到首部，读取接下来984个字节，在此基础上进行byte查找，绝妙！
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen) + boundaryLen;
                    }
                }
            }

            return res;
        }

        private Int32 IndexOf(Byte[] buffer, Int32 len, Byte[] boundaryBytes)
        {
            for (Int32 i = 0; i <= len - boundaryBytes.Length; i++)
            {
                Boolean match = true;
                for (Int32 j = 0; j < boundaryBytes.Length && match; j++)
                {
                    match = buffer[i + j] == boundaryBytes[j];
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }
        #endregion

        #endregion
    }
}
