# AI_Chat 插件开发完整指南

<div align="center">

![AI_Chat Plugin](https://img.shields.io/badge/AI_Chat-Plugin%20Development-blue?style=for-the-badge)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6-purple?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-7.3-green?style=for-the-badge)

**🚀 从零开始编写 AI_Chat 插件的完整教程**

</div>

---

## 📑 目录

- [快速开始](#快速开始)
- [项目结构](#项目结构)
- [核心概念](#核心概念)
- [生命周期详解](#生命周期详解)
- [拦截器系统](#拦截器系统)
- [配置系统](#配置系统)
- [数据持久化](#数据持久化)
- [消息操作](#消息操作)
- [API 参考](#api-参考)
- [完整示例](#完整示例)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 快速开始

### 1. 环境要求

| 项目 | 版本要求 |
|------|---------|
| Visual Studio | 2019 或更高版本 |
| .NET Framework | 4.6 或更高版本 |
| AI_Chat 主程序 | 最新版本 |

### 2. 创建项目

#### 方法一：使用模板（推荐）

1. 复制 `PluginTemplate/Example` 文件夹
2. 重命名为你的插件名称
3. 修改 `.csproj` 文件中的程序集名称
4. 修改 `MyPlugin.cs` 中的插件信息

#### 方法二：从头创建

1. 在 Visual Studio 中创建 **类库(.NET Framework)** 项目
2. 目标框架选择 **.NET Framework 4.6**
3. 添加对 `AI_Chat.exe` 和 `Newtonsoft.Json` 的引用

### 3. 最小可运行插件

```csharp
using System;
using System.Collections.Generic;
using AI_Chat.Plugins;

namespace MyPlugin
{
    [Plugin(
        Id = "MyFirstPlugin",
        Name = "我的第一个插件",
        Version = "1.0.0",
        Author = "YourName",
        Description = "这是一个示例插件",
        AutoStart = true,
        Priority = 10
    )]
    public class MyFirstPlugin : PluginBase
    {
        public override string Id => "MyFirstPlugin";
        public override string Name => "我的第一个插件";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "YourName";
        public override string Description => "这是一个示例插件";

        protected override void OnInitialize()
        {
            Logger?.Info(Id, "插件初始化成功！");
        }

        protected override void OnStart()
        {
            // 注册拦截器
            Api.RegisterPostMergeMessageHandler(ctx =>
            {
                if (ctx.FullMessage == "你好")
                {
                    return new PostMergeMessageResult
                    {
                        IsIntercepted = true,
                        Response = "你好！我是插件回复的。"
                    };
                }
                return new PostMergeMessageResult();
            });
        }

        protected override void OnStop()
        {
            Logger?.Info(Id, "插件已停止");
        }
    }
}
```

### 4. 编译与部署

1. **编译项目**：生成 DLL 文件
2. **复制到插件目录**：将 DLL 复制到 `AI_Chat/Plugins/` 文件夹
3. **重启 AI_Chat**：框架会自动扫描并加载插件
4. **查看日志**：在日志中确认插件加载成功

---

## 项目结构

```
MyPlugin/
├── MyPlugin.csproj          # 项目文件
├── MyPlugin.cs              # 主插件类（必须）
├── Properties/
│   └── AssemblyInfo.cs      # 程序集信息
└── README.md                # 插件说明文档
```

### 项目文件 (.csproj) 示例

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{YOUR-GUID-HERE}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>MyPlugin</RootNamespace>
    <AssemblyName>MyPlugin</AssemblyName>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="Newtonsoft.Json">
      <HintPath>..\..\packages\Newtonsoft.Json.13.0.4\lib\net45\Newtonsoft.Json.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <Compile Include="MyPlugin.cs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\AI_Chat\AI_Chat.csproj">
      <Project>{8b7c5c0e-3a7b-4b2c-9d5e-1f2a3b4c5d6e}</Project>
      <Name>AI_Chat</Name>
    </ProjectReference>
  </ItemGroup>
</Project>
```

---

## 核心概念

### 1. 插件基类 (PluginBase)

所有插件必须继承 `PluginBase` 类，它提供了：

| 成员 | 类型 | 说明 |
|------|------|------|
| `Id` | string | 插件唯一标识 |
| `Name` | string | 插件显示名称 |
| `Version` | Version | 插件版本 |
| `Author` | string | 插件作者 |
| `Description` | string | 插件描述 |
| `Api` | IPluginApi | 框架API接口 |
| `Logger` | ILogger | 日志记录器 |

### 2. 插件特性 ([Plugin])

```csharp
[Plugin(
    Id = "PluginId",              // 唯一标识，不能重复
    Name = "插件名称",            // 显示名称
    Version = "1.0.0",            // 版本号
    Author = "作者名",            // 作者
    Description = "描述",         // 描述
    AutoStart = true,             // 是否自动启动
    Priority = 10                 // 优先级（1-99，数字越小优先级越高）
)]
```

### 3. 插件指令特性 ([PluginCommand])

用于标记公开可调用的方法。

```csharp
[PluginCommand("指令名", Description = "描述", Usage = "用法示例")]
public object MyCommand(Dictionary<string, object> param)
{
    // param 包含调用时传入的参数
    // 返回对象会被序列化为 JSON
    return new { success = true, message = "执行成功" };
}
```

**方法签名要求**：
- 返回类型：`object` 或 `Task<object>`（异步）
- 参数：`Dictionary<string, object> param`
- 访问修饰符：`public`

---

## 生命周期详解

### 完整生命周期流程

```
┌─────────────────┐
│   框架启动       │
└────────┬────────┘
         ▼
┌─────────────────┐
│  扫描插件DLL     │
└────────┬────────┘
         ▼
┌─────────────────┐     ┌─────────────────┐
│  OnInitialize() │────▶│  初始化配置      │
│   【初始化】     │     │  加载持久化数据   │
└────────┬────────┘     └─────────────────┘
         ▼
┌─────────────────┐     ┌─────────────────┐
│   OnStart()     │────▶│  注册拦截器      │
│   【启动】       │     │  启动后台任务    │
└────────┬────────┘     └─────────────────┘
         │
         │◄──────────── 插件运行中 ────────▶
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│   OnStop()      │────▶│  保存数据        │
│   【停止】       │     │  释放资源        │
└────────┬────────┘     └─────────────────┘
         ▼
┌─────────────────┐
│  框架卸载插件    │
└─────────────────┘
```

### 1. OnInitialize - 初始化

**触发时机**：插件被加载时执行一次

**主要任务**：
- 初始化默认配置
- 加载本地持久化数据
- 准备运行时资源

```csharp
protected override void OnInitialize()
{
    Logger?.Info(Id, "══════════ 插件初始化开始 ══════════");
    
    try
    {
        // 1. 初始化配置
        Dictionary<string, object> config = GetConfiguration();
        bool configChanged = false;
        
        if (!config.ContainsKey("MySetting"))
        {
            config["MySetting"] = "默认值";
            configChanged = true;
        }
        
        if (configChanged)
        {
            SetConfiguration(config);
            Logger?.Info(Id, "✅ 默认配置已初始化");
        }
        
        // 2. 加载持久化数据
        dynamic data = LoadData<dynamic>("data.json");
        if (data != null)
        {
            _counter = data.Counter ?? 0;
            Logger?.Info(Id, $"📊 加载历史数据：计数器={_counter}");
        }
        
        Logger?.Info(Id, "══════════ 插件初始化完成 ══════════");
    }
    catch (Exception ex)
    {
        Logger?.Error(Id, "❌ 初始化失败", ex);
    }
}
```

### 2. OnStart - 启动

**触发时机**：初始化完成后，或手动启用插件时

**主要任务**：
- 注册消息拦截器
- 启动后台线程/定时器
- 建立网络连接

```csharp
protected override void OnStart()
{
    Logger?.Info(Id, "▶️ 插件启动");
    
    if (Api == null)
    {
        Logger?.Error(Id, "❌ API未初始化");
        return;
    }
    
    // 注册各种拦截器（以下方法需要在类中自行实现）
    // RegisterPreMergeHandler();
    // RegisterPostMergeHandler();
    // RegisterLLMResponseHandler();
    
    Logger?.Info(Id, "✅ 所有拦截器注册完成");
}
```

### 3. OnStop - 停止

**触发时机**：插件被禁用或框架退出时

**主要任务**：
- 保存运行时数据
- 释放资源
- 清理临时文件

```csharp
protected override void OnStop()
{
    Logger?.Info(Id, "⏹️ 插件停止");
    
    // 保存数据
    SaveData("data.json", new
    {
        Counter = _counter,
        LastTime = DateTime.Now
    });
    
    Logger?.Info(Id, $"💾 数据已保存：计数器={_counter}");
}
```

### 4. OnConfigurationChanged - 配置变更

**触发时机**：前端修改配置并保存后

```csharp
protected override void OnConfigurationChanged()
{
    Logger?.Info(Id, "📝 配置已变更");
    
    // 重新读取配置
    string mySetting = GetConfig("MySetting", "默认值");
    Logger?.Info(Id, $"当前配置：MySetting={mySetting}");
    
    // 根据新配置调整运行时行为（自行实现）
    // UpdateRuntimeBehavior();
    
    base.OnConfigurationChanged();
}
```

---

## 拦截器系统

### 拦截器概览

| 拦截器 | 触发时机 | 可拦截 | 可修改 |
|--------|---------|--------|--------|
| PreMergeMessageHandler | 消息合并前 | ✅ | ✅ |
| PostMergeMessageHandler | 消息合并后 | ✅ | ✅ |
| MessageAppendedHandler | 消息追加完成 | ✅ | ✅ |
| LLMResponseHandler | AI回复生成后 | ✅ | ✅ |

### 1. PreMergeMessageHandler - 合并前拦截

**触发时机**：用户发送的分段消息被合并之前

**适用场景**：
- 敏感词过滤（提前拦截）
- 快速指令识别
- 消息格式校验

```csharp
Api.RegisterPreMergeMessageHandler(ctx =>
{
    // ctx 包含：
    // - RawMessage: 当前原始消息
    // - Source: 发送者ID
    // - Timestamp: 时间戳
    
    string message = ctx.RawMessage;
    
    // 示例1：敏感词过滤
    if (message.Contains("敏感词"))
    {
        Logger?.Warn(Id, $"🚫 拦截敏感消息：{message}");
        return new PreMergeMessageResult
        {
            IsIntercepted = true,  // 拦截此消息
            Response = "消息包含敏感内容，已被拦截。"
        };
    }
    
    // 示例2：快速指令
    if (message.StartsWith("!"))
    {
        return new PreMergeMessageResult
        {
            IsIntercepted = true,
            Response = "收到指令：" + message
        };
    }
    
    // 示例3：修改消息内容
    if (message.Contains("错别字"))
    {
        return new PreMergeMessageResult
        {
            IsModified = true,
            ModifiedMessage = message.Replace("错别字", "正确字")
        };
    }
    
    // 继续处理
    return new PreMergeMessageResult();
});
```

### 2. PostMergeMessageHandler - 合并后拦截

**触发时机**：分段消息合并为完整消息后

**适用场景**：
- 完整指令解析
- 语义分析
- 自定义回复

```csharp
Api.RegisterPostMergeMessageHandler(ctx =>
{
    // ctx 包含：
    // - FullMessage: 合并后的完整消息
    // - Source: 固定为 "user"
    // - Timestamp: 时间戳
    // - MessageFragments: 消息片段列表
    
    string fullMessage = ctx.FullMessage;
    
    // 示例：状态查询指令
    if (fullMessage == "!状态")
    {
        string status = $"📊 插件状态\n" +
                        $"├─ 处理消息：{_processCount} 条\n" +
                        $"└─ 当前配置：{GetConfig("Setting", "default")}";
        
        return new PostMergeMessageResult
        {
            IsIntercepted = true,
            Response = status
        };
    }
    
    // 示例：修改消息后交给AI
    if (fullMessage.StartsWith("翻译："))
    {
        string text = fullMessage.Substring(3);
        return new PostMergeMessageResult
        {
            IsModified = true,
            ModifiedMessage = $"请将以下内容翻译成中文：{text}"
        };
    }
    
    return new PostMergeMessageResult();
});
```

### 3. MessageAppendedHandler - 消息追加完成

**触发时机**：消息被追加到上一条用户消息后

**适用场景**：
- 拦截追加的消息
- 修改追加后的完整消息
- 更新消息统计

```csharp
Api.RegisterMessageAppendedHandler(ctx =>
{
    // ctx 包含：
    // - OriginalMessage: 追加前的消息
    // - AppendedContent: 追加的新内容
    // - FullMessage: 追加后的完整消息
    // - MessageIndex: 消息索引
    
    Logger?.Debug(Id, $"📝 消息追加：索引={ctx.MessageIndex}");
    Logger?.Debug(Id, $"   追加内容：{ctx.AppendedContent}");
    Logger?.Debug(Id, $"   完整消息：{ctx.FullMessage}");
    
    // 示例1：拦截追加的消息
    if (ctx.AppendedContent.Contains("敏感词"))
    {
        return new MessageAppendedResult
        {
            IsIntercepted = true,
            Response = "追加的内容包含敏感词，已被拦截。"
        };
    }
    
    // 示例2：修改追加后的消息
    if (ctx.FullMessage.Length > 100)
    {
        return new MessageAppendedResult
        {
            IsModified = true,
            ModifiedMessage = ctx.FullMessage.Substring(0, 100) + "...(已截断)"
        };
    }
    
    return new MessageAppendedResult();
});
```

### 4. LLMResponseHandler - AI回复处理

**触发时机**：AI生成回复后，返回给用户前

**适用场景**：
- 修改AI回复内容
- 添加前缀/后缀
- 内容过滤
- 延迟调整

```csharp
Api.RegisterLLMResponseHandler(ctx =>
{
    // ctx 包含：
    // - RawResponse: AI原始回复（JSON格式）
    // - RequestId: 请求ID
    
    if (string.IsNullOrWhiteSpace(ctx.RawResponse))
        return new LLMResponseResult();
    
    try
    {
        JObject json = JObject.Parse(ctx.RawResponse);
        bool modified = false;
        
        if (json["messages"] is JArray messages)
        {
            foreach (JToken msg in messages)
            {
                // 1. 添加前缀
                if (GetConfig("EnablePrefix", true) && msg["content"] != null)
                {
                    msg["content"] = "🤖 " + msg["content"];
                    modified = true;
                }
                
                // 2. 限制延迟
                if (msg["delay_ms"] != null)
                {
                    int maxDelay = GetConfig("MaxDelay", 5000);
                    int currentDelay = msg["delay_ms"].Value<int>();
                    
                    if (currentDelay > maxDelay)
                    {
                        msg["delay_ms"] = maxDelay;
                        modified = true;
                    }
                }
                
                // 3. 内容过滤
                if (msg["content"] != null)
                {
                    string content = msg["content"].ToString();
                    if (ContainsForbiddenContent(content))
                    {
                        msg["content"] = "[内容已过滤]";
                        modified = true;
                    }
                }
                
                // 4. 截断超长内容
                if (msg["content"] != null)
                {
                    string content = msg["content"].ToString();
                    int maxLength = GetConfig("MaxContentLength", 1000);
                    
                    if (content.Length > maxLength)
                    {
                        msg["content"] = content.Substring(0, maxLength) + "...(已截断)";
                        modified = true;
                    }
                }
            }
        }
        
        if (modified)
        {
            _processCount++;
            return new LLMResponseResult
            {
                IsModified = true,
                AlternativeResponse = json.ToString(Formatting.None)
            };
        }
    }
    catch (Exception ex)
    {
        Logger?.Error(Id, "处理AI回复失败", ex);
    }
    
    return new LLMResponseResult();
});
```

---

## 配置系统

### 1. 配置项定义

```csharp
// 配置键名常量（避免硬编码）
private const string CFG_ENABLE_FEATURE = "EnableFeature";
private const string CFG_MAX_COUNT = "MaxCount";
private const string CFG_API_KEY = "ApiKey";
private const string CFG_CUSTOM_MESSAGE = "CustomMessage";
```

### 2. 配置初始化

```csharp
protected override void OnInitialize()
{
    Dictionary<string, object> config = GetConfiguration();
    bool configChanged = false;
    
    // 布尔值配置
    if (!config.ContainsKey(CFG_ENABLE_FEATURE))
    {
        config[CFG_ENABLE_FEATURE] = true;
        configChanged = true;
    }
    
    // 整数配置
    if (!config.ContainsKey(CFG_MAX_COUNT))
    {
        config[CFG_MAX_COUNT] = 100;
        configChanged = true;
    }
    
    // 字符串配置
    if (!config.ContainsKey(CFG_API_KEY))
    {
        config[CFG_API_KEY] = "";
        configChanged = true;
    }
    
    // 字符串数组配置（逗号分隔）
    if (!config.ContainsKey(CFG_CUSTOM_MESSAGE))
    {
        config[CFG_CUSTOM_MESSAGE] = "你好,世界,测试";
        configChanged = true;
    }
    
    // 保存默认配置
    if (configChanged)
    {
        SetConfiguration(config);
        Logger?.Info(Id, "✅ 默认配置已保存");
    }
}
```

### 3. 读取配置

```csharp
// 带默认值的类型安全读取
bool enableFeature = GetConfig(CFG_ENABLE_FEATURE, true);
int maxCount = GetConfig(CFG_MAX_COUNT, 100);
string apiKey = GetConfig(CFG_API_KEY, "");
float probability = GetConfig("Probability", 0.5f);

// 读取字符串数组
string[] messages = GetConfig(CFG_CUSTOM_MESSAGE, "")
    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
```

### 4. 保存配置

```csharp
// 修改单个配置项
Dictionary<string, object> config = GetConfiguration();
config[CFG_MAX_COUNT] = 200;
SetConfiguration(config);

// 或使用便捷方法
SetConfig(CFG_MAX_COUNT, 200);
SetConfiguration(GetConfiguration());
```

---

## 数据持久化

### 1. 保存数据

```csharp
// 保存任意对象（自动序列化为JSON）
SaveData("mydata.json", new
{
    Counter = _counter,
    LastTime = DateTime.Now,
    Settings = new
    {
        Theme = "dark",
        Language = "zh-CN"
    }
});

// 保存列表
SaveData("history.json", _messageHistory);
```

### 2. 加载数据

```csharp
// 加载为动态类型
dynamic data = LoadData<dynamic>("mydata.json");
if (data != null)
{
    _counter = data.Counter ?? 0;
    DateTime lastTime = data.LastTime;
}

// 加载为强类型
public class MyData
{
    public int Counter { get; set; }
    public DateTime LastTime { get; set; }
}

MyData data = LoadData<MyData>("mydata.json");
if (data != null)
{
    _counter = data.Counter;
}

// 加载列表
List<string> history = LoadData<List<string>>("history.json") ?? new List<string>();
```

### 3. 数据存储位置

插件数据自动存储在：
```
AI_Chat/Plugins/Data/{PluginId}/
├── mydata.json
├── history.json
└── config.json  (配置文件)
```

---

## 消息操作

### 1. 获取上下文

```csharp
// 获取完整上下文
List<ContextMessage> context = Api.GetFullContext();

// 遍历上下文
foreach (var msg in context)
{
    Logger?.Info(Id, $"[{msg.Role}] {msg.Content}");
}

// 获取最后N条消息
var recentMessages = context.Skip(Math.Max(0, context.Count - 5)).ToList();

// 统计各角色消息数
int userCount = context.Count(m => m.Role == "user");
int assistantCount = context.Count(m => m.Role == "assistant");
```

### 2. 添加上下文消息

```csharp
// 添加系统消息（不会触发前端显示）
Api.AddContextMessage("system", "你是一个 helpful 助手。");

// 添加用户消息
Api.AddContextMessage("user", "你好");

// 添加助手消息
Api.AddContextMessage("assistant", "你好！有什么可以帮助你的吗？");
```

### 3. 删除上下文消息

```csharp
// 删除最后N条AI回复
int removed = Api.RemoveLastMessages("assistant", 3);
Logger?.Info(Id, $"已删除 {removed} 条AI回复");

// 删除最后N条用户消息
Api.RemoveLastMessages("user", 2);

// 删除最后N条系统消息
Api.RemoveLastMessages("system", 1);
```

### 4. 清空上下文

```csharp
Api.ClearContext();
Logger?.Info(Id, "上下文已清空");
```

### 5. 发送消息

```csharp
// 发送文本消息
await Api.SendMessageAsync("你好，这是一条测试消息");

// 发送图片
await Api.SendMessageAsync("C:\\Pictures\\image.png", new SendMessageOptions
{
    MessageType = MessageType.Image
});

// 发送语音
await Api.SendMessageAsync("C:\\Audio\\voice.amr", new SendMessageOptions
{
    MessageType = MessageType.Voice
});

// 指定目标用户
await Api.SendMessageAsync("私信消息", new SendMessageOptions
{
    TargetUserId = 123456789
});
```

---

## API 参考

### IPluginApi 接口

#### 拦截器注册

```csharp
// 合并前拦截器
void RegisterPreMergeMessageHandler(Func<PreMergeMessageContext, PreMergeMessageResult> handler);

// 合并后拦截器
void RegisterPostMergeMessageHandler(Func<PostMergeMessageContext, PostMergeMessageResult> handler);

// 消息追加拦截器
void RegisterMessageAppendedHandler(Func<MessageAppendedContext, MessageAppendedResult> handler);

// AI回复拦截器
void RegisterLLMResponseHandler(Func<LLMResponseContext, LLMResponseResult> handler);
```

#### 上下文操作

```csharp
// 获取完整上下文
List<ContextMessage> GetFullContext();

// 添加消息到上下文
void AddContextMessage(string role, string content);

// 清空上下文
void ClearContext();

// 删除指定角色的最后N条消息
int RemoveLastMessages(string role, int count);
```

#### 消息发送

```csharp
// 发送消息
Task<bool> SendMessageAsync(string message, SendMessageOptions options = null);
```

#### LLM 请求

```csharp
// 直接请求LLM
Task<string> RequestLLMAsync(string requestJson);
```

#### 配置操作

```csharp
// 获取软件全局配置
AppConfig GetConfig();

// 设置软件全局配置
void SetConfig(AppConfig config);

// 获取单个配置项
T GetConfigValue<T>(string key, T defaultValue = default);

// 设置单个配置项
void SetConfigValue<T>(string key, T value);
```

#### 权限相关

```csharp
// 获取当前插件已注册的权限
List<string> GetRegisteredPermissions();

// 获取指定插件的权限
List<string> GetPluginPermissions(string pluginId);

// 获取所有插件的权限
Dictionary<string, List<string>> GetAllPluginPermissions();
```

---

## 完整示例

### 示例：智能回复插件

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AI_Chat.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartReplyPlugin
{
    [Plugin(
        Id = "SmartReply",
        Name = "智能回复插件",
        Version = "1.0.0",
        Author = "Developer",
        Description = "提供智能回复、关键词触发、自动回复等功能",
        AutoStart = true,
        Priority = 5
    )]
    public class SmartReplyPlugin : PluginBase
    {
        public override string Id => "SmartReply";
        public override string Name => "智能回复插件";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "Developer";
        public override string Description => "提供智能回复、关键词触发、自动回复等功能";

        // 配置键名
        private const string CFG_KEYWORDS = "Keywords";
        private const string CFG_AUTO_REPLY = "AutoReply";
        private const string CFG_REPLY_TEMPLATE = "ReplyTemplate";
        private const string DATA_FILE = "replies.json";

        // 运行时数据
        private Dictionary<string, string> _keywordReplies = new Dictionary<string, string>();
        private int _replyCount = 0;

        protected override void OnInitialize()
        {
            Logger?.Info(Id, "🚀 智能回复插件初始化");

            // 初始化配置
            InitConfig();

            // 加载历史数据
            LoadHistoryData();
        }

        private void InitConfig()
        {
            var config = GetConfiguration();
            bool changed = false;

            if (!config.ContainsKey(CFG_KEYWORDS))
            {
                config[CFG_KEYWORDS] = "你好=你好！有什么可以帮你的吗？;再见=再见，祝你有美好的一天！";
                changed = true;
            }

            if (!config.ContainsKey(CFG_AUTO_REPLY))
            {
                config[CFG_AUTO_REPLY] = true;
                changed = true;
            }

            if (!config.ContainsKey(CFG_REPLY_TEMPLATE))
            {
                config[CFG_REPLY_TEMPLATE] = "[自动回复] {content}";
                changed = true;
            }

            if (changed)
            {
                SetConfiguration(config);
                Logger?.Info(Id, "✅ 默认配置已初始化");
            }
        }

        private void LoadHistoryData()
        {
            dynamic data = LoadData<dynamic>(DATA_FILE);
            if (data != null)
            {
                _replyCount = data.ReplyCount ?? 0;
                
                // 加载关键词回复
                if (data.KeywordReplies != null)
                {
                    foreach (var item in data.KeywordReplies)
                    {
                        _keywordReplies[item.Key] = item.Value.ToString();
                    }
                }
                
                Logger?.Info(Id, $"📊 加载历史数据：已回复 {_replyCount} 次");
            }
        }

        protected override void OnStart()
        {
            if (Api == null) return;

            // 注册合并后拦截器
            Api.RegisterPostMergeMessageHandler(ctx =>
            {
                string message = ctx.FullMessage;

                // 检查关键词回复
                foreach (var kvp in _keywordReplies)
                {
                    if (message.Contains(kvp.Key))
                    {
                        _replyCount++;
                        SaveData();

                        string template = GetConfig(CFG_REPLY_TEMPLATE, "[自动回复] {content}");
                        string reply = template.Replace("{content}", kvp.Value);

                        return new PostMergeMessageResult
                        {
                            IsIntercepted = true,
                            Response = reply
                        };
                    }
                }

                return new PostMergeMessageResult();
            });

            Logger?.Info(Id, "✅ 拦截器注册完成");
        }

        protected override void OnStop()
        {
            SaveData();
            Logger?.Info(Id, $"💾 数据已保存，累计回复 {_replyCount} 次");
        }

        private void SaveData()
        {
            SaveData(DATA_FILE, new
            {
                ReplyCount = _replyCount,
                KeywordReplies = _keywordReplies,
                LastSaveTime = DateTime.Now
            });
        }

        // 公开指令：添加关键词
        [PluginCommand("add_keyword", Description = "添加关键词回复", Usage = "add_keyword keyword=关键词 reply=回复内容")]
        public object AddKeyword(Dictionary<string, object> param)
        {
            string keyword = param.ContainsKey("keyword") ? param["keyword"]?.ToString() : null;
            string reply = param.ContainsKey("reply") ? param["reply"]?.ToString() : null;

            if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(reply))
            {
                return new { success = false, message = "参数 keyword 和 reply 不能为空" };
            }

            _keywordReplies[keyword] = reply;
            SaveData();

            Logger?.Info(Id, $"➕ 添加关键词：{keyword} -> {reply}");
            return new { success = true, message = $"已添加关键词：{keyword}" };
        }

        // 公开指令：获取统计
        [PluginCommand("stats", Description = "获取统计信息", Usage = "stats")]
        public object GetStats(Dictionary<string, object> param)
        {
            return new
            {
                success = true,
                message = "统计信息",
                data = new
                {
                    replyCount = _replyCount,
                    keywordCount = _keywordReplies.Count,
                    keywords = _keywordReplies.Keys.ToList()
                }
            };
        }

        public override string GetReadme()
        {
            return @"
            <div style='padding:15px;font-family:Segoe UI,Arial,sans-serif'>
                <h2 style='color:#2196F3'>🤖 智能回复插件</h2>
                <p>根据关键词自动回复消息，支持自定义回复模板。</p>
                
                <h3 style='color:#333'>⚙️ 配置项</h3>
                <ul>
                    <li><b>Keywords</b> - 关键词回复映射（格式：关键词=回复;关键词=回复）</li>
                    <li><b>AutoReply</b> - 是否启用自动回复</li>
                    <li><b>ReplyTemplate</b> - 回复模板（{content}为占位符）</li>
                </ul>
                
                <h3 style='color:#333'>🔧 指令</h3>
                <ul>
                    <li><b>add_keyword</b> - 添加关键词回复</li>
                    <li><b>stats</b> - 查看统计信息</li>
                </ul>
            </div>";
        }

        public override PluginPermissionsInfo GetPermissionsInfo()
        {
            var info = base.GetPermissionsInfo();
            info.DeclaredPermissions.Add("消息拦截 - PostMergeMessageHandler");
            info.DeclaredPermissions.Add("数据持久化 - 保存关键词和统计");
            return info;
        }
    }
}
```

---

## 最佳实践

### 1. 错误处理

```csharp
try
{
    // 可能出错的操作
    var result = Api.GetFullContext();
}
catch (Exception ex)
{
    // 记录详细错误信息
    Logger?.Error(Id, "获取上下文失败", ex);
    
    // 返回友好的错误提示
    return new { success = false, message = "操作失败，请查看日志" };
}
```

### 2. 日志记录

```csharp
// 不同级别的日志
Logger?.Debug(Id, "调试信息");      // 开发调试
Logger?.Info(Id, "一般信息");       // 正常运行
Logger?.Warn(Id, "警告信息");       // 需要注意
Logger?.Error(Id, "错误信息");      // 发生错误
Logger?.Error(Id, "错误详情", ex);  // 带异常对象
```

### 3. 配置管理

```csharp
// 使用常量定义配置键
private const string CFG_KEY = "MyConfig";

// 提供合理的默认值
var value = GetConfig(CFG_KEY, "default_value");

// 配置变更时重新加载
protected override void OnConfigurationChanged()
{
    _cachedValue = GetConfig(CFG_KEY, "default_value");
    base.OnConfigurationChanged();
}
```

### 4. 性能优化

```csharp
// 缓存频繁访问的数据
private List<string> _cachedKeywords;

protected override void OnStart()
{
    // 启动时加载缓存（自行实现 LoadKeywords 方法）
    // _cachedKeywords = LoadKeywords();
}

protected override void OnConfigurationChanged()
{
    // 配置变更时刷新缓存（自行实现 LoadKeywords 方法）
    // _cachedKeywords = LoadKeywords();
    base.OnConfigurationChanged();
}
```

### 5. 线程安全

```csharp
// 使用锁保护共享数据
private readonly object _lockObj = new object();
private int _counter = 0;

public void Increment()
{
    lock (_lockObj)
    {
        _counter++;
    }
}
```

---

## 常见问题

### Q1: 插件没有被加载？

**可能原因**：
1. DLL 没有复制到 `Plugins` 文件夹
2. 插件类没有继承 `PluginBase`
3. 缺少 `[Plugin]` 特性
4. `Id` 属性与特性中的 `Id` 不一致

**解决方法**：
- 检查编译输出路径
- 确认插件类定义正确
- 查看框架日志中的加载信息

### Q2: 拦截器没有生效？

**可能原因**：
1. `Api` 为 null
2. 插件优先级被其他插件覆盖
3. 拦截器返回了错误的结果

**解决方法**：
```csharp
protected override void OnStart()
{
    if (Api == null)
    {
        Logger?.Error(Id, "API 未初始化");
        return;
    }
    
    // 注册拦截器
    Api.RegisterXXXHandler(ctx => {
        // 确保返回正确的结果对象
        return new XXXResult();
    });
}
```

### Q3: 配置没有保存？

**可能原因**：
1. 没有调用 `SetConfiguration()`
2. 配置对象被修改后没有重新设置

**解决方法**：
```csharp
var config = GetConfiguration();
config["Key"] = "Value";
SetConfiguration(config);  // 必须调用才能保存
```

### Q4: 如何调试插件？

**方法**：
1. 使用 `Logger?.Debug()` 输出调试信息
2. 在 Visual Studio 中附加到 AI_Chat 进程
3. 查看 `AI_Chat/Logs/` 文件夹中的日志文件

### Q5: 插件之间如何通信？

**方法**：
1. 使用共享的数据文件
2. 通过框架的事件系统
3. 使用 `GetAllPluginPermissions()` 获取其他插件信息

---

## 附录

### A. 完整配置项列表

| 配置项 | 类型 | 说明 |
|--------|------|------|
| ApiKey | string | LLM API密钥 |
| ApiUrl | string | LLM API地址 |
| Model | string | 模型名称 |
| Temperature | float | 温度参数 |
| MaxTokens | int | 最大token数 |
| TopP | float | Top P参数 |
| BaseSystemPrompt | string | 基础系统提示词 |
| IncompleteInputPrompt | string | 不完整输入提示词 |
| MaxContextRounds | int | 最大上下文轮数 |
| TargetUserId | long | 目标用户ID |

### B. 消息类型

```csharp
public enum MessageType
{
    Text,   // 文本消息
    Image,  // 图片消息
    Voice   // 语音消息
}
```

### C. 角色类型

```csharp
// 上下文消息角色
"user"       // 用户
"assistant"  // AI助手
"system"     // 系统
```

---

<div align="center">

**🎉 恭喜！你现在可以开发自己的 AI_Chat 插件了！**

如有问题，请查看示例代码或提交 Issue。

</div>
