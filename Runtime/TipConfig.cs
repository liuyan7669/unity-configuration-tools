using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
#endif

namespace Cowart.ConfigLoader
{
/// <summary>
/// 提示文本配置数据。
/// 对应 StreamingAssets/tips.json 的根节点数据。
/// </summary>
/// <remarks>
/// 序列化字段名与 tips.json 字段一一对应，不应随 C# 命名风格调整。
/// </remarks>
[Serializable]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "TipConfig")]
public class TipConfig
{
    [NonSerialized]
    private TextLoader textLoader;

    private TextLoader Loader
    {
        get
        {
            if (textLoader == null)
            {
                textLoader = new TextLoader();
            }

            return textLoader;
        }
    }

    /// <summary>
    /// 提示文本列表。
    /// </summary>
    [SerializeField]
    private List<TipsItemData> items = new List<TipsItemData>();

    /// <summary>
    /// 获取只读的提示文本列表。
    /// </summary>
    public IReadOnlyList<TipsItemData> Items
    {
        get
        {
            EnsureItems();
            return items;
        }
    }

    /// <summary>
    /// 当前是否正在加载提示文本配置。
    /// </summary>
    public bool IsLoading => Loader.IsLoading;

    /// <summary>
    /// 读取提示文本 JSON 配置内容。
    /// </summary>
    /// <param name="content">tips.json 的完整 JSON 字符串。</param>
    /// <returns>解析成功返回 true，解析失败返回 false。</returns>
    public bool ReadJson(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        try
        {
            content = content.TrimStart('\uFEFF');
            TipConfig config = JsonUtility.FromJson<TipConfig>(content);
            items = config?.items ?? new List<TipsItemData>();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Parse tips.json failed: {exception.Message}");
            items = new List<TipsItemData>();
            return false;
        }
    }

    /// <summary>
    /// 从指定地址异步读取并解析提示文本 JSON 配置。
    /// </summary>
    public void ReadJsonFileAsync(string filePath, Action<bool> onCompleted)
    {
        Loader.LoadAsync(filePath, success =>
        {
            if (!success)
            {
                onCompleted?.Invoke(false);
                return;
            }

            onCompleted?.Invoke(ReadJson(Loader.Content));
        });
    }

    public void StopLoading()
    {
        Loader.StopLoading();
    }

    public void ReloadJsonFileAsync(string filePath, Action<bool> onCompleted)
    {
        ReadJsonFileAsync(filePath, onCompleted);
    }

    /// <summary>
    /// 获取提示文本数量。
    /// </summary>
    public int Count => items == null ? 0 : items.Count;

    /// <summary>
    /// 按编号获取提示文本数据。
    /// </summary>
    /// <param name="id">提示编号。</param>
    /// <returns>找到时返回提示文本数据，否则返回 null。</returns>
    public TipsItemData GetItemById(int id)
    {
        TryGetItemById(id, out TipsItemData item);
        return item;
    }

    /// <summary>
    /// 尝试按编号获取提示文本数据。
    /// </summary>
    /// <param name="id">提示编号。</param>
    /// <param name="item">找到的提示文本数据。</param>
    /// <returns>找到匹配编号时返回 true，否则返回 false。</returns>
    public bool TryGetItemById(int id, out TipsItemData item)
    {
        EnsureItems();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].id == id)
            {
                item = items[i];
                return true;
            }
        }

        item = null;
        return false;
    }

    // /// <summary>
    // /// 按编号获取提示文本内容。
    // /// </summary>
    // /// <param name="id">提示编号。</param>
    // /// <param name="defaultValue">未找到时返回的默认文本。</param>
    // /// <returns>找到时返回提示文本内容，否则返回默认文本。</returns>
    // public string GetTipNameById(int id, string defaultValue = "")
    // {
    //     return GetTipTextById(id, defaultValue);
    // }

    /// <summary>
    /// 按编号获取提示文本内容。
    /// </summary>
    /// <param name="id">提示编号。</param>
    /// <param name="defaultValue">未找到时返回的默认文本。</param>
    /// <returns>找到时返回拼接后的提示文本内容，否则返回默认文本。</returns>
    public string GetTipTextById(int id, string defaultValue = "")
    {
        return TryGetItemById(id, out TipsItemData item) ? item.GetText() : defaultValue;
    }

    /// <summary>
    /// 按编号获取提示对应的音频 Resources 路径。
    /// </summary>
    /// <param name="id">提示编号。</param>
    /// <param name="defaultValue">未找到时返回的默认路径。</param>
    /// <returns>找到时返回音频 Resources 路径，否则返回默认路径。</returns>
    public string GetTipAudioPathById(int id, string defaultValue = "")
    {
        return TryGetItemById(id, out TipsItemData item) ? item.GetAudioPath() : defaultValue;
    }

    /// <summary>
    /// 按索引获取提示文本数据。
    /// </summary>
    /// <param name="index">提示文本索引。</param>
    /// <returns>索引有效时返回提示文本数据，否则返回 null。</returns>
    public TipsItemData GetItemAt(int index)
    {
        EnsureItems();

        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        return items[index];
    }

    // /// <summary>
    // /// 按索引获取提示文本内容。
    // /// </summary>
    // /// <param name="index">提示文本索引。</param>
    // /// <param name="defaultValue">索引无效时返回的默认文本。</param>
    // /// <returns>索引有效时返回提示文本内容，否则返回默认文本。</returns>
    // public string GetTipNameAt(int index, string defaultValue = "")
    // {
    //     return GetTipTextAt(index, defaultValue);
    // }

    /// <summary>
    /// 按索引获取提示文本内容。
    /// </summary>
    /// <param name="index">提示文本索引。</param>
    /// <param name="defaultValue">索引无效时返回的默认文本。</param>
    /// <returns>索引有效时返回拼接后的提示文本内容，否则返回默认文本。</returns>
    public string GetTipTextAt(int index, string defaultValue = "")
    {
        TipsItemData item = GetItemAt(index);
        return item == null ? defaultValue : item.GetText();
    }

    /// <summary>
    /// 按索引获取提示对应的音频 Resources 路径。
    /// </summary>
    /// <param name="index">提示文本索引。</param>
    /// <param name="defaultValue">索引无效时返回的默认路径。</param>
    /// <returns>索引有效时返回音频 Resources 路径，否则返回默认路径。</returns>
    public string GetTipAudioPathAt(int index, string defaultValue = "")
    {
        TipsItemData item = GetItemAt(index);
        return item == null ? defaultValue : item.GetAudioPath();
    }

    /// <summary>
    /// 确保提示文本列表不为空引用。
    /// </summary>
    private void EnsureItems()
    {
        if (items == null)
        {
            items = new List<TipsItemData>();
        }
    }

#if UNITY_EDITOR
    private const string DemoFileName = "tips.json";
    private const string StreamingAssetsFolderName = "StreamingAssets";

    /// <summary>
    /// 示例提示文本 JSON 内容。
    /// </summary>
    private const string DemoJson =
@"{
    ""items"": [
        {
            ""id"": 1,
            ""audioPath"": ""Audio/Tips/tip_001"",
            ""stringbuilder"": [
                ""Demo tip line 1."",
                ""    Demo tip indent line.""
            ]
        },
        {
            ""id"": 2,
            ""audioPath"": """",
            ""stringbuilder"": [
                ""Demo tip line 2.""
            ]
        }
    ]
}";

    /// <summary>
    /// 在 StreamingAssets 目录下创建提示文本配置示例文件。
    /// </summary>
    [MenuItem("Cowart/配置/模板/创建 tips.json 示例")]
    private static void CreateTipsJsonDemo()
    {
        CreateDemoJson(DemoFileName, DemoJson);
    }

    /// <summary>
    /// 创建示例 JSON 文件。
    /// </summary>
    /// <param name="fileName">示例文件名。</param>
    /// <param name="content">示例文件内容。</param>
    private static void CreateDemoJson(string fileName, string content)
    {
        string folderPath = Path.Combine(Application.dataPath, StreamingAssetsFolderName);
        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, fileName);
        if (File.Exists(filePath))
        {
            Debug.LogWarning($"示例文件已存在，未覆盖：{filePath}");
            return;
        }

        File.WriteAllText(filePath, content, new UTF8Encoding(false));
        AssetDatabase.Refresh();
        Debug.Log($"已创建示例文件：{filePath}");
    }
#endif
}

/// <summary>
/// 单条提示文本数据。
/// </summary>
/// <remarks>
/// 公共字段名属于 tips.json 的序列化契约。
/// </remarks>
[Serializable]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "TipsItemData")]
public class TipsItemData
{
    /// <summary>
    /// 提示编号。
    /// </summary>
    public int id;

    /// <summary>
    /// 提示对应的音频 Resources 路径，不包含 Resources 目录名和文件扩展名。
    /// 为空时表示该提示没有配置音频。
    /// </summary>
    public string audioPath;

    /// <summary>
    /// 提示文本内容。
    /// </summary>
    public List<string> stringbuilder = new List<string>();

    /// <summary>
    /// 获取拼接后的提示文本内容。
    /// </summary>
    /// <returns>使用换行符拼接后的提示文本。</returns>
    public string GetText()
    {
        if (stringbuilder == null || stringbuilder.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("\n", stringbuilder);
    }

    /// <summary>
    /// 获取提示对应的音频 Resources 路径。
    /// </summary>
    /// <returns>配置的音频 Resources 路径；未配置时返回空字符串。</returns>
    public string GetAudioPath()
    {
        return string.IsNullOrEmpty(audioPath) ? string.Empty : audioPath;
    }
}
}
