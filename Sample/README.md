<div align="center">

# 🎯 AI_Chat 插件开发示例

**一个完整的插件开发指南，展示所有可用API功能**

[![版本](https://img.shields.io/badge/版本-1.0.0-blue.svg)](https://github.com)
[![框架](https://img.shields.io/badge/框架-.NET%20Framework%204.8-purple.svg)](https://dotnet.microsoft.com)
[![许可证](https://img.shields.io/badge/许可证-MIT-green.svg)](LICENSE)

[快速开始](#-快速开始) • [API文档](#-api-文档) • [示例代码](#-示例代码) • [常见问题](#-常见问题)

</div>

---

## 📖 目录

- [项目概述](#-项目概述)
- [快速开始](#-快速开始)
- [插件生命周期](#-插件生命周期)
- [插件特性说明](#-插件特性说明)
  - [SupportSandbox 详解](#supportsandbox-属性详解)
- [API 文档](#-api-文档)
  - [消息处理器](#消息处理器)
  - [配置管理](#配置管理)
  - [上下文管理](#上下文管理)
  - [消息发送](#消息发送)
  - [用户与群聊管理](#用户与群聊管理)
  - [LLM调用](#llm调用)
  - [权限系统](#权限系统)
- [数据存储](#-数据存储)
- [沙箱虚拟化系统](#️-沙箱虚拟化系统)
- [插件命令](#-插件命令)
- [示例代码](#-示例代码)
- [最佳实践](#-最佳实践)
- [常见问题](#常见问题)

---

## 🌟 项目概述

本项目是一个完整的 AI_Chat 插件开发示例，展示了插件系统的所有功能接口。通过本示例，您可以学习：

- ✅ 如何创建一个标准插件
- ✅ 如何处理用户消息和AI回复
- ✅ 如何管理配置和数据存储
- ✅ 如何发送消息和调用LLM
- ✅ 如何实现插件命令

### 项目结构

```
Sample/
├── 📁 Sample/
│   ├── 📄 SamplePlugin.cs      # 主插件代码
│   ├── 📄 Sample.csproj        # 项目配置文件
│   └── 📁 Properties/
│       └── 📄 AssemblyInfo.cs  # 程序集信息
├── 📄 Sample.slnx              # 解决方案文件
└── 📄 README.md                # 本文档
```

---

## 🚀 快速开始

### 环境要求

| 要求 | 版本 |
|------|------|
| .NET Framework | 4.8+ |
| IDE | Visual Studio 2017+ |
| 依赖 | AI_Chat 主程序 |

### 构建步骤

```bash
# 1. 克隆或下载项目
cd Ai_Chat-main/Sample

# 2. 使用 dotnet 构建
dotnet build Sample.slnx

# 3. 输出文件位置
# Sample/Sample/bin/Debug/SamplePlugin.dll
```

### 安装插件

1. 编译生成 `SamplePlugin.dll`
2. 将 DLL 复制到 AI_Chat 的 `Plugins` 目录
3. 重启 AI_Chat 或在控制面板中加载插件

---

## 🔄 插件生命周期

插件的生命周期遵循以下状态流转：

```
┌─────────┐    Initialize()    ┌─────────────┐    Start()    ┌─────────┐
│ Unloaded │ ────────────────> │ Initialized │ ────────────> │ Running │
└─────────┘                    └─────────────┘               └─────────┘
                                     │ ▲                         │ │
                                     │ │                         │ │
                                     ▼ │     Stop()              ▼ │
                                 ┌─────────┐                 ┌─────────┐
                                 │ Stopped │ <────────────── │ Running │
                                 └─────────┘                 └─────────┘
                                      │
                                      │ Dispose()
                                      ▼
                                 ┌────────────┐
                                 │ Uninstalled│
                                 └────────────┘
```

### 生命周期方法详解

| 方法 | 调用时机 | 用途 | 注意事项 |
|------|----------|------|----------|
| `OnInitialize()` | 插件加载后 | 读取配置、初始化数据结构 | 不要注册消息处理器 |
| `OnStart()` | 启动插件时 | 注册处理器、启动后台任务 | API已完全初始化 |
| `OnStop()` | 停止插件时 | 保存数据、停止任务 | 应快速完成 |
| `OnDispose()` | 卸载插件时 | 释放所有资源 | 最后的清理机会 |

---

## 🏷️ 插件特性说明

使用 `[Plugin]` 特性定义插件的基本信息：

```csharp
[Plugin(
    Id = "Sample.SamplePlugin",              // 插件唯一标识符
    Name = "示例插件",                        // 显示名称
    Version = "1.0.0",                       // 版本号
    Author = "演示作者",                      // 作者
    Description = "插件描述",                 // 功能描述
    Priority = 100,                          // 优先级（越小越先执行）
    AutoStart = false,                       // 是否自动启动
    SupportSandbox = true                    // 是否支持沙箱
)]
public class SamplePlugin : PluginBase
{
    // 插件实现
}
```

### 特性字段详解

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | string | ✅ | 插件唯一标识，建议格式：`命名空间.类名` |
| `Name` | string | ✅ | 插件显示名称，用于UI展示 |
| `Version` | string | ✅ | 版本号，遵循语义化版本规范（如：1.0.0） |
| `Author` | string | ✅ | 插件作者名称 |
| `Description` | string | ✅ | 插件功能描述 |
| `Priority` | int | ❌ | 执行优先级，默认100，数字越小优先级越高 |
| `AutoStart` | bool | ❌ | 是否在加载后自动启动，默认false |
| `SupportSandbox` | bool | ❌ | 是否支持沙箱运行，默认true |

### SupportSandbox 属性详解

`SupportSandbox` 是插件安全模型中的核心属性，它决定了插件是否运行在受控的沙箱环境中。

#### 什么是沙箱模式？

当 `SupportSandbox = true` 时，插件将在隔离环境中运行：

```
┌─────────────────────────────────────────────────────────────────┐
│                     AI_Chat 主程序                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    沙箱边界                              │   │
│  │  ┌─────────────────────────────────────────────────┐   │   │
│  │  │              插件运行环境                        │   │   │
│  │  │                                                 │   │   │
│  │  │   ┌──────────────┐      ┌──────────────────┐   │   │   │
│  │  │   │  插件代码    │ ───> │ 虚拟化文件系统   │   │   │   │
│  │  │   └──────────────┘      └──────────────────┘   │   │   │
│  │  │          │                       │              │   │   │
│  │  │          ▼                       ▼              │   │   │
│  │  │   ┌──────────────┐      ┌──────────────────┐   │   │   │
│  │  │   │  API 调用    │ <─── │ 安全代理层       │   │   │   │
│  │  │   └──────────────┘      └──────────────────┘   │   │   │
│  │  │                                                 │   │   │
│  │  └─────────────────────────────────────────────────┘   │   │
│  │  │  所有文件操作被重定向到插件专属目录                 │   │
│  │  │  无法访问系统敏感路径                               │   │
│  │  └─────────────────────────────────────────────────────┘   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

#### SupportSandbox = true 的作用

| 功能 | 说明 |
|------|------|
| **路径隔离** | 插件的文件操作自动重定向到专属目录，防止路径冲突 |
| **数据保护** | 插件无法访问其他插件的数据或系统敏感文件 |
| **安全运行** | 即使插件代码存在问题，也不会影响主程序和其他插件 |
| **简化开发** | 使用相对路径即可，无需担心绝对路径的兼容性问题 |

#### 设置建议

```csharp
[Plugin(
    Id = "MyPlugin.MyPlugin",
    Name = "我的插件",
    Version = "1.0.0",
    Author = "作者",
    Description = "插件描述",
    SupportSandbox = true    // ✅ 推荐：启用沙箱保护
)]
public class MyPlugin : PluginBase
{
    // 插件代码
}
```

> **⚠️ 警告**：除非有特殊需求且了解风险，否则**强烈建议**保持 `SupportSandbox = true`。
> 禁用沙箱 (`SupportSandbox = false`) 可能导致：
> - 文件路径冲突
> - 数据安全隐患
> - 插件间相互干扰

---

## 📚 API 文档

### 消息处理器

消息处理器是插件系统的核心功能，允许插件在消息处理流程的各个阶段进行干预。

#### 处理器执行顺序

```
用户发送消息
     │
     ▼
┌─────────────────────┐
│ PreMerge Handler    │ ← 合并前：处理原始消息
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ 消息合并            │ ← 系统自动合并连续消息
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ PostMerge Handler   │ ← 合并后：处理完整消息
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ PreLLMRequest       │ ← LLM请求前：修改请求
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ LLM处理             │ ← 调用大模型
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ LLMResponse Handler │ ← LLM响应：处理AI回复
└─────────────────────┘
     │
     ▼
返回给用户
```

#### 1. 合并前消息处理器

**触发时机**：用户发送消息后，消息合并之前

**用途**：
- 🚫 拦截用户消息（不继续处理）
- ✏️ 修改用户原始输入
- 📝 记录用户消息日志

```csharp
Api.RegisterPreMergeMessageHandler((context) =>
{
    // context 参数说明：
    // ┌─────────────────┬────────────────────────────────┐
    // │ 字段            │ 说明                           │
    // ├─────────────────┼────────────────────────────────┤
    // │ UserId          │ 发送消息的用户ID (long)         │
    // │ RawMessage      │ 用户的原始消息内容 (string)     │
    // │ Source          │ 消息来源标识 (string)           │
    // │ Timestamp       │ 消息发送时间 (DateTime)         │
    // └─────────────────┴────────────────────────────────┘

    // 拦截消息示例
    if (context.RawMessage.Contains("拦截我"))
    {
        return new PreMergeMessageResult
        {
            IsIntercepted = true,      // 拦截标志
            Response = "消息已被拦截"   // 直接返回给用户
        };
    }

    // 修改消息示例
    if (context.RawMessage.Contains("修改我"))
    {
        return new PreMergeMessageResult
        {
            IsModified = true,
            ModifiedMessage = context.RawMessage.Replace("修改我", "[已修改]")
        };
    }

    return new PreMergeMessageResult(); // 继续正常处理
});
```

#### 2. 合并后消息处理器

**触发时机**：多条消息合并完成后，发送给LLM之前

```csharp
Api.RegisterPostMergeMessageHandler((context) =>
{
    // context 参数说明：
    // ┌─────────────────┬────────────────────────────────┐
    // │ 字段            │ 说明                           │
    // ├─────────────────┼────────────────────────────────┤
    // │ UserId          │ 用户ID                         │
    // │ FullMessage     │ 合并后的完整消息               │
    // │ Source          │ 消息来源                       │
    // │ Timestamp       │ 时间戳                         │
    // │ MessageFragments│ 合并前的消息片段列表           │
    // └─────────────────┴────────────────────────────────┘

    return new PostMergeMessageResult
    {
        IsModified = true,
        ModifiedMessage = "[插件标记] " + context.FullMessage
    };
});
```

#### 3. 消息追加处理器

**触发时机**：当新消息被追加到上一条用户消息时

```csharp
Api.RegisterMessageAppendedHandler((context) =>
{
    // context 参数说明：
    // ┌─────────────────┬────────────────────────────────┐
    // │ 字段            │ 说明                           │
    // ├─────────────────┼────────────────────────────────┤
    // │ UserId          │ 用户ID                         │
    // │ OriginalMessage │ 追加前的原始消息               │
    // │ AppendedContent │ 新追加的内容                   │
    // │ FullMessage     │ 追加后的完整消息               │
    // │ MessageIndex    │ 消息在上下文中的索引位置       │
    // └─────────────────┴────────────────────────────────┘

    return new MessageAppendedResult();
});
```

#### 4. LLM响应处理器

**触发时机**：收到LLM响应后，格式化处理之前

```csharp
Api.RegisterLLMResponseHandler((context) =>
{
    // context 参数说明：
    // ┌─────────────────┬────────────────────────────────┐
    // │ 字段            │ 说明                           │
    // ├─────────────────┼────────────────────────────────┤
    // │ UserId          │ 用户ID                         │
    // │ RawResponse     │ LLM的原始响应（JSON格式）      │
    // │ RequestId       │ 请求的唯一标识符               │
    // └─────────────────┴────────────────────────────────┘

    return new LLMResponseResult
    {
        IsModified = true,
        AlternativeResponse = context.RawResponse + "\n\n--- 插件标注 ---"
    };
});
```

#### 5. LLM请求前处理器

**触发时机**：发送请求给LLM之前

```csharp
Api.RegisterPreLLMRequestHandler((context) =>
{
    // context 参数说明：
    // ┌─────────────────┬────────────────────────────────┐
    // │ 字段            │ 说明                           │
    // ├─────────────────┼────────────────────────────────┤
    // │ UserId          │ 用户ID                         │
    // │ RequestJson     │ 将要发送的请求JSON（可修改）   │
    // │ RequestId       │ 请求唯一标识                   │
    // │ ContextMessages │ 当前对话的上下文消息列表       │
    // │ UserMessage     │ 用户输入的原始消息             │
    // └─────────────────┴────────────────────────────────┘

    return new PreLLMRequestResult();
});
```

#### 6. 群聊消息处理器

**触发时机**：收到群聊消息时

```csharp
Api.RegisterGroupMessageHandler((context) =>
{
    // context 参数说明：
    // ┌─────────────────┬────────────────────────────────┐
    // │ 字段            │ 说明                           │
    // ├─────────────────┼────────────────────────────────┤
    // │ GroupId         │ 群聊ID                         │
    // │ UserId          │ 发送者用户ID                   │
    // │ MessageId       │ 消息ID                         │
    // │ RawMessage      │ 原始消息内容                   │
    // │ Timestamp       │ 消息时间戳                     │
    // │ SenderNickname  │ 发送者昵称                     │
    // │ MessageArray    │ 消息卡片数组（CQ码等）         │
    // └─────────────────┴────────────────────────────────┘

    if (context.RawMessage.Contains("@机器人"))
    {
        return new GroupMessageResult
        {
            IsHandled = true,
            ReplyMessage = $"@{context.SenderNickname} 你好！"
        };
    }

    return new GroupMessageResult();
});
```

---

### 配置管理

#### 获取完整配置

```csharp
// 获取完整的软件配置
AppConfig config = Api.GetConfig();

// AppConfig 对象包含以下字段：
// ┌─────────────────────────┬────────────────────────────────┐
// │ 字段                    │ 说明                           │
// ├─────────────────────────┼────────────────────────────────┤
// │ ApiKey                  │ LLM API密钥                    │
// │ ApiUrl                  │ LLM API地址                    │
// │ Model                   │ 使用的模型名称                 │
// │ Temperature             │ 温度参数 (float)               │
// │ MaxTokens               │ 最大token数 (int)              │
// │ TopP                    │ Top-P采样参数 (float)          │
// │ WebsocketServerUri      │ WebSocket服务器地址            │
// │ WebsocketToken          │ WebSocket令牌                  │
// │ WebsocketKeepAliveInterval│ 保活间隔 (int)               │
// │ MaxContextRounds        │ 最大上下文轮数 (int)           │
// │ RoleCardsApiUrl         │ 角色卡API地址                  │
// └─────────────────────────┴────────────────────────────────┘
```

#### 获取/设置单个配置项

```csharp
// 获取配置项
string model = Api.GetConfigValue<string>("LlmModelName", "默认模型");
float temp = Api.GetConfigValue<float>("LlmTemperature", 0.7f);

// 设置配置项（会自动保存）
Api.SetConfigValue("LlmTemperature", 0.8f);
```

---

### 上下文管理

#### 获取上下文

```csharp
// 获取用户的完整对话上下文
List<ContextMessage> context = Api.GetFullContext(userId);

// ContextMessage 对象结构：
// ┌─────────────────┬────────────────────────────────┐
// │ 字段            │ 说明                           │
// ├─────────────────┼────────────────────────────────┤
// │ Role            │ 角色 (user/assistant/system)   │
// │ Content         │ 消息内容                       │
// │ Timestamp       │ 时间戳                         │
// │ Tag             │ 消息类型标记                   │
// └─────────────────┴────────────────────────────────┘
```

#### 添加消息到上下文

```csharp
// 添加系统消息
Api.AddContextMessage(userId, "system", "系统提示内容");

// 添加用户消息
Api.AddContextMessage(userId, "user", "用户消息内容");

// 添加助手消息
Api.AddContextMessage(userId, "assistant", "AI回复内容");
```

#### 删除和清空上下文

```csharp
// 删除指定角色的最后N条消息
int removed = Api.RemoveLastMessages(userId, "user", 2);

// 清空用户的完整上下文（不可逆！）
Api.ClearContext(userId);
```

---

### 消息发送

#### 发送私聊消息

```csharp
// 发送文本消息
bool success = await Api.SendMessageAsync(userId, "消息内容");

// 发送图片消息
var options = new SendMessageOptions { MessageType = MessageType.Image };
bool success = await Api.SendMessageAsync(userId, "图片路径", options);

// 发送语音消息
var options = new SendMessageOptions { MessageType = MessageType.Voice };
bool success = await Api.SendMessageAsync(userId, "语音文件路径", options);
```

#### 发送群聊消息

```csharp
// 发送群聊文本消息
bool success = await Api.SendGroupMessageAsync(groupId, "消息内容");

// 发送群聊图片
var options = new SendMessageOptions { MessageType = MessageType.Image };
bool success = await Api.SendGroupMessageAsync(groupId, "图片路径", options);
```

---

### 用户与群聊管理

#### 用户管理

```csharp
// 获取所有允许的用户ID列表
List<long> allowedUsers = Api.GetAllowedUserIds();

// 检查用户是否被允许
bool isAllowed = Api.IsUserAllowed(userId);

// 添加允许的用户
Api.AddAllowedUser(userId);

// 移除允许的用户
Api.RemoveAllowedUser(userId);
```

#### 群聊管理

```csharp
// 获取所有允许的群聊ID列表
List<long> allowedGroups = Api.GetAllowedGroupIds();

// 检查群聊是否被允许
bool isAllowed = Api.IsGroupAllowed(groupId);

// 添加允许的群聊
Api.AddAllowedGroup(groupId);

// 移除允许的群聊
Api.RemoveAllowedGroup(groupId);
```

---

### LLM调用

插件可以直接调用大模型API：

```csharp
// 构建请求JSON（OpenAI格式）
string requestJson = JsonConvert.SerializeObject(new
{
    model = "gpt-3.5-turbo",
    messages = new[]
    {
        new { role = "system", content = "你是一个助手" },
        new { role = "user", content = "你好" }
    },
    temperature = 0.7,
    max_tokens = 500
});

// 发送请求并获取响应
string response = await Api.RequestLLMAsync(requestJson);
```

---

### 权限系统

```csharp
// 获取当前插件的已注册权限
List<string> myPermissions = Api.GetRegisteredPermissions();

// 获取指定插件的权限列表
List<string> pluginPerms = Api.GetPluginPermissions("Other.Plugin.Id");

// 获取所有插件的权限信息
Dictionary<string, List<string>> allPerms = Api.GetAllPluginPermissions();
```

---

## 💾 数据存储

### PluginDataHelper 类

插件可以通过 `Data` 属性访问数据存储功能：

```csharp
// Data 帮助类提供的功能：
// ┌─────────────────────────────────────────────────────────────┐
// │                       配置操作                              │
// ├──────────────────┬──────────────────────────────────────────┤
// │ Get<T>(key, def) │ 获取配置项，支持泛型                     │
// │ Set<T>(key, val) │ 设置配置项                               │
// │ Has(key)         │ 检查配置是否存在                         │
// │ Remove(key)      │ 移除配置项                               │
// │ SaveConfig()     │ 保存配置到文件                           │
// │ LoadConfig()     │ 从文件加载配置                           │
// ├──────────────────┴──────────────────────────────────────────┤
// │                       文件操作                              │
// ├──────────────────┬──────────────────────────────────────────┤
// │ ReadText(path)   │ 读取文本文件                             │
// │ WriteText(path)  │ 写入文本文件                             │
// │ SaveJson<T>()    │ 保存对象为JSON                           │
// │ LoadJson<T>()    │ 从JSON加载对象                           │
// │ ReadBytes(path)  │ 读取二进制文件                           │
// │ WriteBytes()     │ 写入二进制文件                           │
// ├──────────────────┴──────────────────────────────────────────┤
// │                       目录操作                              │
// ├──────────────────┬──────────────────────────────────────────┤
// │ CreateDir(path)  │ 创建目录                                 │
// │ DirExists(path)  │ 检查目录是否存在                         │
// │ DeleteDir(path)  │ 删除目录                                 │
// │ Files(path)      │ 获取文件列表                             │
// │ Dirs(path)       │ 获取目录列表                             │
// └──────────────────┴──────────────────────────────────────────┘
```

### 存储路径

| 类型 | 路径 |
|------|------|
| 配置文件 | `PluginConfigs/{插件ID}.json` |
| 数据文件 | `PluginData/{插件ID}/` |

### 使用示例

```csharp
protected override void OnInitialize()
{
    // 读取配置
    string setting = Data.Get("MySetting", "默认值");
    int count = Data.Get("Count", 0);
    
    // 设置配置
    Data.Set("LastRun", DateTime.Now.ToString());
    Data.Set("Count", count + 1);
    
    // 保存配置
    Data.SaveConfig();
    
    // 文件操作
    Data.WriteText("log.txt", "日志内容");
    string content = Data.ReadText("log.txt");
    
    // JSON操作
    var data = new { Name = "测试", Value = 123 };
    Data.SaveJson("data.json", data);
    var loaded = Data.LoadJson<dynamic>("data.json");
}
```

---

## ⚠️ 沙箱虚拟化系统

### 为什么必须使用 Data 帮助类？

> **重要提示**：当 `SupportSandbox = true` 时，插件运行在沙箱环境中，直接使用 `System.IO` 类可能会导致不可预期的行为！

### 不使用 Data 帮助类的严重后果

#### 后果 1：路径不可靠

```csharp
// ❌ 错误做法：直接使用 System.IO
protected override void OnInitialize()
{
    // 你认为写入到了这个路径
    File.WriteAllText("C:\\MyPlugin\\config.json", "内容");
    
    // 但实际上文件可能被写入到了其他位置！
    // 你无法确定文件的真实存储位置
}

// ✅ 正确做法：使用 Data 帮助类
protected override void OnInitialize()
{
    // 使用相对路径，Data 帮助类会正确处理
    Data.WriteText("config.json", "内容");
    // 稳定可靠，路径明确
}
```

#### 后果 2：配置文件丢失

```csharp
// ❌ 错误做法
protected override void OnInitialize()
{
    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    if (File.Exists(configPath))  // 可能返回错误结果！
    {
        string content = File.ReadAllText(configPath);  // 可能读取到错误的文件！
    }
}

// ✅ 正确做法
protected override void OnInitialize()
{
    // Data 帮助类自动处理路径
    string content = Data.ReadText("config.json");
    // 或者使用配置系统
    string value = Data.Get("ConfigKey", "默认值");
}
```

#### 后果 3：文件删除不生效

```csharp
// ❌ 错误做法
protected override void OnStop()
{
    // 你以为删除了文件
    File.Delete("C:\\data\\temp.txt");
    
    // 但实际上文件可能仍然存在！
    // 其他程序可能仍然能访问到它
}

// ✅ 正确做法
protected override void OnStop()
{
    // 使用 Data 帮助类，确保操作正确执行
    Data.Delete("temp.txt");
}
```

#### 后果 4：跨插件数据冲突

```csharp
// ❌ 错误做法：使用硬编码路径
protected override void OnInitialize()
{
    // 多个插件使用相同路径会导致冲突
    File.WriteAllText("C:\\plugins\\shared.json", "数据");
    // 数据可能被其他插件覆盖，或读取到错误的数据
}

// ✅ 正确做法：使用 Data 帮助类
protected override void OnInitialize()
{
    // 每个插件的数据自动隔离
    Data.WriteText("shared.json", "数据");
    // 数据安全隔离，不会冲突
}
```

#### 后果 5：调试困难

```csharp
// ❌ 错误做法
protected override void OnStart()
{
    File.WriteAllText("debug.log", "调试信息");
    // 你可能找不到这个文件在哪里！
}

// ✅ 正确做法
protected override void OnStart()
{
    Data.WriteText("debug.log", "调试信息");
    // 文件位置明确: PluginData/{插件ID}/debug.log
    Logger.Info(Id, "调试信息");  // 或者直接使用日志系统
}
```

#### 后果 6：文件操作异常

```csharp
// ❌ 错误做法
protected override void OnInitialize()
{
    // 可能抛出意外的异常
    // - FileNotFoundException（文件实际存在）
    // - UnauthorizedAccessException（有权限但被阻止）
    // - 文件内容与预期不符
    var files = Directory.GetFiles("C:\\data");
}

// ✅ 正确做法
protected override void OnInitialize()
{
    // 稳定可靠的文件操作
    var files = Data.Files("", "*.txt");
}
```

### Data 帮助类的优势

| 特性 | System.IO | Data 帮助类 |
|------|-----------|-------------|
| 路径管理 | 需要手动处理 | 自动管理相对路径 |
| 沙箱兼容 | 可能产生意外行为 | 完全兼容 |
| 数据隔离 | 需要手动实现 | 自动隔离 |
| 配置持久化 | 需要手动实现 | 自动保存/加载 |
| 安全性 | 可能访问敏感路径 | 限制在插件目录内 |
| 调试友好 | 路径不透明 | 路径明确可查 |
| 异常处理 | 可能产生意外异常 | 行为可预测 |

### 存储路径

| 类型 | 路径 |
|------|------|
| 配置文件 | `PluginConfigs/{插件ID}.json` |
| 数据文件 | `PluginData/{插件ID}/` |

### 最佳实践总结

```
┌─────────────────────────────────────────────────────────────────────┐
│                        文件操作最佳实践                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ✅ DO（推荐）                        ❌ DON'T（避免）              │
│  ─────────────────                   ─────────────────              │
│  Data.ReadText("file.txt")           File.ReadAllText(path)         │
│  Data.WriteText("file.txt", content) File.WriteAllText(path, ...)   │
│  Data.SaveJson("data.json", obj)     JsonSerializer + File.Write    │
│  Data.Get("key", default)            手动读取配置文件               │
│  Data.SaveConfig()                   手动保存配置                   │
│  Logger.Info(Id, "message")          File.AppendAllText(log, ...)   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 插件命令

插件可以定义可被外部调用的命令：

### 定义命令

```csharp
/// <summary>
/// 命令说明
/// </summary>
/// <param name="parameters">参数字典</param>
/// <returns>命令执行结果</returns>
[PluginCommand("CommandName", Description = "命令描述", Usage = "使用说明")]
public object CommandMethod(Dictionary<string, object> parameters)
{
    // 获取参数
    string param1 = parameters["key1"].ToString();
    int param2 = Convert.ToInt32(parameters["key2"]);
    
    // 返回结果
    return new { Success = true, Result = "结果" };
}
```

### 命令特性参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `Name` | string | 命令名称 |
| `Description` | string | 命令描述 |
| `Usage` | string | 使用说明 |

### 调用命令

```csharp
// 通过 PluginManager 调用
var result = pluginManager.ExecuteCommand(
    "Sample.SamplePlugin",    // 插件ID
    "GetInfo",                // 命令名称
    new Dictionary<string, object>()  // 参数
);
```

---

## 📝 示例代码

### 完整插件模板

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AI_Chat.Plugins;

namespace MyPlugin
{
    [Plugin(
        Id = "MyPlugin.MyPlugin",
        Name = "我的插件",
        Version = "1.0.0",
        Author = "作者名",
        Description = "插件描述",
        Priority = 100,
        AutoStart = false,
        SupportSandbox = true
    )]
    public class MyPlugin : PluginBase
    {
        protected override void OnInitialize()
        {
            Logger.Info(Id, "插件初始化");
            // 读取配置、初始化数据
        }

        protected override void OnStart()
        {
            Logger.Info(Id, "插件启动");
            // 注册消息处理器
            Api.RegisterPreMergeMessageHandler(OnPreMergeMessage);
        }

        protected override void OnStop()
        {
            Logger.Info(Id, "插件停止");
            Data.SaveConfig();
        }

        protected override void OnDispose()
        {
            Logger.Info(Id, "插件释放");
        }

        private PreMergeMessageResult OnPreMergeMessage(PreMergeMessageContext context)
        {
            // 处理消息
            return new PreMergeMessageResult();
        }

        [PluginCommand("MyCommand", Description = "我的命令", Usage = "无参数")]
        public object MyCommand(Dictionary<string, object> parameters)
        {
            return new { Success = true };
        }
    }
}
```

---

## ⭐ 最佳实践

### 1. 插件设计原则

| 原则 | 说明 |
|------|------|
| 🔒 **安全性** | 不要暴露敏感信息，验证所有输入 |
| ⚡ **性能** | 避免在处理器中执行耗时操作 |
| 🔄 **兼容性** | 处理API可能为null的情况 |
| 📝 **日志** | 记录关键操作，便于调试 |

### 2. 错误处理

```csharp
Api.RegisterPreMergeMessageHandler((context) =>
{
    try
    {
        // 处理逻辑
        return new PreMergeMessageResult();
    }
    catch (Exception ex)
    {
        Logger.Error(Id, "处理消息出错", ex);
        return new PreMergeMessageResult(); // 返回默认结果，不中断流程
    }
});
```

### 3. 资源管理

```csharp
private System.Threading.Timer _timer;

protected override void OnStart()
{
    _timer = new System.Threading.Timer(Callback, null, 1000, 1000);
}

protected override void OnStop()
{
    _timer?.Dispose(); // 释放资源
    _timer = null;
}
```

### 4. 配置管理

```csharp
// 使用有意义的配置键名
Data.Set("Feature.Enabled", true);
Data.Set("Feature.Timeout", 30);

// 使用默认值确保兼容性
bool enabled = Data.Get("Feature.Enabled", true);
```

---

## ❓ 常见问题

<details>
<summary><b>Q: 插件加载失败怎么办？</b></summary>

**A:** 检查以下几点：
1. 确保 DLL 文件位于正确的 `Plugins` 目录
2. 检查 .NET Framework 版本是否匹配
3. 查看日志文件中的错误信息
4. 确保插件类正确继承 `PluginBase`
5. 确保插件类有 `[Plugin]` 特性

</details>

<details>
<summary><b>Q: 为什么消息处理器没有被执行？</b></summary>

**A:** 可能的原因：
1. 插件未启动（检查 `AutoStart` 设置）
2. 插件优先级过低，被其他插件拦截
3. 处理器返回了 `IsIntercepted = true`
4. API 对象为 null（检查 `OnStart` 中的判断）

</details>

<details>
<summary><b>Q: 如何调试插件？</b></summary>

**A:** 调试方法：
1. 使用 `Logger` 记录调试信息
2. 在 Visual Studio 中附加到 AI_Chat 进程
3. 检查 `BotLogs` 目录下的日志文件

</details>

<details>
<summary><b>Q: 插件配置保存在哪里？</b></summary>

**A:** 配置文件位置：
- 配置文件：`PluginConfigs/{插件ID}.json`
- 数据文件：`PluginData/{插件ID}/`

</details>

<details>
<summary><b>Q: 如何处理异步操作？</b></summary>

**A:** 使用 async/await：

```csharp
[PluginCommand("AsyncCommand")]
public async Task<object> AsyncCommand(Dictionary<string, object> parameters)
{
    await Task.Delay(1000);
    return new { Success = true };
}
```

</details>

---

## 📄 许可证

本项目采用 MIT 许可证。

---

<div align="center">

**Made with ❤️ for AI_Chat Plugin Developers**

[⬆ 返回顶部](#-ai_chat-插件开发示例)

</div>
