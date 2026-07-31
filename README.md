# Configuration Tools

适用于 Unity 的轻量配置数据工具，提供常用配置的数据结构、JSON 解析、异步文件加载和查询接口。

## 功能

- `DialogueConfig`：管理分组对话、角色、文本、音频和动画配置。
- `QuestionConfig`：管理题目、选项、答案、得分和答案比较。
- `TipConfig`：管理提示文本及对应的音频资源路径。
- `RawTextConfig`：读取 JSON、XML 或表格文件中的原始文本内容。
- 支持通过 `UnityWebRequest` 从本地路径或 URL 异步加载配置。
- 提供 Unity 编辑器菜单，一键生成配置示例文件。

## 安装

### 通过 OpenUPM 安装

在 Unity 的 `Edit > Project Settings > Package Manager` 中添加：

```text
Name: package.openupm.com
URL: https://package.openupm.com
Scope: com.cowart.configuration-tools
```

然后通过 `Add package by name` 安装：

```text
com.cowart.configuration-tools
```

### 通过 Git URL 安装

```text
https://github.com/liuyan7669/unity-configuration-tools.git#1.0.1
```

## 创建示例配置

安装后可通过 Unity 菜单 `Cowart > 配置模板` 创建以下文件：

- `dialogues.json`
- `questions.json`
- `tips.json`
- `raw_text.json`

示例文件生成在 `Assets/StreamingAssets` 中。若同名文件已经存在，不会覆盖原文件。

## 使用示例

### 解析 JSON 字符串

```csharp
using Cowart.ConfigurationTools;

DialogueConfig config = new DialogueConfig();
if (config.ReadJson(jsonContent))
{
    var dialogues = config.GetDialoguesByTableName("Main");
}
```

### 从 StreamingAssets 异步加载

```csharp
using System.IO;
using Cowart.ConfigurationTools;
using UnityEngine;

QuestionConfig config = new QuestionConfig();
string path = Path.Combine(Application.streamingAssetsPath, "questions.json");

config.ReadJsonFileAsync(path, success =>
{
    if (success && config.TryGetQuestionById("question_001", out QuestionData question))
    {
        Debug.Log(question.question);
    }
});
```

需要中止当前异步加载时，可调用 `StopLoading()`；需要停止当前加载并重新读取时，可调用对应的 `ReloadJsonFileAsync()` 或 `ReloadFileAsync()`。

## 主要接口

- `DialogueConfig`：`GetTableByName`、`TryGetTableByName`、`GetDialoguesByTableName`
- `QuestionConfig`：`TryGetQuestionById`、`SetCurrentScoreById`、`SetSelectAnswerById`、`IsAnswerCorrectById`
- `TipConfig`：`GetItemById`、`TryGetItemById`、`GetTipTextById`、`GetTipAudioPathById`
- `RawTextConfig`：`ReadJson`、`ReadXml`、`ReadTable`、`ReadFileAsync`

## 注意事项

- 配置类使用 `JsonUtility` 解析 JSON，根节点和字段名必须与示例结构一致。
- 对话、题目和提示配置使用对象根节点，不支持直接以 JSON 数组作为根节点。
- `audioPath` 保存的是 `Resources` 相对路径，不包含 `Resources` 目录名和文件扩展名。
- 发布新版本时，请同步更新 `package.json` 中的 `version` 和 `CHANGELOG.md`。

## 环境要求

- Unity 2021.3 或更高版本

## 许可证

本包使用 MIT License，详情参见仓库中的许可证文件。
