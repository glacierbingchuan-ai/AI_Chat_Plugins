using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AI_Chat.Plugins;

namespace Sample
{
    /// <summary>
    /// 示例插件 - 演示所有可用的插件API功能
    /// 
    /// 本插件展示了插件系统的完整功能，包括：
    /// 1. 插件基本信息的定义方式
    /// 2. 生命周期方法的实现
    /// 3. 各种消息处理器的注册和使用
    /// 4. 配置和数据的存取
    /// 5. 插件命令的定义和实现
    /// </summary>
    [Plugin(
        Id = "Sample.SamplePlugin",              // 插件唯一标识符，建议使用"命名空间.类名"格式
        Name = "示例插件",                        // 插件显示名称，用于在UI中展示
        Version = "1.0.0",                       // 插件版本号，遵循语义化版本规范
        Author = "演示作者",                      // 插件作者名称
        Description = "一个完整的示例插件，展示所有插件API的使用方法",  // 插件功能描述
        Priority = 100,                          // 插件优先级，数字越小优先级越高，处理器按优先级顺序执行
        AutoStart = false,                       // 是否自动启动，true表示加载后自动启动
        SupportSandbox = true                    // 是否支持沙箱运行，true表示可以在安全沙箱中运行
    )]
    public class SamplePlugin : PluginBase
    {
        #region 生命周期方法

        /// <summary>
        /// 插件初始化时调用
        /// 
        /// 调用时机：插件被加载后，在Start()之前调用
        /// 用途：
        /// - 读取和初始化配置
        /// - 初始化数据结构
        /// - 建立数据库连接等资源
        /// - 注册服务
        /// 
        /// 注意：
        /// - 此时Api可能还未完全初始化，不要在这里注册消息处理器
        /// - 如果初始化失败，应该抛出异常，插件将不会被启动
        /// </summary>
        protected override void OnInitialize()
        {
            Logger.Info(Id, "示例插件正在初始化...");

            // ========================================
            // 功能演示: 使用 Data 帮助类操作配置和文件
            // ========================================
            // Data 是 PluginDataHelper 类的实例，提供以下功能：
            // - 配置的读写（自动持久化到JSON文件）
            // - 数据文件的读写（文本、JSON、二进制）
            // - 目录操作
            
            // -------------------- 配置操作 --------------------
            
            // Data.Get<T>(key, defaultValue) - 读取配置项
            // 参数说明：
            //   key: 配置项的键名，字符串类型
            //   defaultValue: 如果配置项不存在，返回的默认值
            // 返回值：配置项的值，类型为T
            string welcomeMessage = Data.Get("WelcomeMessage", "欢迎使用示例插件！");
            int counter = Data.Get("Counter", 0);
            
            Logger.Info(Id, $"读取到配置 - 欢迎消息: {welcomeMessage}, 计数器: {counter}");

            // Data.Set<T>(key, value) - 设置配置项
            // 参数说明：
            //   key: 配置项的键名
            //   value: 配置项的值
            // 注意：设置后需要调用 SaveConfig() 才会持久化到文件
            Data.Set("LastInitialized", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Data.Set("Counter", counter + 1);
            
            // Data.SaveConfig() - 保存配置到文件
            // 配置文件保存在：PluginConfigs/{插件ID}.json
            Data.SaveConfig();

            // -------------------- 文件操作 --------------------
            
            try
            {
                // Data.WriteText(relativePath, content) - 写入文本文件
                // 参数说明：
                //   relativePath: 相对于插件数据目录的路径
                //   content: 要写入的文本内容
                // 文件保存在：PluginData/{插件ID}/{relativePath}
                Data.WriteText("example.txt", "这是示例插件写入的文本内容\n" + DateTime.Now);
                
                // Data.ReadText(relativePath) - 读取文本文件
                // 参数说明：
                //   relativePath: 相对于插件数据目录的路径
                // 返回值：文件内容字符串，文件不存在返回null
                string content = Data.ReadText("example.txt");
                Logger.Info(Id, "读取到文件内容: " + content);

                // Data.SaveJson<T>(relativePath, data) - 保存对象为JSON文件
                // 参数说明：
                //   relativePath: 相对路径
                //   data: 要保存的对象（会被序列化为JSON）
                var sampleData = new { Name = "示例数据", Value = 123, CreatedAt = DateTime.Now };
                Data.SaveJson("sample_data.json", sampleData);

                // Data.LoadJson<T>(relativePath, defaultValue) - 从JSON文件加载对象
                // 参数说明：
                //   relativePath: 相对路径
                //   defaultValue: 文件不存在时的默认返回值
                // 返回值：反序列化后的对象
                var loadedData = Data.LoadJson<dynamic>("sample_data.json");
                Logger.Info(Id, "读取到JSON数据: " + loadedData?.Name);
            }
            catch (Exception ex)
            {
                Logger.Error(Id, "文件操作出错", ex);
            }

            Logger.Info(Id, "示例插件初始化完成");
        }

        /// <summary>
        /// 插件启动时调用
        /// 
        /// 调用时机：Initialize()成功后，手动或自动启动时调用
        /// 用途：
        /// - 注册消息处理器
        /// - 启动后台任务
        /// - 开始监听事件
        /// 
        /// 注意：
        /// - 此时Api已完全初始化，可以安全使用所有API
        /// - 注册的处理器会按优先级顺序执行
        /// </summary>
        protected override void OnStart()
        {
            Logger.Info(Id, "示例插件正在启动...");

            if (Api == null)
            {
                Logger.Warning(Id, "API 不可用，部分功能无法演示");
                return;
            }

            // ========================================
            // 功能演示: 注册合并前消息处理器
            // ========================================
            // 触发时机：用户发送消息后，消息合并之前
            // 用途：
            // - 拦截用户消息（不继续处理）
            // - 修改用户原始输入
            // - 记录用户消息日志
            // 
            // 处理器按优先级顺序执行，优先级低的先执行
            
            Api.RegisterPreMergeMessageHandler((context) =>
            {
                // PreMergeMessageContext 上下文说明：
                // - UserId: 发送消息的用户ID（长整型）
                // - RawMessage: 用户的原始消息内容（字符串）
                // - Source: 消息来源标识（如"private"、"group"等）
                // - Timestamp: 消息发送时间
                
                Logger.Debug(Id, $"[合并前] 收到用户 {context.UserId} 的消息: {context.RawMessage}");

                // PreMergeMessageResult 返回值说明：
                // - IsIntercepted: 是否拦截消息（true则不继续处理，直接返回Response）
                // - Response: 拦截时返回给用户的响应内容
                // - ModifiedMessage: 修改后的消息内容（继续处理时使用）
                // - IsModified: 是否修改了消息
                
                // 示例1: 拦截消息 - 如果消息包含 "拦截我"，则拦截并直接回复
                if (context.RawMessage.Contains("拦截我"))
                {
                    return new PreMergeMessageResult
                    {
                        IsIntercepted = true,  // 拦截消息，不继续处理
                        Response = "这条消息已被示例插件拦截！这是直接返回的响应。"
                    };
                }

                // 示例2: 修改消息 - 如果消息包含 "修改我"，则修改消息内容
                if (context.RawMessage.Contains("修改我"))
                {
                    return new PreMergeMessageResult
                    {
                        IsModified = true,  // 标记消息已修改
                        ModifiedMessage = context.RawMessage.Replace("修改我", "[已被示例插件修改]")
                    };
                }

                // 默认：不做任何处理，继续正常流程
                return new PreMergeMessageResult();
            });

            // ========================================
            // 功能演示: 注册合并后消息处理器
            // ========================================
            // 触发时机：多条消息合并完成后，发送给LLM之前
            // 用途：
            // - 拦截合并后的完整消息
            // - 修改将要发送给LLM的消息内容
            // - 基于完整消息做额外处理
            
            Api.RegisterPostMergeMessageHandler((context) =>
            {
                // PostMergeMessageContext 上下文说明：
                // - UserId: 用户ID
                // - FullMessage: 合并后的完整消息内容
                // - Source: 消息来源
                // - Timestamp: 时间戳
                // - MessageFragments: 合并前的消息片段列表
                
                Logger.Debug(Id, $"[合并后] 完整消息长度: {context.FullMessage.Length}");

                // PostMergeMessageResult 返回值说明：
                // - IsIntercepted: 是否拦截（true则不发送给LLM）
                // - Response: 拦截时返回给用户的内容
                // - ModifiedMessage: 修改后的消息内容
                // - IsModified: 是否修改了消息
                
                // 示例：给所有消息添加前缀标记
                if (!context.FullMessage.StartsWith("[示例插件]"))
                {
                    return new PostMergeMessageResult
                    {
                        IsModified = true,
                        ModifiedMessage = "[示例插件]" + context.FullMessage
                    };
                }

                return new PostMergeMessageResult();
            });

            // ========================================
            // 功能演示: 注册消息追加完成处理器
            // ========================================
            // 触发时机：当新消息被追加到上一条用户消息时（消息合并场景）
            // 用途：
            // - 监控消息追加行为
            // - 修改追加后的消息
            // - 基于追加内容做特殊处理
            
            Api.RegisterMessageAppendedHandler((context) =>
            {
                // MessageAppendedContext 上下文说明：
                // - UserId: 用户ID
                // - OriginalMessage: 追加前的原始消息
                // - AppendedContent: 新追加的内容
                // - FullMessage: 追加后的完整消息
                // - MessageIndex: 消息在上下文中的索引位置
                
                Logger.Debug(Id, $"[消息追加] 追加了内容: {context.AppendedContent}");
                
                // MessageAppendedResult 返回值说明：
                // - IsIntercepted: 是否拦截
                // - Response: 拦截时的响应
                // - ModifiedMessage: 修改后的完整消息
                // - IsModified: 是否修改
                
                return new MessageAppendedResult();
            });

            // ========================================
            // 功能演示: 注册LLM响应处理器
            // ========================================
            // 触发时机：收到LLM响应后，格式化处理之前
            // 用途：
            // - 拦截AI回复
            // - 修改AI回复内容
            // - 记录AI响应日志
            // - 对AI回复做内容审查
            
            Api.RegisterLLMResponseHandler((context) =>
            {
                // LLMResponseContext 上下文说明：
                // - UserId: 用户ID
                // - RawResponse: LLM的原始响应内容（JSON格式）
                // - RequestId: 请求的唯一标识符
                
                Logger.Debug(Id, "[LLM响应] 收到AI回复");

                // LLMResponseResult 返回值说明：
                // - IsIntercepted: 是否拦截（true则不发送给用户）
                // - AlternativeResponse: 替代的响应内容（JSON字符串）
                // - IsModified: 是否修改了响应
                
                // 示例：给AI回复添加标注后缀
                return new LLMResponseResult
                {
                    IsModified = true,
                    AlternativeResponse = context.RawResponse + "\n\n--- 来自示例插件的标注 ---"
                };
            });

            // ========================================
            // 功能演示: 注册LLM请求前处理器
            // ========================================
            // 触发时机：发送请求给LLM之前
            // 用途：
            // - 修改发送给LLM的请求内容
            // - 添加自定义参数
            // - 拦截请求（不发送给LLM）
            // - 记录请求日志
            
            Api.RegisterPreLLMRequestHandler((context) =>
            {
                // PreLLMRequestContext 上下文说明：
                // - UserId: 用户ID
                // - RequestJson: 将要发送的请求JSON（可修改）
                // - RequestId: 请求唯一标识
                // - ContextMessages: 当前对话的上下文消息列表
                // - UserMessage: 用户输入的原始消息
                
                Logger.Debug(Id, "[LLM请求前] 准备发送请求");
                
                // PreLLMRequestResult 返回值说明：
                // - IsIntercepted: 是否拦截（true则不发送给LLM）
                // - InterceptedResponse: 拦截时的替代响应
                // - ModifiedRequestJson: 修改后的请求JSON
                // - IsModified: 是否修改了请求
                
                return new PreLLMRequestResult();
            });

            // ========================================
            // 功能演示: 注册群聊消息处理器
            // ========================================
            // 触发时机：收到群聊消息时
            // 用途：
            // - 处理群聊消息
            // - 实现群聊机器人功能
            // - 群聊消息过滤和监控
            
            Api.RegisterGroupMessageHandler((context) =>
            {
                // GroupMessageContext 上下文说明：
                // - GroupId: 群聊ID
                // - UserId: 发送者用户ID
                // - MessageId: 消息ID
                // - RawMessage: 原始消息内容
                // - Timestamp: 消息时间戳
                // - SenderNickname: 发送者昵称
                // - MessageArray: 消息卡片数组（CQ码等原始格式）
                
                Logger.Debug(Id, $"[群聊] 收到群 {context.GroupId} 中 {context.SenderNickname} 的消息");

                // GroupMessageResult 返回值说明：
                // - IsHandled: 是否已处理（true则不再传递给其他插件）
                // - ReplyMessage: 回复消息内容（如果设置则自动发送）
                
                // 示例：响应@消息
                if (context.RawMessage.Contains("@示例插件"))
                {
                    return new GroupMessageResult
                    {
                        IsHandled = true,
                        ReplyMessage = $"@{context.SenderNickname} 你好！我是示例插件。"
                    };
                }

                return new GroupMessageResult();
            });

            Logger.Info(Id, "示例插件已启动，所有处理器已注册");
        }

        /// <summary>
        /// 插件停止时调用
        /// 
        /// 调用时机：手动停止插件或程序关闭时
        /// 用途：
        /// - 保存未保存的数据
        /// - 停止后台任务
        /// - 释放占用的资源
        /// - 断开网络连接
        /// 
        /// 注意：
        /// - 应该快速完成，不要执行耗时操作
        /// - 确保数据完整性
        /// </summary>
        protected override void OnStop()
        {
            Logger.Info(Id, "示例插件正在停止...");
            
            // 保存配置到文件
            Data.SaveConfig();
            
            Logger.Info(Id, "示例插件已停止");
        }

        /// <summary>
        /// 插件释放时调用
        /// 
        /// 调用时机：插件被卸载时
        /// 用途：
        /// - 进行最终的清理工作
        /// - 释放所有资源
        /// - 关闭文件句柄
        /// 
        /// 注意：
        /// - 此方法调用后插件实例将被销毁
        /// - 应该释放所有托管和非托管资源
        /// </summary>
        protected override void OnDispose()
        {
            Logger.Info(Id, "示例插件正在释放资源...");
        }

        #endregion

        #region 插件命令

        /// <summary>
        /// 插件命令说明：
        /// 
        /// 通过 [PluginCommand] 特性标记的方法可以被外部调用
        /// 方法签名要求：
        /// - 返回值：object 或 Task&lt;object&gt;
        /// - 参数：Dictionary&lt;string, object&gt; parameters
        /// 
        /// 调用方式：
        /// - 通过 PluginManager.ExecuteCommand(pluginId, commandName, parameters)
        /// - 通过控制面板的命令执行功能
        /// </summary>

        /// <summary>
        /// 获取插件信息命令
        /// 
        /// 功能：返回插件的基本信息
        /// 用法：无参数
        /// 返回：包含插件ID、名称、版本、作者、描述、当前时间的对象
        /// </summary>
        /// <param name="parameters">命令参数字典（此命令不需要参数）</param>
        /// <returns>包含插件信息的匿名对象</returns>
        [PluginCommand("GetInfo", Description = "获取示例插件的基本信息", Usage = "无参数")]
        public object GetInfoCommand(Dictionary<string, object> parameters)
        {
            return new
            {
                PluginId = Id,           // 插件唯一标识符
                PluginName = Name,       // 插件显示名称
                Version = Version.ToString(),  // 插件版本号
                Author = Author,         // 插件作者
                Description = Description,  // 插件描述
                CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")  // 当前时间
            };
        }

        /// <summary>
        /// 发送消息命令
        /// 
        /// 功能：向指定用户发送消息
        /// 用法：parameters["userId"] = 用户ID, parameters["message"] = 消息内容
        /// 返回：包含Success和Message的结果对象
        /// </summary>
        /// <param name="parameters">
        /// 命令参数字典：
        /// - userId: 目标用户ID（长整型）
        /// - message: 要发送的消息内容（字符串）
        /// </param>
        /// <returns>包含发送结果的对象</returns>
        [PluginCommand("SendMessage", Description = "向指定用户发送消息", Usage = "参数: userId(长整型), message(字符串)")]
        public async Task<object> SendMessageCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            try
            {
                // 从参数字典中获取参数值
                // Convert.ToInt64 可以处理字符串和数字类型的转换
                long userId = Convert.ToInt64(parameters["userId"]);
                string message = parameters["message"].ToString();

                // Api.SendMessageAsync 参数说明：
                // - userId: 目标用户ID
                // - message: 消息内容（文本或文件路径）
                // - options: 发送选项（可选）
                //   - MessageType: 消息类型（Text/Image/Voice）
                bool result = await Api.SendMessageAsync(userId, message);
                
                return new { Success = result, Message = result ? "消息发送成功" : "消息发送失败" };
            }
            catch (Exception ex)
            {
                return new { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 获取和修改配置命令
        /// 
        /// 功能：演示配置的读取和修改
        /// 用法：无参数
        /// 返回：包含当前配置信息的对象
        /// </summary>
        /// <param name="parameters">命令参数字典（此命令不需要参数）</param>
        /// <returns>包含配置信息的对象</returns>
        [PluginCommand("ConfigDemo", Description = "演示配置操作", Usage = "无参数")]
        public object ConfigDemoCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            try
            {
                // Api.GetConfig() - 获取完整的软件配置
                // 返回 AppConfig 对象，包含以下字段：
                // - ApiKey: LLM API密钥
                // - ApiUrl: LLM API地址
                // - Model: 使用的模型名称
                // - Temperature: 温度参数
                // - MaxTokens: 最大token数
                // - TopP: Top-P采样参数
                // - WebsocketServerUri: WebSocket服务器地址
                // - WebsocketToken: WebSocket令牌
                // - WebsocketKeepAliveInterval: 保活间隔
                // - MaxContextRounds: 最大上下文轮数
                // - RoleCardsApiUrl: 角色卡API地址
                var config = Api.GetConfig();
                
                // Api.GetConfigValue<T>(key, defaultValue) - 获取特定配置项
                // 参数说明：
                // - key: 配置项名称（对应ControlPanelConfig的属性名）
                // - defaultValue: 默认值
                string model = Api.GetConfigValue<string>("LlmModelName", "未知模型");
                float temp = Api.GetConfigValue<float>("LlmTemperature", 0.7f);

                // Api.SetConfigValue<T>(key, value) - 设置配置项
                // 注意：会自动保存配置并触发配置变更事件
                // Api.SetConfigValue("LlmTemperature", 0.8f);

                return new
                {
                    Success = true,
                    CurrentModel = model,
                    Temperature = temp,
                    ApiUrl = config.ApiUrl,
                    FullConfig = config
                };
            }
            catch (Exception ex)
            {
                return new { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 上下文操作命令
        /// 
        /// 功能：演示对话上下文的操作
        /// 用法：parameters["userId"] = 用户ID
        /// 返回：包含操作结果的对象
        /// </summary>
        /// <param name="parameters">
        /// 命令参数字典：
        /// - userId: 目标用户ID（长整型）
        /// </param>
        /// <returns>包含上下文操作结果的对象</returns>
        [PluginCommand("ContextDemo", Description = "演示上下文操作", Usage = "参数: userId(长整型)")]
        public object ContextDemoCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            try
            {
                long userId = Convert.ToInt64(parameters["userId"]);

                // Api.GetFullContext(userId) - 获取用户的完整对话上下文
                // 返回 List<ContextMessage>，每个ContextMessage包含：
                // - Role: 角色（user/assistant/system）
                // - Content: 消息内容
                // - Timestamp: 时间戳
                // - Tag: 消息类型标记
                var context = Api.GetFullContext(userId);

                // Api.AddContextMessage(userId, role, content) - 添加消息到上下文
                // 参数说明：
                // - userId: 用户ID
                // - role: 角色（"user"/"assistant"/"system"）
                // - content: 消息内容
                Api.AddContextMessage(userId, "system", "这是示例插件添加的系统消息");
                Api.AddContextMessage(userId, "user", "这是示例插件添加的用户消息");
                Api.AddContextMessage(userId, "assistant", "这是示例插件添加的助手消息");

                // Api.RemoveLastMessages(userId, role, count) - 删除指定角色的最后N条消息
                // 参数说明：
                // - userId: 用户ID
                // - role: 角色
                // - count: 删除数量
                // 返回：实际删除的数量
                int removed = Api.RemoveLastMessages(userId, "user", 2);

                // Api.ClearContext(userId) - 清空用户的完整上下文
                // 注意：此操作不可逆！
                // Api.ClearContext(userId);

                return new
                {
                    Success = true,
                    OriginalContextCount = context.Count,
                    RemovedMessages = removed
                };
            }
            catch (Exception ex)
            {
                return new { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 权限查询命令
        /// 
        /// 功能：查询插件的权限信息
        /// 用法：无参数
        /// 返回：包含权限列表的对象
        /// </summary>
        /// <param name="parameters">命令参数字典（此命令不需要参数）</param>
        /// <returns>包含权限信息的对象</returns>
        [PluginCommand("GetPermissions", Description = "获取插件权限列表", Usage = "无参数")]
        public object GetPermissionsCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            // Api.GetRegisteredPermissions() - 获取当前插件的已注册权限
            // 权限是在注册处理器时自动记录的
            var myPermissions = Api.GetRegisteredPermissions();
            
            // Api.GetAllPluginPermissions() - 获取所有插件的权限信息
            // 返回 Dictionary<string, List<string>>，键为插件ID，值为权限列表
            var allPermissions = Api.GetAllPluginPermissions();

            // Api.GetPluginPermissions(pluginId) - 获取指定插件的权限列表
            // var otherPluginPerms = Api.GetPluginPermissions("Other.Plugin.Id");

            return new
            {
                Success = true,
                MyPermissions = myPermissions,
                AllPluginsPermissions = allPermissions
            };
        }

        /// <summary>
        /// 用户管理命令
        /// 
        /// 功能：演示用户管理功能
        /// 用法：无参数
        /// 返回：包含允许用户列表的对象
        /// </summary>
        /// <param name="parameters">命令参数字典（此命令不需要参数）</param>
        /// <returns>包含用户管理信息的对象</returns>
        [PluginCommand("UserManagementDemo", Description = "演示用户管理功能", Usage = "无参数")]
        public object UserManagementDemoCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            // Api.GetAllowedUserIds() - 获取所有允许的用户ID列表
            // 返回 List<long>
            var allowedUsers = Api.GetAllowedUserIds();
            
            // Api.IsUserAllowed(userId) - 检查用户是否被允许
            // 参数：userId - 用户ID
            // 返回：bool
            // bool isAllowed = Api.IsUserAllowed(12345);
            
            // Api.AddAllowedUser(userId) - 添加允许的用户
            // 参数：userId - 用户ID
            // Api.AddAllowedUser(12345);
            
            // Api.RemoveAllowedUser(userId) - 移除允许的用户
            // 参数：userId - 用户ID
            // Api.RemoveAllowedUser(12345);

            return new
            {
                Success = true,
                AllowedUserIds = allowedUsers
            };
        }

        /// <summary>
        /// 群聊管理命令
        /// 
        /// 功能：演示群聊管理功能
        /// 用法：无参数
        /// 返回：包含允许群聊列表的对象
        /// </summary>
        /// <param name="parameters">命令参数字典（此命令不需要参数）</param>
        /// <returns>包含群聊管理信息的对象</returns>
        [PluginCommand("GroupManagementDemo", Description = "演示群聊管理功能", Usage = "无参数")]
        public object GroupManagementDemoCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            // Api.GetAllowedGroupIds() - 获取所有允许的群聊ID列表
            // 返回 List<long>
            var allowedGroups = Api.GetAllowedGroupIds();

            // Api.IsGroupAllowed(groupId) - 检查群聊是否被允许
            // 参数：groupId - 群聊ID
            // 返回：bool
            
            // Api.AddAllowedGroup(groupId) - 添加允许的群聊
            // 参数：groupId - 群聊ID
            
            // Api.RemoveAllowedGroup(groupId) - 移除允许的群聊
            // 参数：groupId - 群聊ID

            return new
            {
                Success = true,
                AllowedGroupIds = allowedGroups
            };
        }

        /// <summary>
        /// 调用LLM命令
        /// 
        /// 功能：直接调用大模型API
        /// 用法：parameters["prompt"] = 提示词
        /// 返回：包含请求和响应的对象
        /// </summary>
        /// <param name="parameters">
        /// 命令参数字典：
        /// - prompt: 发送给LLM的提示词（字符串）
        /// </param>
        /// <returns>包含LLM调用结果的对象</returns>
        [PluginCommand("CallLLM", Description = "直接调用大模型", Usage = "参数: prompt(字符串)")]
        public async Task<object> CallLLMCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            try
            {
                string prompt = parameters["prompt"].ToString();

                // Api.RequestLLMAsync(requestJson) - 直接调用LLM
                // 参数：requestJson - OpenAI格式的请求JSON字符串
                // 返回：LLM的原始响应JSON字符串
                // 
                // 请求JSON格式（OpenAI兼容）：
                // {
                //   "model": "模型名称",
                //   "messages": [
                //     { "role": "system", "content": "系统提示" },
                //     { "role": "user", "content": "用户消息" }
                //   ],
                //   "temperature": 0.7,
                //   "max_tokens": 500,
                //   "top_p": 1.0
                // }
                string requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                });

                string response = await Api.RequestLLMAsync(requestJson);

                return new
                {
                    Success = true,
                    Request = requestJson,
                    Response = response
                };
            }
            catch (Exception ex)
            {
                return new { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 发送群聊消息命令
        /// 
        /// 功能：向指定群聊发送消息
        /// 用法：parameters["groupId"] = 群聊ID, parameters["message"] = 消息内容
        /// 返回：包含发送结果的对象
        /// </summary>
        /// <param name="parameters">
        /// 命令参数字典：
        /// - groupId: 目标群聊ID（长整型）
        /// - message: 要发送的消息内容（字符串）
        /// </param>
        /// <returns>包含发送结果的对象</returns>
        [PluginCommand("SendGroupMessage", Description = "向指定群聊发送消息", Usage = "参数: groupId(长整型), message(字符串)")]
        public async Task<object> SendGroupMessageCommand(Dictionary<string, object> parameters)
        {
            if (Api == null)
                return new { Success = false, Error = "API 不可用" };

            try
            {
                long groupId = Convert.ToInt64(parameters["groupId"]);
                string message = parameters["message"].ToString();

                // Api.SendGroupMessageAsync(groupId, message, options) - 发送群聊消息
                // 参数说明：
                // - groupId: 目标群聊ID
                // - message: 消息内容
                // - options: 发送选项（可选）
                bool result = await Api.SendGroupMessageAsync(groupId, message);
                
                return new { Success = result, Message = result ? "群聊消息发送成功" : "群聊消息发送失败" };
            }
            catch (Exception ex)
            {
                return new { Success = false, Error = ex.Message };
            }
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 获取插件自述文档（HTML格式）
        /// 
        /// 用途：返回插件的说明文档，会在控制面板中显示
        /// 格式：HTML格式的内容
        /// </summary>
        /// <returns>HTML格式的自述文档</returns>
        public override string GetReadme()
        {
            return $@"
<h1>{Name}</h1>
<p><strong>版本:</strong> {Version}</p>
<p><strong>作者:</strong> {Author}</p>
<p><strong>描述:</strong> {Description}</p>

<h2>功能概述</h2>
<p>这是一个完整的示例插件，展示了插件系统的所有功能：</p>
<ul>
    <li>消息处理（合并前、合并后、追加完成）</li>
    <li>LLM响应处理</li>
    <li>LLM请求处理</li>
    <li>群聊消息处理</li>
    <li>配置管理</li>
    <li>上下文管理</li>
    <li>文件操作</li>
    <li>发送消息</li>
    <li>用户和群聊管理</li>
    <li>直接调用LLM</li>
    <li>插件命令</li>
</ul>

<h2>插件命令</h2>
<ul>
    <li><strong>GetInfo</strong> - 获取插件基本信息</li>
    <li><strong>SendMessage</strong> - 发送消息给用户</li>
    <li><strong>SendGroupMessage</strong> - 发送消息到群聊</li>
    <li><strong>ConfigDemo</strong> - 演示配置操作</li>
    <li><strong>ContextDemo</strong> - 演示上下文操作</li>
    <li><strong>GetPermissions</strong> - 获取权限列表</li>
    <li><strong>UserManagementDemo</strong> - 用户管理演示</li>
    <li><strong>GroupManagementDemo</strong> - 群聊管理演示</li>
    <li><strong>CallLLM</strong> - 直接调用大模型</li>
</ul>

<h2>使用示例</h2>
<p>发送包含""拦截我""的消息，插件会直接回复而不经过LLM。</p>
<p>发送包含""修改我""的消息，插件会修改消息内容。</p>
<p>在群聊中@示例插件，插件会回复你。</p>
";
        }

        /// <summary>
        /// 获取插件权限列表
        /// 
        /// 用途：返回插件声明的权限列表
        /// 说明：权限信息用于在控制面板中展示插件的功能范围
        /// </summary>
        /// <returns>权限描述列表</returns>
        public override List<string> GetPermissions()
        {
            var permissions = base.GetPermissions();
            
            // 可以在这里添加额外的权限声明
            permissions.Add("示例插件额外权限声明");
            
            return permissions;
        }

        #endregion
    }
}
