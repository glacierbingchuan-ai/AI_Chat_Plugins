using System;
using System.Collections.Generic;
using System.IO;
using AI_Chat.Plugins;// 请引用 AI_Chat.exe
using Newtonsoft.Json;

namespace AI_Chat.Plugin.Timestamp
{
    /// <summary>
    /// AI时间观念插件
    /// 核心功能：为每条用户消息添加时间戳，让AI具备时间感知能力
    /// </summary>
    [Plugin(
        Id = "TimestampPlugin",
        Name = "AI时间观念",
        Version = "1.0.0",
        Author = "Glacier",
        Description = "为每条用户消息添加时间戳，让AI具备时间感知能力，支持自定义时间格式",
        AutoStart = true,
        Priority = 5
    )]
    public class TimestampPlugin : PluginBase
    {
        // ==============================================
        // 插件元数据重写（必须实现）
        // ==============================================
        public override string Id => "TimestampPlugin";
        public override string Name => "AI时间观念";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "Glacier";
        public override string Description => "为每条用户消息添加时间戳，让AI具备时间感知能力，支持自定义时间格式";

        // ==============================================
        // 插件运行时状态
        // ==============================================
        /// <summary>
        /// 已添加时间戳的消息数量（用于统计）
        /// </summary>
        private int _timestampCount = 0;

        /// <summary>
        /// 数据存储文件名（保存统计次数）
        /// </summary>
        private const string DATA_FILE = "timestamp_data.json";

        // ==============================================
        // 核心配置项
        // ==============================================
        /// <summary>
        /// 是否启用时间戳（布尔值）
        /// </summary>
        private const string CFG_ENABLE_TIMESTAMP = "EnableTimestamp";

        /// <summary>
        /// 时间格式字符串（字符串，遵循C# DateTime格式）
        /// </summary>
        private const string CFG_TIME_FORMAT = "TimeFormat";

        /// <summary>
        /// 默认时间格式
        /// </summary>
        private const string DEFAULT_TIME_FORMAT = "[yyyy-MM-dd HH:mm:ss] ";

        // ==============================================
        // 插件初始化
        // ==============================================
        protected override void OnInitialize()
        {
            Logger?.Info(Id, "AI时间观念插件初始化开始");

            try
            {
                // 初始化配置
                Dictionary<string, object> config = GetConfiguration();
                bool configChanged = false;

                // 1. 是否启用时间戳（默认启用）
                if (!config.ContainsKey(CFG_ENABLE_TIMESTAMP))
                {
                    config[CFG_ENABLE_TIMESTAMP] = true;
                    configChanged = true;
                }

                // 2. 时间格式（使用默认格式）
                if (!config.ContainsKey(CFG_TIME_FORMAT))
                {
                    config[CFG_TIME_FORMAT] = DEFAULT_TIME_FORMAT;
                    configChanged = true;
                }

                // 保存新配置
                if (configChanged)
                {
                    SetConfiguration(config);
                    Logger?.Info(Id, "默认配置已初始化并保存");
                }

                // 加载历史统计数据
                dynamic data = LoadData<dynamic>(DATA_FILE);
                if (data != null && data.TimestampCount != null)
                {
                    _timestampCount = data.TimestampCount;
                    Logger?.Info(Id, $"历史数据加载完成，累计添加时间戳 {_timestampCount} 次");
                }
                else
                {
                    Logger?.Info(Id, "无历史数据，计数器归零");
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
            Logger?.Info(Id, "AI时间观念插件已启动");

            if (Api == null)
            {
                Logger?.Error(Id, "API 实例未初始化，无法注册拦截器");
                return;
            }

            // ------------------------------------------------------
            // 拦截器1：消息合并后拦截器（添加时间戳到新消息）
            // ------------------------------------------------------
            Api.RegisterPostMergeMessageHandler(ctx =>
            {
                // 检查是否启用
                bool enableTimestamp = GetConfig(CFG_ENABLE_TIMESTAMP, true);
                if (!enableTimestamp)
                {
                    return new PostMergeMessageResult();
                }

                string fullMessage = ctx.FullMessage;
                string timeFormat = GetConfig(CFG_TIME_FORMAT, DEFAULT_TIME_FORMAT);
                
                // 移除消息中所有已存在的时间戳（格式：[yyyy-MM-dd HH:mm:ss]）
                System.Text.RegularExpressions.Regex timestampRegex = 
                    new System.Text.RegularExpressions.Regex(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] ");
                fullMessage = timestampRegex.Replace(fullMessage, "");

                // 生成新的时间戳并添加到消息开头
                string timestamp = DateTime.Now.ToString(timeFormat);
                string modifiedMessage = timestamp + fullMessage;

                _timestampCount++;
                Logger?.Info(Id, $"已添加时间戳：{timestamp}，累计 {_timestampCount} 次");

                // 保存统计数据
                SaveData(DATA_FILE, new
                {
                    TimestampCount = _timestampCount,
                    LastTimestampTime = DateTime.Now
                });

                return new PostMergeMessageResult
                {
                    IsModified = true,
                    ModifiedMessage = modifiedMessage
                };
            });

            // ------------------------------------------------------
            // 拦截器2：消息追加完成拦截器（处理追加后的消息）
            // 当消息被追加到上一条用户消息时，重新处理整个消息
            // ------------------------------------------------------
            Api.RegisterMessageAppendedHandler(ctx =>
            {
                // 检查是否启用
                bool enableTimestamp = GetConfig(CFG_ENABLE_TIMESTAMP, true);
                if (!enableTimestamp)
                {
                    return new MessageAppendedResult();
                }

                string fullMessage = ctx.FullMessage;
                string timeFormat = GetConfig(CFG_TIME_FORMAT, DEFAULT_TIME_FORMAT);
                
                // 移除消息中所有已存在的时间戳
                System.Text.RegularExpressions.Regex timestampRegex = 
                    new System.Text.RegularExpressions.Regex(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] ");
                fullMessage = timestampRegex.Replace(fullMessage, "");

                // 生成新的时间戳并添加到消息开头
                string timestamp = DateTime.Now.ToString(timeFormat);
                string modifiedMessage = timestamp + fullMessage;

                _timestampCount++;
                Logger?.Info(Id, $"追加消息已重新添加时间戳：{timestamp}，累计 {_timestampCount} 次");

                // 保存统计数据
                SaveData(DATA_FILE, new
                {
                    TimestampCount = _timestampCount,
                    LastTimestampTime = DateTime.Now
                });

                return new MessageAppendedResult
                {
                    IsModified = true,
                    ModifiedMessage = modifiedMessage
                };
            });
        }

        // ==============================================
        // 插件停止（保存统计数据）
        // ==============================================
        protected override void OnStop()
        {
            SaveData(DATA_FILE, new
            {
                TimestampCount = _timestampCount,
                LastSaveTime = DateTime.Now
            });

            Logger?.Info(Id, $"插件已停止，累计添加时间戳 {_timestampCount} 次，数据已保存");
        }

        // ==============================================
        // 配置变更处理
        // ==============================================
        protected override void OnConfigurationChanged()
        {
            Logger?.Info(Id, "配置已变更，重新加载核心配置");

            bool enableTimestamp = GetConfig(CFG_ENABLE_TIMESTAMP, true);
            string timeFormat = GetConfig(CFG_TIME_FORMAT, DEFAULT_TIME_FORMAT);

            Logger?.Info(Id, $"当前配置：启用={enableTimestamp}，格式={timeFormat}");

            base.OnConfigurationChanged();
        }

        // ==============================================
        // 公开指令
        // ==============================================

        /// <summary>
        /// 重置时间戳统计
        /// </summary>
        [PluginCommand("reset", Description = "重置时间戳添加次数统计")]
        public object Reset(Dictionary<string, object> param)
        {
            _timestampCount = 0;
            SaveData(DATA_FILE, new { TimestampCount = 0 });
            Logger?.Info(Id, "时间戳统计已重置");

            return new { success = true, message = "时间戳统计已清零" };
        }

        /// <summary>
        /// 查看当前时间格式示例
        /// </summary>
        [PluginCommand("test", Description = "查看当前时间格式示例")]
        public object TestFormat(Dictionary<string, object> param)
        {
            string timeFormat = GetConfig(CFG_TIME_FORMAT, DEFAULT_TIME_FORMAT);
            string example = DateTime.Now.ToString(timeFormat);

            return new
            {
                success = true,
                message = "当前时间格式示例",
                data = new
                {
                    format = timeFormat,
                    example = example + "用户消息内容"
                }
            };
        }

        // ==============================================
        // 插件说明文档
        // ==============================================
        public override string GetReadme()
        {
            return "<div style='padding:10px'>" +
                   "<h3>🕐 AI时间观念插件</h3>" +
                   "<p>为每条用户消息添加时间戳，让AI具备时间感知能力</p>" +
                   "<h4>📋 核心功能：</h4>" +
                   "<ul>" +
                   "<li>✅ 可开关的时间戳功能</li>" +
                   "<li>✅ 自定义时间格式（支持C# DateTime格式）</li>" +
                   "<li>✅ 统计添加次数，重启不丢失</li>" +
                   "<li>✅ 高优先级处理（Priority=5）</li>" +
                   "</ul>" +
                   "<h4>⚙️ 配置项说明：</h4>" +
                   "<table style='border-collapse:collapse;width:100%'>" +
                   "<tr style='background:#f0f0f0'><th style='border:1px solid #ccc;padding:8px'>配置项</th><th style='border:1px solid #ccc;padding:8px'>类型</th><th style='border:1px solid #ccc;padding:8px'>默认值</th><th style='border:1px solid #ccc;padding:8px'>说明</th></tr>" +
                   "<tr><td style='border:1px solid #ccc;padding:8px'>EnableTimestamp</td><td style='border:1px solid #ccc;padding:8px'>布尔值</td><td style='border:1px solid #ccc;padding:8px'>true</td><td style='border:1px solid #ccc;padding:8px'>是否启用时间戳</td></tr>" +
                   "<tr><td style='border:1px solid #ccc;padding:8px'>TimeFormat</td><td style='border:1px solid #ccc;padding:8px'>字符串</td><td style='border:1px solid #ccc;padding:8px'>[yyyy-MM-dd HH:mm:ss]</td><td style='border:1px solid #ccc;padding:8px'>时间格式字符串</td></tr>" +
                   "</table>" +
                   "<h4>💡 时间格式参考：</h4>" +
                   "<ul>" +
                   "<li>yyyy - 四位年份（2026）</li>" +
                   "<li>MM - 两位月份（02）</li>" +
                   "<li>dd - 两位日期（16）</li>" +
                   "<li>HH - 24小时制小时（21）</li>" +
                   "<li>mm - 分钟（30）</li>" +
                   "<li>ss - 秒（45）</li>" +
                   "</ul>" +
                   "<h4>🔧 快捷指令：</h4>" +
                   "<ul>" +
                   "<li>reset：重置时间戳统计</li>" +
                   "<li>test：查看当前时间格式示例</li>" +
                   "</ul>" +
                   "<h4>📝 工作原理：</h4>" +
                   "<p>插件使用 PreMergeMessageHandler 在消息合并前拦截用户消息，" +
                   "在消息开头添加当前时间戳，然后将修改后的消息传递给后续处理流程。" +
                   "这样AI在回复时就能看到用户发送消息的具体时间。</p>" +
                   "</div>";
        }
    }
}
