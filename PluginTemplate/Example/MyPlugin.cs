using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// AI_Chat 框架的插件核心依赖
using AI_Chat.Plugins;//请引用AI_Chat.exe
// Newtonsoft.Json（Json.NET）：.NET 生态最常用的 JSON 处理库
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AI_Chat.Plugin.Template
{
    /// <summary>
    /// AI_Chat 插件标准实现模板 - 完整功能示例
    /// =================================================================================
    /// 【设计说明】：
    /// 1. 所有插件必须继承自框架提供的 PluginBase 基类
    /// 2. 通过 [Plugin] 特性标记插件元数据，框架会自动扫描并加载
    /// 3. 核心生命周期：Initialize（初始化）→ Start（启动）→ Stop（停止）
    /// 4. 支持五种核心拦截器：
    ///    - PreMergeMessageHandler：消息合并前（原始消息）
    ///    - PostMergeMessageHandler：消息合并后（完整消息）
    ///    - MessageAppendedHandler：消息追加完成时
    ///    - PreLLMRequestHandler：LLM请求前（可修改请求JSON）
    ///    - LLMResponseHandler：AI回复生成后
    /// 5. 内置配置管理和数据持久化能力，无需手动处理文件路径
    /// 6. 支持通过 [PluginCommand] 特性暴露公开指令
    /// =================================================================================
    /// </summary>
    [Plugin(
        Id = "MyPlugin",              // 插件唯一标识（全局不可重复），建议使用英文+版本的形式
        Name = "我的插件",             // 插件显示名称（前端面板展示）
        Version = "1.0.0",             // 插件版本号（遵循语义化版本规范：主版本.次版本.修订号）
        Author = "YourName",           // 插件作者
        Description = "这是一个完整的插件开发模板，展示了所有可用功能", // 插件简短描述
        AutoStart = true,              // 是否随框架启动自动加载（true=自动启动，false=手动启动）
        Priority = 10                  // 插件执行优先级（数值越小优先级越高，范围 1-99）
    )]
    public class MyPlugin : PluginBase
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // 插件元数据重写（必须实现）
        // 【设计说明】：
        // PluginBase 基类定义了抽象属性，子类必须重写
        // 这些属性值必须与 [Plugin] 特性中的值完全一致
        // 框架通过这些属性在运行时识别插件信息
        // ═══════════════════════════════════════════════════════════════════════════════
        public override string Id => "MyPlugin";
        public override string Name => "我的插件";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "YourName";
        public override string Description => "这是一个完整的插件开发模板，展示了所有可用功能";

        // ═══════════════════════════════════════════════════════════════════════════════
        // 插件运行时状态（私有字段）
        // 【设计说明】：
        // 用于存储插件运行过程中的临时/累计数据
        // 私有化字段 + 按需提供公开访问方法是封装的最佳实践
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 已处理的 AI 回复总数（运行时统计）
        /// 【业务说明】：每次修改AI回复内容时计数+1，用于展示插件运行状态
        /// </summary>
        private int _processCount = 0;

        /// <summary>
        /// 已处理的用户消息数量
        /// </summary>
        private int _messageCount = 0;

        /// <summary>
        /// 插件启动时间（用于计算运行时长）
        /// </summary>
        private DateTime _startTime;

        /// <summary>
        /// 插件本地数据存储文件名
        /// 【路径说明】：框架会自动将此文件放在插件专属目录下，无需拼接路径
        /// 【用途】：持久化存储需要跨进程保留的数据（如统计计数、用户配置）
        /// </summary>
        private const string DATA_FILE = "plugin_data.json";

        // ═══════════════════════════════════════════════════════════════════════════════
        // 配置项键名常量定义
        // 【设计说明】：
        // 1. 使用常量避免硬编码，降低维护成本
        // 2. 统一管理配置键名，防止拼写错误
        // 3. 配置值会被框架持久化到配置文件，支持运行时修改
        // ═══════════════════════════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────────────────────────────
        // 基础功能配置
        // ─────────────────────────────────────────────────────────────────────────────
        private const string CFG_ENABLE_PREFIX = "EnablePrefix";           // 是否启用AI回复前缀（布尔值）
        private const string CFG_PREFIX_TEXT = "PrefixText";               // AI回复前缀文本（字符串）
        private const string CFG_MAX_DELAY = "MaxDelay";                   // AI回复最大延迟限制（整数，毫秒）
        private const string CFG_REPLY_TEMPLATE = "ReplyTemplate";         // 回复模板（字符串）

        // ─────────────────────────────────────────────────────────────────────────────
        // 内容处理配置
        // ─────────────────────────────────────────────────────────────────────────────
        private const string CFG_ENABLE_FILTER = "EnableFilter";           // 是否启用敏感词过滤（布尔值）
        private const string CFG_SENSITIVE_WORDS = "SensitiveWords";       // 敏感词列表（字符串数组，逗号分隔）
        private const string CFG_MAX_MESSAGE_LENGTH = "MaxMessageLength";  // 最大消息长度（整数）
        private const string CFG_ENABLE_EMOJI = "EnableEmoji";             // 是否启用表情处理（布尔值）

        // ─────────────────────────────────────────────────────────────────────────────
        // 高级功能配置
        // ─────────────────────────────────────────────────────────────────────────────
        private const string CFG_ENABLE_STATS = "EnableStats";             // 是否启用统计功能（布尔值）
        private const string CFG_LOG_LEVEL = "LogLevel";                   // 日志级别（字符串：Debug/Info/Warning/Error）
        private const string CFG_PROBABILITY = "Probability";              // 功能触发概率（浮点数，0-1）
        private const string CFG_CUSTOM_RESPONSE = "CustomResponse";       // 自定义响应内容（字符串）

        // ─────────────────────────────────────────────────────────────────────────────
        // 示例配置（用于演示各种功能）
        // ─────────────────────────────────────────────────────────────────────────────
        private const string CFG_ENABLE_TIMESTAMP = "EnableTimestamp";     // 是否启用时间戳（布尔值）
        private const string CFG_ENABLE_APPEND_MARK = "EnableAppendMark";  // 是否添加追加标记（布尔值）
        private const string CFG_MAX_CONTENT_LENGTH = "MaxContentLength";  // 回复内容最大长度（整数）
        private const string CFG_ENABLE_BLOCK_CHECK = "EnableBlockCheck";  // 是否启用内容拦截检查（布尔值）

        // ═══════════════════════════════════════════════════════════════════════════════
        // 插件核心生命周期 - 初始化（OnInitialize）
        // 【执行时机】：插件被框架加载时执行一次（仅执行一次）
        // 【核心作用】：
        // 1. 初始化默认配置（首次运行时）
        // 2. 加载持久化数据（如历史统计、用户配置）
        // 3. 初始化依赖资源（如数据库连接、第三方API客户端）
        // ═══════════════════════════════════════════════════════════════════════════════
        protected override void OnInitialize()
        {
            // Logger 是框架提供的日志对象，自动集成到框架日志系统
            // 建议所有关键操作都记录日志，方便问题排查
            Logger?.Info(Id, "═══════════════════════════════════════════════════");
            Logger?.Info(Id, $"插件 '{Name}' 初始化开始");
            Logger?.Info(Id, "═══════════════════════════════════════════════════");

            try
            {
                // 1. 初始化插件配置
                // GetConfiguration()：获取当前插件的所有配置（返回键值对字典）
                Dictionary<string, object> config = GetConfiguration();
                bool configChanged = false;

                // 检查配置项是否存在，不存在则设置默认值
                // 【设计考量】：首次安装插件时无配置，需要初始化默认值
                // 【配置类型说明】：
                // - 布尔值：true/false，用于功能开关
                // - 整数：用于数值限制（延迟、长度等）
                // - 浮点数：用于概率、比例（0-1范围）
                // - 字符串：用于文本模板、消息内容
                // - 字符串数组：逗号分隔的列表，用于多值配置

                // ─────────────────────────────────────────────────────────────────
                // 基础功能配置初始化
                // ─────────────────────────────────────────────────────────────────
                if (!config.ContainsKey(CFG_ENABLE_PREFIX))
                {
                    config[CFG_ENABLE_PREFIX] = true;           // 默认启用AI回复前缀
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_PREFIX_TEXT))
                {
                    config[CFG_PREFIX_TEXT] = "[AI增强]";       // 默认前缀文本
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_MAX_DELAY))
                {
                    config[CFG_MAX_DELAY] = 5000;               // 默认最大延迟5000毫秒
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_REPLY_TEMPLATE))
                {
                    config[CFG_REPLY_TEMPLATE] = "{prefix} {content}"; // 默认回复模板
                    configChanged = true;
                }

                // ─────────────────────────────────────────────────────────────────
                // 内容处理配置初始化
                // ─────────────────────────────────────────────────────────────────
                if (!config.ContainsKey(CFG_ENABLE_FILTER))
                {
                    config[CFG_ENABLE_FILTER] = true;           // 默认启用敏感词过滤
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_SENSITIVE_WORDS))
                {
                    config[CFG_SENSITIVE_WORDS] = "脏话,广告,诈骗"; // 默认敏感词列表
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_MAX_MESSAGE_LENGTH))
                {
                    config[CFG_MAX_MESSAGE_LENGTH] = 2000;      // 默认最大消息长度2000字符
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_ENABLE_EMOJI))
                {
                    config[CFG_ENABLE_EMOJI] = true;            // 默认启用表情处理
                    configChanged = true;
                }

                // ─────────────────────────────────────────────────────────────────
                // 高级功能配置初始化
                // ─────────────────────────────────────────────────────────────────
                if (!config.ContainsKey(CFG_ENABLE_STATS))
                {
                    config[CFG_ENABLE_STATS] = true;            // 默认启用统计功能
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_LOG_LEVEL))
                {
                    config[CFG_LOG_LEVEL] = "Info";             // 默认日志级别
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_PROBABILITY))
                {
                    config[CFG_PROBABILITY] = 1.0;              // 默认100%触发概率
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_CUSTOM_RESPONSE))
                {
                    config[CFG_CUSTOM_RESPONSE] = "这是自定义响应消息，可在配置中修改！";
                    configChanged = true;
                }

                // ─────────────────────────────────────────────────────────────────
                // 示例配置初始化（用于演示功能）
                // ─────────────────────────────────────────────────────────────────
                if (!config.ContainsKey(CFG_ENABLE_TIMESTAMP))
                {
                    config[CFG_ENABLE_TIMESTAMP] = false;       // 默认不启用时间戳
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_ENABLE_APPEND_MARK))
                {
                    config[CFG_ENABLE_APPEND_MARK] = false;     // 默认不添加追加标记
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_MAX_CONTENT_LENGTH))
                {
                    config[CFG_MAX_CONTENT_LENGTH] = 1000;      // 默认最大内容长度1000字符
                    configChanged = true;
                }

                if (!config.ContainsKey(CFG_ENABLE_BLOCK_CHECK))
                {
                    config[CFG_ENABLE_BLOCK_CHECK] = false;     // 默认不启用内容拦截
                    configChanged = true;
                }

                // 如果有新配置项被添加，提交配置并持久化到文件
                // 【注意】：SetConfiguration 会将配置保存到 {Id}.json 文件
                if (configChanged)
                {
                    SetConfiguration(config);
                    Logger?.Info(Id, "✅ 默认配置已初始化并保存");
                }
                else
                {
                    Logger?.Info(Id, "ℹ️ 配置已存在，跳过初始化");
                }

                // 2. 加载本地持久化数据
                // LoadData<T>()：框架封装的通用数据加载方法，自动处理路径和反序列化
                // dynamic 类型：灵活接收JSON数据，无需定义实体类
                dynamic data = LoadData<dynamic>(DATA_FILE);
                if (data != null)
                {
                    // 恢复历史统计数据
                    if (data.ProcessCount != null)
                        _processCount = data.ProcessCount;
                    if (data.MessageCount != null)
                        _messageCount = data.MessageCount;

                    Logger?.Info(Id, $"📊 历史数据加载完成：处理消息 {_messageCount} 条，修改回复 {_processCount} 次");
                }
                else
                {
                    // 无历史数据时计数器归零（首次运行）
                    Logger?.Info(Id, "📭 无历史数据，计数器归零");
                }

                Logger?.Info(Id, "═══════════════════════════════════════════════════");
                Logger?.Info(Id, $"插件 '{Name}' 初始化完成");
                Logger?.Info(Id, "═══════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                // 捕获初始化异常并记录（避免插件初始化失败导致框架崩溃）
                // 异常日志需包含异常对象，方便查看堆栈信息
                Logger?.Error(Id, "❌ 插件初始化异常", ex);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 插件核心生命周期 - 启动（OnStart）
        // 【执行时机】：插件初始化完成后执行（AutoStart=true 时自动执行）
        // 【核心作用】：
        // 1. 注册各种消息拦截器/处理器
        // 2. 启动定时任务/后台线程
        // 3. 建立网络连接/监听端口
        // ═══════════════════════════════════════════════════════════════════════════════
        protected override void OnStart()
        {
            _startTime = DateTime.Now;
            Logger?.Info(Id, "═══════════════════════════════════════════════════");
            Logger?.Info(Id, $"🚀 插件 '{Name}' 已启动");
            Logger?.Info(Id, "═══════════════════════════════════════════════════");

            // 安全检查：Api 是框架提供的核心接口实例，必须非空才能注册处理器
            if (Api == null)
            {
                Logger?.Error(Id, "❌ API 实例未初始化，无法注册拦截器");
                return;
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // 拦截器 1：消息合并前拦截器（PreMergeMessageHandler）
            // 【触发时机】：用户发送的分段消息被合并为完整消息之前
            // 【适用场景】：
            // 1. 敏感词/违禁词过滤（提前拦截，避免拼接完整消息）
            // 2. 快速指令识别（无需等待完整消息）
            // 3. 消息格式校验（如长度限制）
            // 4. 修改原始消息内容（IsModified=true）
            // ═══════════════════════════════════════════════════════════════════════════
            Api.RegisterPreMergeMessageHandler(ctx =>
            {
                // ctx（当前消息信息）：包含当前正在处理的这条消息的信息
                // RawMessage：用户当前发送的单条原始消息（未合并）
                // Source：发送者ID（如QQ号），对应 msgData.user_id
                // Timestamp：消息时间戳

                try
                {
                    // 判空检查
                    if (string.IsNullOrWhiteSpace(ctx.RawMessage))
                        return new PreMergeMessageResult();

                    string rawMessage = ctx.RawMessage;
                    bool isModified = false;

                    // ─────────────────────────────────────────────────────────────
                    // 处理 1：敏感词过滤（替换为***）
                    // 【说明】：使用 IsModified 修改消息，而不是拦截
                    // ─────────────────────────────────────────────────────────────
                    bool enableFilter = GetConfig(CFG_ENABLE_FILTER, true);
                    if (enableFilter)
                    {
                        string sensitiveWordsStr = GetConfig(CFG_SENSITIVE_WORDS, "脏话,广告,诈骗");
                        string[] sensitiveWords = sensitiveWordsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var word in sensitiveWords)
                        {
                            if (rawMessage.Contains(word))
                            {
                                rawMessage = rawMessage.Replace(word, "***");
                                isModified = true;
                                Logger?.Info(Id, $"🔄 敏感词已替换：'{word}' → '***'");
                            }
                        }
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 2：消息长度限制（截断超长消息）
                    // 【说明】：使用 IsModified 截断消息，而不是拦截
                    // ─────────────────────────────────────────────────────────────
                    int maxLength = GetConfig(CFG_MAX_MESSAGE_LENGTH, 2000);
                    if (rawMessage.Length > maxLength)
                    {
                        rawMessage = rawMessage.Substring(0, maxLength) + "...[已截断]";
                        isModified = true;
                        Logger?.Warning(Id, $"✂️ 消息已截断：原长度 {ctx.RawMessage.Length} → {maxLength}");
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 3：自动添加前缀（示例）
                    // 【说明】：为特定用户的消息添加标识
                    // ─────────────────────────────────────────────────────────────
                    if (ctx.Source == "12345678") // 示例：特定用户ID
                    {
                        rawMessage = "[VIP] " + rawMessage;
                        isModified = true;
                    }

                    // 如果消息被修改，返回修改后的结果
                    if (isModified)
                    {
                        return new PreMergeMessageResult
                        {
                            IsModified = true,
                            ModifiedMessage = rawMessage
                        };
                    }

                    // 返回空结果：框架继续处理原始消息
                    return new PreMergeMessageResult();
                }
                catch (Exception ex)
                {
                    Logger?.Error(Id, "PreMergeMessageHandler 处理异常", ex);
                    return new PreMergeMessageResult();
                }
            });

            // ═══════════════════════════════════════════════════════════════════════════
            // 拦截器 2：消息合并后拦截器（PostMergeMessageHandler）
            // 【触发时机】：用户发送的分段消息被合并为完整消息之后
            // 【适用场景】：
            // 1. 完整指令解析（如 !状态、!帮助）
            // 2. 语义分析（需要完整消息内容）
            // 3. 自定义回复（直接返回结果，无需调用AI）
            // 4. 修改完整消息内容（IsModified=true）
            // ═══════════════════════════════════════════════════════════════════════════
            Api.RegisterPostMergeMessageHandler(ctx =>
            {
                // FullMessage：合并后的完整用户消息
                // Source：固定为 "user"（不是发送者ID）
                // Timestamp：消息时间戳
                // MessageFragments：消息片段列表（当前实现为包含 FullMessage 的单元素列表）

                try
                {
                    // 判空检查
                    if (string.IsNullOrWhiteSpace(ctx.FullMessage))
                        return new PostMergeMessageResult();

                    string fullMessage = ctx.FullMessage.Trim();

                    // 统计消息数量
                    _messageCount++;

                    // ─────────────────────────────────────────────────────────────
                    // 处理 1：指令处理（拦截并直接回复）
                    // 【说明】：使用 IsIntercepted 拦截消息，不交给AI处理
                    // ─────────────────────────────────────────────────────────────

                    // 指令：!帮助 / !help
                    if (fullMessage.Equals("!帮助", StringComparison.OrdinalIgnoreCase) ||
                        fullMessage.Equals("!help", StringComparison.OrdinalIgnoreCase))
                    {
                        return new PostMergeMessageResult
                        {
                            IsIntercepted = true,
                            Response = GetHelpText()
                        };
                    }

                    // 指令：!状态 / !status
                    if (fullMessage.Equals("!状态", StringComparison.OrdinalIgnoreCase) ||
                        fullMessage.Equals("!status", StringComparison.OrdinalIgnoreCase))
                    {
                        return new PostMergeMessageResult
                        {
                            IsIntercepted = true,
                            Response = GetStatusText()
                        };
                    }

                    // 指令：!统计 / !stats
                    if (fullMessage.Equals("!统计", StringComparison.OrdinalIgnoreCase) ||
                        fullMessage.Equals("!stats", StringComparison.OrdinalIgnoreCase))
                    {
                        return new PostMergeMessageResult
                        {
                            IsIntercepted = true,
                            Response = GetStatsText()
                        };
                    }

                    // 指令：!配置 / !config
                    if (fullMessage.Equals("!配置", StringComparison.OrdinalIgnoreCase) ||
                        fullMessage.Equals("!config", StringComparison.OrdinalIgnoreCase))
                    {
                        return new PostMergeMessageResult
                        {
                            IsIntercepted = true,
                            Response = GetConfigText()
                        };
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 2：修改完整消息（示例：添加时间戳）
                    // 【说明】：使用 IsModified 修改消息，然后交给AI处理
                    // ─────────────────────────────────────────────────────────────
                    bool enableTimestamp = GetConfig(CFG_ENABLE_TIMESTAMP, false);
                    if (enableTimestamp)
                    {
                        string timestamp = DateTime.Now.ToString("[HH:mm:ss] ");
                        string modifiedMessage = timestamp + fullMessage;

                        Logger?.Info(Id, $"🕐 已添加时间戳：{timestamp}");

                        return new PostMergeMessageResult
                        {
                            IsModified = true,
                            ModifiedMessage = modifiedMessage
                        };
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 3：消息格式化（示例：统一换行符）
                    // 【说明】：修改消息格式，然后交给AI处理
                    // ─────────────────────────────────────────────────────────────
                    if (fullMessage.Contains("\\n"))
                    {
                        string modifiedMessage = fullMessage.Replace("\\n", "\n");

                        return new PostMergeMessageResult
                        {
                            IsModified = true,
                            ModifiedMessage = modifiedMessage
                        };
                    }

                    // 返回空结果：框架继续调用AI处理原始消息
                    return new PostMergeMessageResult();
                }
                catch (Exception ex)
                {
                    Logger?.Error(Id, "PostMergeMessageHandler 处理异常", ex);
                    return new PostMergeMessageResult();
                }
            });

            // ═══════════════════════════════════════════════════════════════════════════
            // 拦截器 3：消息追加完成拦截器（MessageAppendedHandler）
            // 【触发时机】：当消息被追加到上一条用户消息时触发
            // 【适用场景】：
            // 1. 重新处理追加后的完整消息
            // 2. 更新消息统计信息
            // 3. 触发特定于追加消息的逻辑
            // 4. 修改追加后的消息内容（IsModified=true）
            // ═══════════════════════════════════════════════════════════════════════════
            Api.RegisterMessageAppendedHandler(ctx =>
            {
                // OriginalMessage：追加前的完整消息内容
                // AppendedContent：追加的新消息内容
                // FullMessage：追加后的完整消息内容
                // MessageIndex：消息在上下文中的索引

                try
                {
                    Logger?.Debug(Id, $"📝 消息追加完成：索引={ctx.MessageIndex}，追加内容长度={ctx.AppendedContent?.Length ?? 0}");

                    string fullMessage = ctx.FullMessage;
                    bool isModified = false;

                    // ─────────────────────────────────────────────────────────────
                    // 处理 1：移除重复的时间戳（示例）
                    // 【说明】：追加消息后可能产生重复的时间戳，需要清理
                    // ─────────────────────────────────────────────────────────────
                    System.Text.RegularExpressions.Regex timestampRegex =
                        new System.Text.RegularExpressions.Regex(@"\[\d{2}:\d{2}:\d{2}\] ");
                    var matches = timestampRegex.Matches(fullMessage);
                    if (matches.Count > 1)
                    {
                        // 只保留第一个时间戳
                        fullMessage = matches[0].Value + timestampRegex.Replace(fullMessage, "");
                        isModified = true;
                        Logger?.Info(Id, "🔄 已清理重复时间戳");
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 2：合并重复内容（示例）
                    // 【说明】：如果追加内容与原有内容重复，进行合并
                    // ─────────────────────────────────────────────────────────────
                    if (!string.IsNullOrEmpty(ctx.AppendedContent) &&
                        ctx.OriginalMessage.EndsWith(ctx.AppendedContent))
                    {
                        // 追加内容与原有内容重复，移除重复部分
                        fullMessage = ctx.OriginalMessage;
                        isModified = true;
                        Logger?.Info(Id, "🔄 已合并重复内容");
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 3：添加追加标记（示例）
                    // 【说明】：在追加的消息后添加标记
                    // ─────────────────────────────────────────────────────────────
                    if (GetConfig(CFG_ENABLE_APPEND_MARK, false))
                    {
                        fullMessage += " [已追加]";
                        isModified = true;
                    }

                    // 如果消息被修改，返回修改后的结果
                    if (isModified)
                    {
                        return new MessageAppendedResult
                        {
                            IsModified = true,
                            ModifiedMessage = fullMessage
                        };
                    }

                    return new MessageAppendedResult();
                }
                catch (Exception ex)
                {
                    Logger?.Error(Id, "MessageAppendedHandler 处理异常", ex);
                    return new MessageAppendedResult();
                }
            });

            // ═══════════════════════════════════════════════════════════════════════════
            // 拦截器 4：LLM 请求前拦截器（PreLLMRequestHandler）
            // 【触发时机】：构建 LLM 请求 JSON 之后，发送给 LLM API 之前
            // 【适用场景】：
            // 1. 修改请求参数（如 temperature、max_tokens）
            // 2. 动态添加/修改系统提示词
            // 3. 修改用户消息内容
            // 4. 拦截请求（直接返回自定义响应，不调用 LLM）
            // 5. 记录/分析请求日志
            // ═══════════════════════════════════════════════════════════════════════════
            Api.RegisterPreLLMRequestHandler(ctx =>
            {
                // ctx（请求上下文）：包含请求相关信息
                // RequestJson：发送给 LLM 的请求 JSON 字符串（可修改）
                // RequestId：请求唯一标识
                // ContextMessages：当前对话上下文消息列表
                // UserMessage：用户输入的原始消息

                try
                {
                    // 判空检查
                    if (string.IsNullOrWhiteSpace(ctx.RequestJson))
                        return new PreLLMRequestResult();

                    // 解析请求 JSON
                    JObject request = JObject.Parse(ctx.RequestJson);
                    bool isModified = false;

                    // ─────────────────────────────────────────────────────────────
                    // 处理 1：动态修改 temperature（示例：根据时间调整）
                    // 【说明】：晚上降低 temperature，让回复更稳定
                    // ─────────────────────────────────────────────────────────────
                    int hour = DateTime.Now.Hour;
                    if (hour >= 22 || hour < 6)
                    {
                        request["temperature"] = 0.5;  // 晚上降低随机性
                        isModified = true;
                        Logger?.Info(Id, "🌙 夜间模式：降低 temperature 至 0.5");
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 2：动态添加系统提示词（示例：添加当前时间）
                    // 【说明】：在系统提示词后追加当前时间信息
                    // ─────────────────────────────────────────────────────────────
                    if (request["messages"] is JArray messages)
                    {
                        // 查找系统消息
                        var systemMsg = messages.FirstOrDefault(m => m["role"]?.ToString() == "system");
                        if (systemMsg != null)
                        {
                            string originalContent = systemMsg["content"]?.ToString() ?? "";
                            string timeInfo = $"\n\n[系统时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
                            
                            // 避免重复添加时间戳
                            if (!originalContent.Contains("[系统时间："))
                            {
                                systemMsg["content"] = originalContent + timeInfo;
                                isModified = true;
                                Logger?.Info(Id, "⏰ 已添加系统时间到提示词");
                            }
                        }
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 3：根据用户消息内容调整 max_tokens（示例）
                    // 【说明】：简单问题减少 token，复杂问题增加 token
                    // ─────────────────────────────────────────────────────────────
                    if (!string.IsNullOrEmpty(ctx.UserMessage))
                    {
                        // 简单问题判断（长度短且没有问号）
                        if (ctx.UserMessage.Length < 10 && !ctx.UserMessage.Contains("？") && !ctx.UserMessage.Contains("?"))
                        {
                            request["max_tokens"] = 512;  // 简单问题减少 token
                            isModified = true;
                            Logger?.Info(Id, "📏 简单问题：减少 max_tokens 至 512");
                        }
                        // 复杂问题判断（包含多个问号或长度很长）
                        else if (ctx.UserMessage.Count(c => c == '？' || c == '?') > 2 || ctx.UserMessage.Length > 200)
                        {
                            request["max_tokens"] = 2048;  // 复杂问题增加 token
                            isModified = true;
                            Logger?.Info(Id, "📏 复杂问题：增加 max_tokens 至 2048");
                        }
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 4：拦截特定请求（示例：测试模式）
                    // 【说明】：如果用户消息包含特定关键词，直接返回测试响应
                    // 【注意】：返回的是 AI 回复内容（JSON格式），不是完整的 OpenAI 响应格式
                    // ─────────────────────────────────────────────────────────────
                    if (ctx.UserMessage?.Contains("!测试") == true)
                    {
                        Logger?.Info(Id, "🧪 测试模式：拦截 LLM 请求");
                        
                        // 构造测试响应（AI 回复的 JSON 格式，与 LLM 返回的 content 字段格式一致）
                        var testReply = new
                        {
                            messages = new[]
                            {
                                new { type = "text", content = "🧪 这是测试响应！插件已成功拦截 LLM 请求。" }
                            }
                        };
                        string testResponse = JsonConvert.SerializeObject(testReply);
                        
                        return new PreLLMRequestResult
                        {
                            IsIntercepted = true,
                            InterceptedResponse = testResponse
                        };
                    }

                    // ─────────────────────────────────────────────────────────────
                    // 处理 5：记录请求日志（示例）
                    // 【说明】：将请求信息记录到日志，用于调试分析
                    // ─────────────────────────────────────────────────────────────
                    Logger?.Debug(Id, $"📤 LLM请求：消息数={ctx.ContextMessages?.Count ?? 0}, 长度={ctx.RequestJson.Length}");

                    // 如果请求被修改，返回修改后的结果
                    if (isModified)
                    {
                        string modifiedJson = request.ToString(Formatting.None);
                        Logger?.Info(Id, "✏️ LLM 请求已被插件修改");
                        
                        return new PreLLMRequestResult
                        {
                            IsModified = true,
                            ModifiedRequestJson = modifiedJson
                        };
                    }

                    // 返回空结果：框架继续发送原始请求
                    return new PreLLMRequestResult();
                }
                catch (Exception ex)
                {
                    Logger?.Error(Id, "PreLLMRequestHandler 处理异常", ex);
                    return new PreLLMRequestResult();
                }
            });

            // ═══════════════════════════════════════════════════════════════════════════
            // 拦截器 5：AI 回复拦截器（LLMResponseHandler）
            // 【触发时机】：AI 生成回复内容之后，返回给用户之前
            // 【适用场景】：
            // 1. 修改AI回复内容（如添加前缀、过滤敏感信息）
            // 2. 限制回复属性（如延迟、长度）
            // 3. 拦截AI回复（替换为自定义内容）
            // 4. 统计AI回复数据（如处理次数、内容长度）
            // ═══════════════════════════════════════════════════════════════════════════
            Api.RegisterLLMResponseHandler(ctx =>
            {
                // RawResponse：AI原始回复内容（通常为JSON格式）
                // RequestId：请求ID

                // 安全检查：AI回复内容为空时直接返回
                if (string.IsNullOrWhiteSpace(ctx.RawResponse))
                    return new LLMResponseResult();

                try
                {
                    // 解析AI回复的JSON内容（框架返回的AI回复通常为JSON格式）
                    JObject json = JObject.Parse(ctx.RawResponse);
                    bool modified = false; // 标记是否修改了AI回复

                    // 定位到AI回复的消息数组（messages 是框架约定的字段名）
                    if (json["messages"] is JArray messages)
                    {
                        // 遍历每条消息进行处理
                        foreach (JToken msg in messages)
                        {
                            // ─────────────────────────────────────────────────────
                            // 处理 1：为AI回复添加前缀（根据配置开关控制）
                            // ─────────────────────────────────────────────────────
                            bool enablePrefix = GetConfig(CFG_ENABLE_PREFIX, true);
                            if (enablePrefix && msg["content"] != null)
                            {
                                string prefixText = GetConfig(CFG_PREFIX_TEXT, "[AI增强]");
                                string originalContent = msg["content"].Value<string>();

                                // 避免重复添加前缀
                                if (!originalContent.StartsWith(prefixText))
                                {
                                    msg["content"] = prefixText + " " + originalContent;
                                    modified = true;
                                }
                            }

                            // ─────────────────────────────────────────────────────
                            // 处理 2：限制回复最大延迟（防止延迟过高影响体验）
                            // ─────────────────────────────────────────────────────
                            if (msg["delay_ms"] != null)
                            {
                                int maxDelay = GetConfig(CFG_MAX_DELAY, 5000);  // 读取配置的最大延迟
                                int currDelay = msg["delay_ms"].Value<int>();   // 获取当前延迟值

                                // 如果当前延迟超过最大值，强制修改为最大值
                                if (currDelay > maxDelay)
                                {
                                    msg["delay_ms"] = maxDelay;
                                    modified = true;
                                    Logger?.Info(Id, $"⏱️ 延迟调整：{currDelay}ms → {maxDelay}ms");
                                }
                            }

                            // ─────────────────────────────────────────────────────
                            // 处理 3：表情处理（示例）
                            // ─────────────────────────────────────────────────────
                            bool enableEmoji = GetConfig(CFG_ENABLE_EMOJI, true);
                            if (enableEmoji && msg["content"] != null)
                            {
                                string content = msg["content"].Value<string>();
                                // 示例：在内容末尾添加随机表情（实际逻辑根据需求实现）
                                // content += " 😊";
                                // msg["content"] = content;
                                // modified = true;
                            }

                            // ─────────────────────────────────────────────────────
                            // 处理 4：内容长度限制（截断超长回复）
                            // ─────────────────────────────────────────────────────
                            int maxContentLength = GetConfig(CFG_MAX_CONTENT_LENGTH, 1000);
                            if (msg["content"] != null)
                            {
                                string content = msg["content"].Value<string>();
                                if (content.Length > maxContentLength)
                                {
                                    msg["content"] = content.Substring(0, maxContentLength) + "...[内容已截断]";
                                    modified = true;
                                    Logger?.Info(Id, $"✂️ 回复内容已截断：{content.Length} → {maxContentLength}");
                                }
                            }
                        }

                        // ─────────────────────────────────────────────────────
                        // 处理 5：检查是否需要拦截整个回复
                        // 【说明】：使用 IsIntercepted 拦截AI回复，替换为自定义内容
                        // 【注意】：拦截会导致大模型上下文混乱，谨慎使用
                        // ─────────────────────────────────────────────────────
                        if (GetConfig(CFG_ENABLE_BLOCK_CHECK, false))
                        {
                            var firstMsg = messages.FirstOrDefault();
                            if (firstMsg != null && firstMsg["content"] != null)
                            {
                                string content = firstMsg["content"].Value<string>();
                                // 示例：如果包含特定关键词，拦截并替换回复
                                if (content.Contains("敏感内容"))
                                {
                                    Logger?.Warning(Id, "🚫 拦截包含敏感内容的AI回复");

                                    // 构造替换的回复JSON
                                    var blockedResponse = new
                                    {
                                        messages = new[]
                                        {
                                            new { type = "text", content = "⚠️ 该回复包含不适宜内容，已被拦截。" }
                                        }
                                    };

                                    return new LLMResponseResult
                                    {
                                        IsIntercepted = true,
                                        AlternativeResponse = JsonConvert.SerializeObject(blockedResponse)
                                    };
                                }
                            }
                        }
                    }

                    // 如果AI回复被修改，返回修改后的结果
                    if (modified)
                    {
                        _processCount++; // 处理计数+1

                        // 如果启用了统计，保存数据
                        bool enableStats = GetConfig(CFG_ENABLE_STATS, true);
                        if (enableStats)
                        {
                            SaveData(DATA_FILE, new
                            {
                                ProcessCount = _processCount,
                                MessageCount = _messageCount,
                                LastProcessTime = DateTime.Now
                            });
                        }

                        return new LLMResponseResult
                        {
                            // IsIntercepted = true; // 若设置为true，拦截本次回复(不建议这样操作，会导致大模型上下文混乱)
                            IsModified = true,                                      // 标记为已修改
                            AlternativeResponse = json.ToString(Formatting.None)    // 返回修改后的JSON字符串
                        };
                    }
                }
                catch (Exception ex)
                {
                    // 捕获JSON解析/处理异常，避免影响框架主流程
                    Logger?.Error(Id, "❌ 处理AI回复时发生异常", ex);
                }

                // 未修改AI回复时，返回空结果（框架使用原始AI回复）
                return new LLMResponseResult();
            });

            Logger?.Info(Id, "✅ 所有拦截器注册完成");
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 插件核心生命周期 - 停止（OnStop）
        // 【执行时机】：插件被手动停止/框架退出时执行
        // 【核心作用】：
        // 1. 保存运行时数据（如统计计数）
        // 2. 释放资源（如关闭数据库连接、停止线程）
        // 3. 清理临时文件/缓存
        // ═══════════════════════════════════════════════════════════════════════════════
        protected override void OnStop()
        {
            Logger?.Info(Id, "═══════════════════════════════════════════════════");
            Logger?.Info(Id, $"🛑 插件 '{Name}' 正在停止...");

            // 保存统计数据到本地文件
            // SaveData()：框架封装的通用数据保存方法，自动序列化并写入文件
            SaveData(DATA_FILE, new
            {
                ProcessCount = _processCount,       // 累计处理次数
                MessageCount = _messageCount,       // 累计消息数
                LastSaveTime = DateTime.Now         // 最后保存时间（便于排查数据问题）
            });

            Logger?.Info(Id, $"💾 数据已保存：处理消息 {_messageCount} 条，修改回复 {_processCount} 次");
            Logger?.Info(Id, $"🛑 插件 '{Name}' 已停止");
            Logger?.Info(Id, "═══════════════════════════════════════════════════");
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 配置变更处理（OnConfigurationChanged）
        // 【执行时机】：前端修改配置并保存后自动触发
        // 【核心作用】：
        // 1. 感知配置变更事件
        // 2. 重新加载配置到内存变量（如果需要）
        // 3. 执行配置变更后的业务逻辑（如重新初始化、刷新状态）
        // ═══════════════════════════════════════════════════════════════════════════════
        protected override void OnConfigurationChanged()
        {
            Logger?.Info(Id, "⚙️ 配置已变更，正在重新加载...");

            // 示例：重新读取关键配置项到本地变量
            // 注意：这里只是演示，实际使用时根据业务需求决定是否缓存配置值
            bool enablePrefix = GetConfig(CFG_ENABLE_PREFIX, true);
            bool enableFilter = GetConfig(CFG_ENABLE_FILTER, true);
            int maxDelay = GetConfig(CFG_MAX_DELAY, 5000);
            string logLevel = GetConfig(CFG_LOG_LEVEL, "Info");

            Logger?.Info(Id, $"📋 当前配置：Prefix={enablePrefix}, Filter={enableFilter}, MaxDelay={maxDelay}ms, LogLevel={logLevel}");

            // 可以在这里执行配置变更后的业务逻辑
            // 例如：重新初始化某些组件、刷新缓存、更新运行时参数等

            base.OnConfigurationChanged();
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 插件公开指令（PluginCommand）
        // 【设计说明】：
        // 1. 通过 [PluginCommand] 特性标记公开方法，框架会自动注册为可调用指令
        // 2. 支持通过框架面板/API调用，实现插件功能的动态控制
        // 3. 方法参数为 Dictionary<string, object>，接收调用时传入的参数
        // 4. 返回值建议为匿名对象（便于JSON序列化返回）
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 重置统计计数器
        /// 【指令说明】：
        /// - 指令名：reset
        /// - 功能：将累计处理次数清零，并更新本地数据文件
        /// - 调用方式：框架面板→插件→执行指令→输入 reset
        /// </summary>
        [PluginCommand("reset", Description = "重置插件统计数据", Usage = "reset")]
        public object Reset(Dictionary<string, object> param)
        {
            // 1. 重置内存中的计数器
            _processCount = 0;
            _messageCount = 0;

            // 2. 重置本地文件中的数据
            SaveData(DATA_FILE, new
            {
                ProcessCount = 0,
                MessageCount = 0,
                ResetTime = DateTime.Now
            });

            // 3. 记录操作日志
            Logger?.Info(Id, "🔄 统计数据已重置");

            // 4. 返回执行结果（建议包含success和message字段，便于前端解析）
            return new
            {
                success = true,
                message = "统计数据已清零",
                data = new { processCount = 0, messageCount = 0 }
            };
        }

        /// <summary>
        /// 获取当前统计信息
        /// 【指令说明】：
        /// - 指令名：stats
        /// - 功能：获取当前插件的统计信息
        /// </summary>
        [PluginCommand("stats", Description = "获取插件统计信息", Usage = "stats")]
        public object GetStats(Dictionary<string, object> param)
        {
            TimeSpan runningTime = DateTime.Now - _startTime;

            return new
            {
                success = true,
                message = "统计信息",
                data = new
                {
                    processCount = _processCount,
                    messageCount = _messageCount,
                    runningTime = $"{runningTime.TotalMinutes:F1} 分钟",
                    startTime = _startTime.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }

        /// <summary>
        /// 获取当前配置信息
        /// 【指令说明】：
        /// - 指令名：get_config
        /// - 功能：获取当前插件的所有配置项
        /// </summary>
        [PluginCommand("get_config", Description = "获取当前配置", Usage = "get_config")]
        public object GetCurrentConfig(Dictionary<string, object> param)
        {
            return new
            {
                success = true,
                message = "当前配置",
                data = new
                {
                    enablePrefix = GetConfig(CFG_ENABLE_PREFIX, true),
                    prefixText = GetConfig(CFG_PREFIX_TEXT, "[AI增强]"),
                    maxDelay = GetConfig(CFG_MAX_DELAY, 5000),
                    enableFilter = GetConfig(CFG_ENABLE_FILTER, true),
                    sensitiveWords = GetConfig(CFG_SENSITIVE_WORDS, "脏话,广告,诈骗"),
                    maxMessageLength = GetConfig(CFG_MAX_MESSAGE_LENGTH, 2000),
                    enableStats = GetConfig(CFG_ENABLE_STATS, true),
                    logLevel = GetConfig(CFG_LOG_LEVEL, "Info"),
                    probability = GetConfig(CFG_PROBABILITY, 1.0)
                }
            };
        }

        /// <summary>
        /// 添加系统消息到上下文（不会触发前端显示，仅添加到LLM上下文）
        /// 【指令说明】：
        /// - 指令名：add_context
        /// - 参数：content=消息内容
        /// 【注意】：此方法只修改LLM上下文，不会发送消息到前端界面
        /// </summary>
        [PluginCommand("add_context", Description = "添加消息到LLM上下文", Usage = "add_context content=消息内容")]
        public object AddContextMessage(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            string content = param.ContainsKey("content") ? param["content"]?.ToString() : null;
            if (string.IsNullOrWhiteSpace(content))
                return new { success = false, message = "参数 content 不能为空" };

            // 添加系统消息到上下文（仅添加到LLM上下文，不会触发前端显示）
            Api.AddContextMessage("system", $"[插件注入] {content}");
            Logger?.Info(Id, $"💬 已添加系统消息到上下文：{content}");

            return new { success = true, message = "消息已添加到LLM上下文（不会显示在前端）" };
        }

        /// <summary>
        /// 清空上下文
        /// 【指令说明】：
        /// - 指令名：clear_context
        /// </summary>
        [PluginCommand("clear_context", Description = "清空所有上下文", Usage = "clear_context")]
        public object ClearContext(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            Api.ClearContext();
            Logger?.Info(Id, "🧹 上下文已清空");

            return new { success = true, message = "上下文已清空" };
        }

        /// <summary>
        /// 删除最后N条AI回复
        /// 【指令说明】：
        /// - 指令名：remove_last_ai
        /// - 参数：count=删除数量（默认1）
        /// </summary>
        [PluginCommand("remove_last_ai", Description = "删除最后N条AI回复", Usage = "remove_last_ai count=1")]
        public object RemoveLastAI(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            int count = 1;
            if (param.ContainsKey("count") && int.TryParse(param["count"]?.ToString(), out int c))
                count = c;

            int removed = Api.RemoveLastMessages("assistant", count);
            Logger?.Info(Id, $"🗑️ 已删除 {removed} 条AI回复");

            return new { success = true, message = $"已删除 {removed} 条AI回复" };
        }

        /// <summary>
        /// 测试发送消息
        /// 【指令说明】：
        /// - 指令名：send_test
        /// - 参数：message=消息内容
        /// </summary>
        [PluginCommand("send_test", Description = "测试发送消息", Usage = "send_test message=测试消息")]
        public async Task<object> SendTestMessage(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            string message = param.ContainsKey("message") ? param["message"]?.ToString() : "测试消息";

            try
            {
                bool result = await Api.SendMessageAsync(message);
                Logger?.Info(Id, $"📤 测试消息发送{(result ? "成功" : "失败")}：{message}");

                return new
                {
                    success = result,
                    message = result ? "消息发送成功" : "消息发送失败"
                };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "发送测试消息失败", ex);
                return new { success = false, message = $"发送失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 直接请求LLM（插件自己构建请求JSON）
        /// 【指令说明】：
        /// - 指令名：request_llm
        /// - 参数：prompt=提示词内容
        /// </summary>
        [PluginCommand("request_llm", Description = "直接请求LLM", Usage = "request_llm prompt=你好")]
        public async Task<object> RequestLLM(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            string prompt = param.ContainsKey("prompt") ? param["prompt"]?.ToString() : "你好";

            try
            {
                // 构建OpenAI格式的请求JSON
                var requestObj = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个有帮助的助手。" },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };

                string requestJson = JsonConvert.SerializeObject(requestObj);
                string response = await Api.RequestLLMAsync(requestJson);

                Logger?.Info(Id, "✅ LLM请求成功");

                return new
                {
                    success = true,
                    message = "LLM请求成功",
                    data = response
                };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "LLM请求失败", ex);
                return new { success = false, message = $"请求失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 获取软件全局配置
        /// 【指令说明】：
        /// - 指令名：get_app_config
        /// - 功能：获取AI_Chat软件的全局配置信息（包含所有配置项）
        /// </summary>
        [PluginCommand("get_app_config", Description = "获取软件全局配置", Usage = "get_app_config")]
        public object GetAppConfig(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            try
            {
                AppConfig config = Api.GetConfig();

                return new
                {
                    success = true,
                    message = "软件全局配置",
                    data = new
                    {
                        // LLM 配置
                        apiKey = config.ApiKey,
                        apiUrl = config.ApiUrl,
                        model = config.Model,
                        temperature = config.Temperature,
                        maxTokens = config.MaxTokens,
                        topP = config.TopP,

                        // WebSocket 配置
                        websocketServerUri = config.WebsocketServerUri,
                        websocketToken = config.WebsocketToken,
                        websocketKeepAliveInterval = config.WebsocketKeepAliveInterval,

                        // 功能开关
                        intentAnalysisEnabled = config.IntentAnalysisEnabled,
                        proactiveChatEnabled = config.ProactiveChatEnabled,
                        reminderEnabled = config.ReminderEnabled,

                        // 聊天配置
                        targetUserId = config.TargetUserId,
                        maxContextRounds = config.MaxContextRounds,
                        activeChatProbability = config.ActiveChatProbability,

                        // 日志配置
                        logRootFolder = config.LogRootFolder,
                        generalLogSubfolder = config.GeneralLogSubfolder,
                        contextLogSubfolder = config.ContextLogSubfolder,

                        // 提示词配置
                        baseSystemPrompt = config.BaseSystemPrompt,
                        incompleteInputPrompt = config.IncompleteInputPrompt,

                        // 角色卡配置
                        roleCardsApiUrl = config.RoleCardsApiUrl
                    }
                };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "获取软件配置失败", ex);
                return new { success = false, message = $"获取失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 获取单个配置项值
        /// 【指令说明】：
        /// - 指令名：get_config_value
        /// - 参数：key=配置项名称
        /// </summary>
        [PluginCommand("get_config_value", Description = "获取单个配置项", Usage = "get_config_value key=Temperature")]
        public object GetConfigValue(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            string key = param.ContainsKey("key") ? param["key"]?.ToString() : null;
            if (string.IsNullOrWhiteSpace(key))
                return new { success = false, message = "参数 key 不能为空" };

            try
            {
                // 示例：获取温度配置值
                if (key.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
                {
                    float temperature = Api.GetConfigValue("LlmTemperature", 0.7f);
                    return new { success = true, message = "配置项值", data = new { key, value = temperature } };
                }

                // 示例：获取最大token数
                if (key.Equals("MaxTokens", StringComparison.OrdinalIgnoreCase))
                {
                    int maxTokens = Api.GetConfigValue("LlmMaxTokens", 2000);
                    return new { success = true, message = "配置项值", data = new { key, value = maxTokens } };
                }

                return new { success = false, message = $"未知配置项：{key}" };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, $"获取配置项 {key} 失败", ex);
                return new { success = false, message = $"获取失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 获取当前插件权限列表
        /// 【指令说明】：
        /// - 指令名：get_permissions
        /// - 功能：获取当前插件已注册的所有权限
        /// </summary>
        [PluginCommand("get_permissions", Description = "获取插件权限列表", Usage = "get_permissions")]
        public object GetPermissions(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            try
            {
                List<string> permissions = Api.GetRegisteredPermissions();

                return new
                {
                    success = true,
                    message = "插件权限列表",
                    data = permissions
                };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "获取权限列表失败", ex);
                return new { success = false, message = $"获取失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 发送图片消息示例
        /// 【指令说明】：
        /// - 指令名：send_image
        /// - 参数：path=图片路径
        /// </summary>
        [PluginCommand("send_image", Description = "发送图片消息", Usage = "send_image path=图片路径")]
        public async Task<object> SendImage(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            string path = param.ContainsKey("path") ? param["path"]?.ToString() : null;
            if (string.IsNullOrWhiteSpace(path))
                return new { success = false, message = "参数 path（图片路径）不能为空" };

            try
            {
                bool result = await Api.SendMessageAsync(path, new SendMessageOptions
                {
                    MessageType = MessageType.Image
                });

                return new
                {
                    success = result,
                    message = result ? "图片发送成功" : "图片发送失败"
                };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "发送图片失败", ex);
                return new { success = false, message = $"发送失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 发送语音消息
        /// 【指令说明】：
        /// - 指令名：send_voice
        /// - 参数：path=语音文件路径（支持 .amr, .silk 等格式）
        /// 【注意】：语音文件路径可以是绝对路径，会自动添加 file:// 前缀
        /// </summary>
        [PluginCommand("send_voice", Description = "发送语音消息", Usage = "send_voice path=语音文件路径")]
        public async Task<object> SendVoice(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            string path = param.ContainsKey("path") ? param["path"]?.ToString() : null;
            if (string.IsNullOrWhiteSpace(path))
                return new { success = false, message = "参数 path（语音文件路径）不能为空" };

            try
            {
                bool result = await Api.SendMessageAsync(path, new SendMessageOptions
                {
                    MessageType = MessageType.Voice
                });

                Logger?.Info(Id, $"🎤 语音消息发送{(result ? "成功" : "失败")}：{path}");

                return new
                {
                    success = result,
                    message = result ? "语音发送成功" : "语音发送失败"
                };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "发送语音失败", ex);
                return new { success = false, message = $"发送失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 获取完整上下文并分析
        /// 【指令说明】：
        /// - 指令名：analyze_context
        /// - 功能：获取当前对话上下文并进行简单分析
        /// </summary>
        [PluginCommand("analyze_context", Description = "分析当前上下文", Usage = "analyze_context")]
        public object AnalyzeContext(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            try
            {
                List<ContextMessage> context = Api.GetFullContext();

                int userCount = context.Count(m => m.Role == "user");
                int assistantCount = context.Count(m => m.Role == "assistant");
                int systemCount = context.Count(m => m.Role == "system");

                return new
                {
                    success = true,
                    message = "上下文分析结果",
                    data = new
                    {
                        totalMessages = context.Count,
                        userMessages = userCount,
                        assistantMessages = assistantCount,
                        systemMessages = systemCount,
                        recentMessages = context.Skip(Math.Max(0, context.Count - 3)).Select(m => new { m.Role, Content = m.Content?.Substring(0, Math.Min(50, m.Content?.Length ?? 0)) + "..." }).ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "分析上下文失败", ex);
                return new { success = false, message = $"分析失败：{ex.Message}" };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 辅助方法：生成帮助文本
        // ═══════════════════════════════════════════════════════════════════════════════
        private string GetHelpText()
        {
            return @"🚀 AI_Chat 插件模板 - 帮助信息

📋 可用指令：
  !帮助 / !help     - 显示此帮助信息
  !状态 / !status   - 显示插件运行状态
  !统计 / !stats    - 显示统计信息
  !配置 / !config   - 显示当前配置

⚙️ 插件功能：
  ✅ 敏感词过滤 - 自动拦截包含违禁词的消息
  ✅ 消息长度限制 - 限制单条消息最大长度
  ✅ AI回复前缀 - 为AI回复添加自定义前缀
  ✅ 延迟限制 - 限制AI回复的最大延迟时间
  ✅ 统计功能 - 记录处理消息和修改回复次数

💡 提示：
  所有配置项均可通过前端面板进行修改";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 辅助方法：生成状态文本
        // ═══════════════════════════════════════════════════════════════════════════════
        private string GetStatusText()
        {
            TimeSpan runningTime = DateTime.Now - _startTime;

            return $"📊 插件运行状态\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"🔌 插件名称：{Name}\n" +
                   $"🔢 插件版本：{Version}\n" +
                   $"👤 插件作者：{Author}\n" +
                   $"⏱️ 运行时长：{runningTime.TotalMinutes:F1} 分钟\n" +
                   $"📅 启动时间：{_startTime:yyyy-MM-dd HH:mm:ss}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"📨 已处理消息：{_messageCount} 条\n" +
                   $"🔄 已修改回复：{_processCount} 次";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 辅助方法：生成统计文本
        // ═══════════════════════════════════════════════════════════════════════════════
        private string GetStatsText()
        {
            return $"📈 插件统计信息\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"📨 累计处理消息：{_messageCount} 条\n" +
                   $"🔄 累计修改回复：{_processCount} 次\n" +
                   $"💾 数据文件：{DATA_FILE}";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 辅助方法：生成配置文本
        // ═══════════════════════════════════════════════════════════════════════════════
        private string GetConfigText()
        {
            return $"⚙️ 当前配置\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"📌 前缀功能：{(GetConfig(CFG_ENABLE_PREFIX, true) ? "✅ 启用" : "❌ 禁用")}\n" +
                   $"📝 前缀文本：{GetConfig(CFG_PREFIX_TEXT, "[AI增强]")}\n" +
                   $"⏱️ 最大延迟：{GetConfig(CFG_MAX_DELAY, 5000)} ms\n" +
                   $"🛡️ 过滤功能：{(GetConfig(CFG_ENABLE_FILTER, true) ? "✅ 启用" : "❌ 禁用")}\n" +
                   $"📏 最大长度：{GetConfig(CFG_MAX_MESSAGE_LENGTH, 2000)} 字符\n" +
                   $"📊 统计功能：{(GetConfig(CFG_ENABLE_STATS, true) ? "✅ 启用" : "❌ 禁用")}\n" +
                   $"📝 日志级别：{GetConfig(CFG_LOG_LEVEL, "Info")}";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 插件说明文档（GetReadme）
        // 【设计说明】：
        // 1. 重写此方法提供插件的可视化说明文档
        // 2. 返回HTML字符串，支持基本的样式和排版
        // 3. 展示在框架面板的插件详情页，方便用户了解插件功能
        // ═══════════════════════════════════════════════════════════════════════════════
        public override string GetReadme()
        {
            return @"<div style='padding:15px;font-family:Segoe UI,Microsoft YaHei,sans-serif'>
                <h2 style='color:#2196F3;margin-bottom:10px'>🚀 AI_Chat 插件开发模板</h2>
                <p style='color:#666'>这是一个完整的插件开发模板，展示了AI_Chat插件系统的所有可用功能。</p>
                
                <h3 style='color:#333;margin-top:20px'>📋 核心功能</h3>
                <ul style='line-height:1.8'>
                    <li><b>敏感词过滤</b> - 在消息合并前拦截包含违禁词的内容</li>
                    <li><b>消息长度限制</b> - 限制单条消息的最大长度</li>
                    <li><b>AI回复前缀</b> - 为AI回复添加自定义前缀标识</li>
                    <li><b>延迟限制</b> - 限制AI回复的最大延迟时间</li>
                    <li><b>统计功能</b> - 记录处理消息和修改回复次数，数据持久化</li>
                    <li><b>上下文操作</b> - 支持添加/删除/清空LLM上下文消息（不触发前端显示）</li>
                </ul>
                
                <h3 style='color:#333;margin-top:20px'>⚙️ 配置项说明</h3>
                <table style='border-collapse:collapse;width:100%;margin-top:10px'>
                    <tr style='background:#f5f5f5'>
                        <th style='border:1px solid #ddd;padding:10px;text-align:left'>配置项</th>
                        <th style='border:1px solid #ddd;padding:10px;text-align:left'>类型</th>
                        <th style='border:1px solid #ddd;padding:10px;text-align:left'>默认值</th>
                        <th style='border:1px solid #ddd;padding:10px;text-align:left'>说明</th>
                    </tr>
                    <tr>
                        <td style='border:1px solid #ddd;padding:8px'>EnablePrefix</td>
                        <td style='border:1px solid #ddd;padding:8px'>布尔值</td>
                        <td style='border:1px solid #ddd;padding:8px'>true</td>
                        <td style='border:1px solid #ddd;padding:8px'>是否启用AI回复前缀</td>
                    </tr>
                    <tr style='background:#fafafa'>
                        <td style='border:1px solid #ddd;padding:8px'>PrefixText</td>
                        <td style='border:1px solid #ddd;padding:8px'>字符串</td>
                        <td style='border:1px solid #ddd;padding:8px'>[AI增强]</td>
                        <td style='border:1px solid #ddd;padding:8px'>AI回复前缀文本</td>
                    </tr>
                    <tr>
                        <td style='border:1px solid #ddd;padding:8px'>MaxDelay</td>
                        <td style='border:1px solid #ddd;padding:8px'>整数</td>
                        <td style='border:1px solid #ddd;padding:8px'>5000</td>
                        <td style='border:1px solid #ddd;padding:8px'>AI回复最大延迟（毫秒）</td>
                    </tr>
                    <tr style='background:#fafafa'>
                        <td style='border:1px solid #ddd;padding:8px'>EnableFilter</td>
                        <td style='border:1px solid #ddd;padding:8px'>布尔值</td>
                        <td style='border:1px solid #ddd;padding:8px'>true</td>
                        <td style='border:1px solid #ddd;padding:8px'>是否启用敏感词过滤</td>
                    </tr>
                    <tr>
                        <td style='border:1px solid #ddd;padding:8px'>SensitiveWords</td>
                        <td style='border:1px solid #ddd;padding:8px'>字符串</td>
                        <td style='border:1px solid #ddd;padding:8px'>脏话,广告,诈骗</td>
                        <td style='border:1px solid #ddd;padding:8px'>敏感词列表（逗号分隔）</td>
                    </tr>
                    <tr style='background:#fafafa'>
                        <td style='border:1px solid #ddd;padding:8px'>MaxMessageLength</td>
                        <td style='border:1px solid #ddd;padding:8px'>整数</td>
                        <td style='border:1px solid #ddd;padding:8px'>2000</td>
                        <td style='border:1px solid #ddd;padding:8px'>最大消息长度（字符）</td>
                    </tr>
                    <tr>
                        <td style='border:1px solid #ddd;padding:8px'>EnableStats</td>
                        <td style='border:1px solid #ddd;padding:8px'>布尔值</td>
                        <td style='border:1px solid #ddd;padding:8px'>true</td>
                        <td style='border:1px solid #ddd;padding:8px'>是否启用统计功能</td>
                    </tr>
                    <tr style='background:#fafafa'>
                        <td style='border:1px solid #ddd;padding:8px'>LogLevel</td>
                        <td style='border:1px solid #ddd;padding:8px'>字符串</td>
                        <td style='border:1px solid #ddd;padding:8px'>Info</td>
                        <td style='border:1px solid #ddd;padding:8px'>日志级别</td>
                    </tr>
                    <tr>
                        <td style='border:1px solid #ddd;padding:8px'>EnableTimestamp</td>
                        <td style='border:1px solid #ddd;padding:8px'>布尔值</td>
                        <td style='border:1px solid #ddd;padding:8px'>false</td>
                        <td style='border:1px solid #ddd;padding:8px'>是否为消息添加时间戳</td>
                    </tr>
                    <tr style='background:#fafafa'>
                        <td style='border:1px solid #ddd;padding:8px'>MaxContentLength</td>
                        <td style='border:1px solid #ddd;padding:8px'>整数</td>
                        <td style='border:1px solid #ddd;padding:8px'>1000</td>
                        <td style='border:1px solid #ddd;padding:8px'>AI回复内容最大长度</td>
                    </tr>
                </table>
                
                <h3 style='color:#333;margin-top:20px'>💬 聊天指令</h3>
                <ul style='line-height:1.8'>
                    <li><code>!帮助</code> / <code>!help</code> - 显示帮助信息</li>
                    <li><code>!状态</code> / <code>!status</code> - 显示插件运行状态</li>
                    <li><code>!统计</code> / <code>!stats</code> - 显示统计信息</li>
                    <li><code>!配置</code> / <code>!config</code> - 显示当前配置</li>
                </ul>
                
                <h3 style='color:#333;margin-top:20px'>🔧 公开指令</h3>
                <ul style='line-height:1.8'>
                    <li><b>reset</b> - 重置统计数据</li>
                    <li><b>stats</b> - 获取统计信息</li>
                    <li><b>get_config</b> - 获取当前配置</li>
                    <li><b>add_context</b> - 添加消息到上下文</li>
                    <li><b>clear_context</b> - 清空上下文</li>
                    <li><b>remove_last_ai</b> - 删除最后N条AI回复</li>
                    <li><b>send_test</b> - 测试发送消息</li>
                    <li><b>request_llm</b> - 直接请求LLM（自定义JSON）</li>
                    <li><b>get_app_config</b> - 获取软件全局配置</li>
                    <li><b>get_config_value</b> - 获取单个配置项值</li>
                    <li><b>get_permissions</b> - 获取插件权限列表</li>
                    <li><b>send_image</b> - 发送图片消息</li>
                    <li><b>send_voice</b> - 发送语音消息（支持 .amr, .silk 格式）</li>
                    <li><b>analyze_context</b> - 分析当前上下文</li>
                </ul>

                <h3 style='color:#333;margin-top:20px'>📝 插件权限</h3>
                <ul style='line-height:1.8'>
                    <li><b>消息拦截</b> - 注册PreMerge/PostMerge/MessageAppended/PreLLMRequest/LLMResponse拦截器</li>
                    <li><b>上下文操作</b> - 读取/添加/删除/清空LLM上下文</li>
                    <li><b>消息发送</b> - 发送文本/图片/语音消息</li>
                    <li><b>配置读取</b> - 读取软件全局配置和单个配置项</li>
                    <li><b>LLM请求</b> - 直接请求大模型API</li>
                    <li><b>权限查询</b> - 查询当前插件权限列表</li>
                </ul>

                <h3 style='color:#333;margin-top:20px'>📝 开发提示</h3>
                <ul style='line-height:1.8'>
                    <li>配置项会在插件首次启动时自动初始化默认值</li>
                    <li>修改配置后无需重启插件，配置变更会自动触发 OnConfigurationChanged</li>
                    <li>使用 <code>GetConfig&lt;T&gt;(key, defaultValue)</code> 读取配置</li>
                    <li>使用 <code>SetConfig(key, value)</code> + <code>SetConfiguration(config)</code> 保存配置</li>
                    <li>使用 <code>SaveData(fileName, data)</code> 和 <code>LoadData&lt;T&gt;(fileName)</code> 进行数据持久化</li>
                </ul>
            </div>";
        }

        /// <summary>
        /// 获取插件权限信息（重写以声明额外权限）
        /// </summary>
        public override PluginPermissionsInfo GetPermissionsInfo()
        {
            var info = base.GetPermissionsInfo();

            // 声明插件自述的额外权限
            info.DeclaredPermissions.Add("消息拦截 - 注册PreMerge/PostMerge/MessageAppended/PreLLMRequest/LLMResponse拦截器");
            info.DeclaredPermissions.Add("上下文操作 - 读取/添加/删除/清空LLM上下文");
            info.DeclaredPermissions.Add("消息发送 - 发送文本/图片/语音消息");
            info.DeclaredPermissions.Add("配置读取 - 读取软件全局配置和单个配置项");
            info.DeclaredPermissions.Add("LLM请求 - 直接请求大模型API");
            info.DeclaredPermissions.Add("权限查询 - 查询当前插件权限列表");

            return info;
        }
    }
}
