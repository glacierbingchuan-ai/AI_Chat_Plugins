using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AI_Chat.Plugins;//请引用AI_Chat.exe
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AI_Chat.Plugin.Reinforcement
{
    /// <summary>
    /// 动态提示词强化插件
    /// 核心功能：每隔N轮对话自动插入提示词强化，防止AI遗忘核心设定
    /// </summary>
    [Plugin(
        Id = "ReinforcementPlugin",
        Name = "动态提示词强化",
        Version = "1.0.1",
        Author = "Glacier",
        Description = "(新规范)每隔设定轮数自动插入提示词强化，防止AI遗忘核心设定，支持自定义强化内容和间隔",
        AutoStart = true,
        Priority = 20,
        SupportSandbox = true                    // ✅ 启用沙箱保护，确保文件操作安全隔离
    )]
    public class ReinforcementPlugin : PluginBase
    {
        // ==============================================
        // 插件运行时状态
        // ==============================================
        /// <summary>
        /// 已注入提示词强化的次数（用于统计）
        /// </summary>
        private int _injectCount = 0;

        /// <summary>
        /// 数据存储文件名（保存统计次数）
        /// </summary>
        private const string DATA_FILE = "reinforcement_data.json";

        /// <summary>
        /// 标记已注入的提示词（用于避免重复注入）
        /// </summary>
        private const string TAG_REINFORCEMENT = "[Instruction Reinforcement]";

        // ==============================================
        // 核心配置项
        // ==============================================
        /// <summary>
        /// 是否启用提示词强化（布尔值）
        /// </summary>
        private const string CFG_ENABLE_REINFORCEMENT = "EnableReinforcement";

        /// <summary>
        /// 每隔多少轮用户消息注入一次提示词（整数）
        /// </summary>
        private const string CFG_INTERVAL = "ReinforcementInterval";

        /// <summary>
        /// 提示词强化内容（字符串）
        /// </summary>
        private const string CFG_PROMPT = "ReinforcementPrompt";

        // ==============================================
        // 默认提示词强化内容
        // ==============================================
        private const string DEFAULT_PROMPT = @"核心记忆强化：
1. 你是18岁高中生刘舒颜，语气要俏皮、撒娇或犯贱。
2. 必须严格遵守JSON回复格式。
3. 文字与表情包必须完全分离，分别放在messages数组的不同对象里。
4. 每句话建议在5字以内，严禁长篇大论。
5. 仅聊河南高中生生活相关话题。
6. 禁止使用[MEME_MSG]发送表情包，必须与文字分别放在messages数组的不同对象里。";

        // ==============================================
        // 插件初始化
        // ==============================================
        protected override void OnInitialize()
        {
            Logger?.Info(Id, "动态提示词强化插件初始化开始");

            try
            {
                // ========================================
                // 使用 Data 帮助类初始化配置
                // ========================================
                // Data 帮助类提供沙箱安全的配置和数据操作
                // 配置文件自动保存在：PluginConfigs/{插件ID}.json
                // 数据文件自动保存在：PluginData/{插件ID}/

                // 检查并初始化默认配置
                bool configChanged = false;

                // 1. 是否启用提示词强化（默认启用）
                if (!Data.Has(CFG_ENABLE_REINFORCEMENT))
                {
                    Data.Set(CFG_ENABLE_REINFORCEMENT, true);
                    configChanged = true;
                }

                // 2. 注入间隔（默认每3轮用户消息注入一次）
                if (!Data.Has(CFG_INTERVAL))
                {
                    Data.Set(CFG_INTERVAL, 3);
                    configChanged = true;
                }

                // 3. 提示词强化内容（使用默认内容）
                if (!Data.Has(CFG_PROMPT))
                {
                    Data.Set(CFG_PROMPT, DEFAULT_PROMPT);
                    configChanged = true;
                }

                // 保存新配置
                if (configChanged)
                {
                    Data.SaveConfig();
                    Logger?.Info(Id, "默认配置已初始化并保存");
                }

                // ========================================
                // 使用 Data 帮助类加载历史统计数据
                // ========================================
                var data = Data.LoadJson<dynamic>(DATA_FILE);
                if (data != null && data.InjectCount != null)
                {
                    _injectCount = (int)data.InjectCount;
                    Logger?.Info(Id, $"历史数据加载完成，累计注入提示词 {_injectCount} 次");
                }
                else
                {
                    Logger?.Info(Id, "无历史数据，注入次数归零");
                }

                Logger?.Info(Id, "插件初始化完成");
            }
            catch (Exception ex)
            {
                Logger?.Error(Id, "插件初始化异常", ex);
            }
        }

        // ==============================================
        // 插件启动（注册拦截器）
        // ==============================================
        protected override void OnStart()
        {
            Logger?.Info(Id, "动态提示词强化插件已启动");

            if (Api == null)
            {
                Logger?.Error(Id, "API 实例未初始化，无法注册拦截器");
                return;
            }

            // ------------------------------------------------------
            // 拦截器1：消息合并前拦截器（处理统计查询指令）
            // ------------------------------------------------------
            Api.RegisterPreMergeMessageHandler(ctx =>
            {
                if (string.IsNullOrWhiteSpace(ctx.RawMessage))
                {
                    return new PreMergeMessageResult();
                }

                // 识别统计指令
                string rawMsg = ctx.RawMessage.Trim().ToLower();
                if (rawMsg == "!强化统计" || rawMsg == "!提示词统计")
                {
                    // 使用 Data 帮助类读取配置
                    bool enableReinforcement = Data.Get(CFG_ENABLE_REINFORCEMENT, true);
                    int interval = Data.Get(CFG_INTERVAL, 3);
                    string prompt = Data.Get(CFG_PROMPT, DEFAULT_PROMPT);
                    string preview = prompt.Length > 50 ? prompt.Substring(0, 50) + "..." : prompt;

                    string response = $"📊 动态提示词强化统计\r\n" +
                                     $"✅ 功能启用状态：{(enableReinforcement ? "已启用" : "已禁用")}\r\n" +
                                     $"🔄 注入间隔：每 {interval} 轮用户消息\r\n" +
                                     $"🔢 累计注入次数：{_injectCount} 次\r\n" +
                                     $"📝 当前提示词预览：{preview}";

                    Logger?.Info(Id, $"用户查询强化统计，当前注入次数：{_injectCount}");

                    return new PreMergeMessageResult
                    {
                        IsIntercepted = true,
                        Response = response
                    };
                }

                return new PreMergeMessageResult();
            });

            // ------------------------------------------------------
            // 拦截器2：合并后消息拦截器（核心逻辑：注入提示词强化）
            // ------------------------------------------------------
            Api.RegisterPostMergeMessageHandler(ctx =>
            {
                // 检查是否启用
                bool enableReinforcement = Data.Get(CFG_ENABLE_REINFORCEMENT, true);
                if (!enableReinforcement)
                {
                    return new PostMergeMessageResult();
                }

                try
                {
                    // 获取当前完整上下文
                    var context = Api.GetFullContext(ctx.UserId);
                    if (context == null || context.Count == 0)
                    {
                        return new PostMergeMessageResult();
                    }

                    // 统计用户消息数量（排除系统消息和已注入的强化消息）
                    int userMessageCount = 0;
                    int lastUserMessageIndex = -1;

                    for (int i = 0; i < context.Count; i++)
                    {
                        var msg = context[i];
                        if (msg.Role == "user")
                        {
                            userMessageCount++;
                            lastUserMessageIndex = i;
                        }
                    }

                    // 检查是否达到注入间隔
                    int interval = Data.Get(CFG_INTERVAL, 3);
                    if (userMessageCount == 0 || userMessageCount % interval != 0)
                    {
                        return new PostMergeMessageResult();
                    }

                    // 检查是否已注入（避免重复注入）
                    if (lastUserMessageIndex > 0)
                    {
                        var prevMsg = context[lastUserMessageIndex - 1];
                        if (prevMsg.Content != null && prevMsg.Content.Contains(TAG_REINFORCEMENT))
                        {
                            return new PostMergeMessageResult();
                        }
                    }

                    // 获取提示词内容
                    string prompt = Data.Get(CFG_PROMPT, DEFAULT_PROMPT);
                    string fullPrompt = TAG_REINFORCEMENT + " " + prompt;

                    // 使用API添加上下文消息（触发前端显示）
                    Api.AddContextMessage(ctx.UserId, "system", fullPrompt);

                    _injectCount++;
                    Logger?.Info(Id, $"第 {userMessageCount} 轮用户消息，已注入提示词强化，累计 {_injectCount} 次");

                    // ========================================
                    // 使用 Data 帮助类保存统计数据
                    // ========================================
                    Data.SaveJson(DATA_FILE, new
                    {
                        InjectCount = _injectCount,
                        LastInjectTime = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    Logger?.Error(Id, "处理提示词强化时发生异常", ex);
                }

                return new PostMergeMessageResult();
            });
        }

        // ==============================================
        // 插件停止（保存统计数据）
        // ==============================================
        protected override void OnStop()
        {
            // ========================================
            // 使用 Data 帮助类保存统计数据
            // ========================================
            Data.SaveJson(DATA_FILE, new
            {
                InjectCount = _injectCount,
                LastSaveTime = DateTime.Now
            });

            // Data.SaveConfig() 会在基类中自动调用
            Logger?.Info(Id, $"插件已停止，累计注入提示词 {_injectCount} 次，数据已保存");
        }

        // ==============================================
        // 配置变更处理
        // ==============================================
        protected override void OnConfigurationChanged()
        {
            Logger?.Info(Id, "配置已变更，重新加载核心配置");

            // 使用 Data 帮助类读取配置
            bool enableReinforcement = Data.Get(CFG_ENABLE_REINFORCEMENT, true);
            int interval = Data.Get(CFG_INTERVAL, 3);
            string prompt = Data.Get(CFG_PROMPT, DEFAULT_PROMPT);
            string preview = prompt.Length > 30 ? prompt.Substring(0, 30) + "..." : prompt;

            Logger?.Info(Id, $"当前配置：启用={enableReinforcement}，间隔={interval}轮，提示词={preview}");

            base.OnConfigurationChanged();
        }

        // ==============================================
        // 公开指令
        // ==============================================

        /// <summary>
        /// 重置注入次数统计
        /// </summary>
        [PluginCommand("reset", Description = "重置提示词注入次数统计")]
        public object Reset(Dictionary<string, object> param)
        {
            _injectCount = 0;
            
            // 使用 Data 帮助类保存
            Data.SaveJson(DATA_FILE, new { InjectCount = 0 });
            Logger?.Info(Id, "提示词注入次数已重置");

            return new { success = true, message = "提示词注入次数已清零" };
        }

        /// <summary>
        /// 手动触发一次提示词注入
        /// </summary>
        [PluginCommand("inject", Description = "手动触发提示词强化注入")]
        public object InjectNow(Dictionary<string, object> param)
        {
            if (Api == null)
                return new { success = false, message = "API 未初始化" };

            // 使用 Data 帮助类读取配置
            string prompt = Data.Get(CFG_PROMPT, DEFAULT_PROMPT);
            string fullPrompt = TAG_REINFORCEMENT + " " + prompt;

            // 需要指定用户ID，这里从参数获取或使用默认值
            long userId = 0;
            if (param.ContainsKey("userId"))
            {
                userId = Convert.ToInt64(param["userId"]);
            }

            if (userId == 0)
            {
                return new { success = false, message = "需要提供 userId 参数" };
            }

            Api.AddContextMessage(userId, "system", fullPrompt);
            _injectCount++;

            Logger?.Info(Id, $"手动触发提示词注入，累计 {_injectCount} 次");
            return new { success = true, message = "已手动注入提示词强化" };
        }

        /// <summary>
        /// 测试提示词内容
        /// </summary>
        [PluginCommand("test", Description = "测试提示词内容")]
        public object TestPrompt(Dictionary<string, object> param)
        {
            // 使用 Data 帮助类读取配置
            string prompt = Data.Get(CFG_PROMPT, DEFAULT_PROMPT);
            string fullPrompt = TAG_REINFORCEMENT + " " + prompt;

            return new
            {
                success = true,
                message = "当前提示词内容",
                data = fullPrompt
            };
        }

        // ==============================================
        // 插件说明文档
        // ==============================================
        public override string GetReadme()
        {
            return "<div style='padding:10px'>" +
                   "<h3>🚀 动态提示词强化插件</h3>" +
                   "<p>每隔设定轮数自动插入提示词强化，防止AI遗忘核心设定</p>" +
                   "<h4>📋 核心功能：</h4>" +
                   "<ul>" +
                   "<li>✅ 可开关的提示词强化功能</li>" +
                   "<li>✅ 自定义注入间隔（每隔N轮用户消息）</li>" +
                   "<li>✅ 自定义提示词强化内容</li>" +
                   "<li>✅ 自动避免重复注入</li>" +
                   "<li>✅ 统计注入次数，重启不丢失</li>" +
                   "<li>✅ 支持手动触发注入</li>" +
                   "<li>✅ 沙箱安全运行（SupportSandbox=true）</li>" +
                   "</ul>" +
                   "<h4>⚙️ 配置项说明：</h4>" +
                   "<table style='border-collapse:collapse;width:100%'>" +
                   "<tr style='background:#f0f0f0'><th style='border:1px solid #ccc;padding:8px'>配置项</th><th style='border:1px solid #ccc;padding:8px'>类型</th><th style='border:1px solid #ccc;padding:8px'>默认值</th><th style='border:1px solid #ccc;padding:8px'>说明</th></tr>" +
                   "<tr><td style='border:1px solid #ccc;padding:8px'>EnableReinforcement</td><td style='border:1px solid #ccc;padding:8px'>布尔值</td><td style='border:1px solid #ccc;padding:8px'>true</td><td style='border:1px solid #ccc;padding:8px'>是否启用提示词强化</td></tr>" +
                   "<tr><td style='border:1px solid #ccc;padding:8px'>ReinforcementInterval</td><td style='border:1px solid #ccc;padding:8px'>整数</td><td style='border:1px solid #ccc;padding:8px'>3</td><td style='border:1px solid #ccc;padding:8px'>每隔N轮用户消息注入一次</td></tr>" +
                   "<tr><td style='border:1px solid #ccc;padding:8px'>ReinforcementPrompt</td><td style='border:1px solid #ccc;padding:8px'>字符串</td><td style='border:1px solid #ccc;padding:8px'>（见代码）</td><td style='border:1px solid #ccc;padding:8px'>提示词强化内容</td></tr>" +
                   "</table>" +
                   "<h4>💡 快捷指令：</h4>" +
                   "<ul>" +
                   "<li>!强化统计 / !提示词统计：查看当前配置和累计注入次数</li>" +
                   "<li>inject：手动触发一次提示词注入（需要userId参数）</li>" +
                   "<li>test：查看当前提示词内容</li>" +
                   "</ul>" +
                   "<h4>🛡️ 安全特性：</h4>" +
                   "<ul>" +
                   "<li>✅ 沙箱隔离运行，数据安全有保障</li>" +
                   "<li>✅ 使用 Data 帮助类进行文件操作</li>" +
                   "<li>✅ 配置文件和数据自动隔离存储</li>" +
                   "</ul>" +
                   "<h4>📝 工作原理：</h4>" +
                   "<p>插件会在每次用户发送消息后检查：如果用户消息数量达到设定的间隔值（如每3轮），" +
                   "且上一轮不是提示词强化消息，则自动在上下文前插入一条系统消息（提示词强化）。" +
                   "这样可以定期提醒AI遵守核心设定，防止长对话后遗忘角色设定。</p>" +
                   "</div>";
        }
    }
}
