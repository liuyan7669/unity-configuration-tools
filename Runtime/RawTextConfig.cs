using System;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
#endif

namespace Cowart.ConfigurationTools
{
/// <summary>
/// 原始文本配置数据。
/// 用于通过 ConfigKit 加载文件时保留完整文本内容，不对内容做结构化解析。
/// </summary>
public class RawTextConfig
{
    private sealed class LoadRequestState
    {
        public UnityWebRequest request;
        public bool isCancelled;
        public bool invokeCallbackWhenCancelled;
    }

    private LoadRequestState activeLoadRequest;

    /// <summary>
    /// 文件原始文本内容。
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// 当前是否正在加载原始文本。
    /// </summary>
    public bool IsLoading => activeLoadRequest != null;

    /// <summary>
    /// 读取 XML 文本内容。
    /// </summary>
    /// <param name="content">加载到的 XML 原始字符串。</param>
    /// <returns>始终返回 true，表示已接收文本内容。</returns>
    public bool ReadXml(string content)
    {
        return ReadContent(content);
    }

    /// <summary>
    /// 读取 JSON 文本内容。
    /// </summary>
    /// <param name="content">加载到的 JSON 原始字符串。</param>
    /// <returns>始终返回 true，表示已接收文本内容。</returns>
    public bool ReadJson(string content)
    {
        return ReadContent(content);
    }

    /// <summary>
    /// 读取 CSV 或表格文本内容。
    /// </summary>
    /// <param name="content">加载到的表格原始字符串。</param>
    /// <returns>始终返回 true，表示已接收文本内容。</returns>
    public bool ReadTable(string content)
    {
        return ReadContent(content);
    }

    /// <summary>
    /// 保存原始文本内容。
    /// </summary>
    /// <param name="content">加载到的原始字符串。</param>
    /// <returns>始终返回 true，表示已接收文本内容。</returns>
    private bool ReadContent(string content)
    {
        Content = content?.TrimStart('\uFEFF') ?? string.Empty;
        return true;
    }

    /// <summary>
    /// 从指定地址异步读取原始文本。
    /// </summary>
    public void ReadFileAsync(string filePath, Action<bool> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogError("原始文本文件路径不能为空。");
            onCompleted?.Invoke(false);
            return;
        }

        CancelActiveLoad(false);

        string requestPath = filePath.Contains("://") || filePath.StartsWith("jar:")
            ? filePath
            : new Uri(filePath).AbsoluteUri;

        UnityWebRequest request = UnityWebRequest.Get(requestPath);
        LoadRequestState requestState = new LoadRequestState { request = request };
        activeLoadRequest = requestState;

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += _ =>
        {
            try
            {
                if (requestState.isCancelled)
                {
                    if (requestState.invokeCallbackWhenCancelled)
                    {
                        onCompleted?.Invoke(false);
                    }

                    return;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"读取原始文本文件失败：{request.error}\n{filePath}");
                    onCompleted?.Invoke(false);
                    return;
                }

                onCompleted?.Invoke(ReadContent(request.downloadHandler.text));
            }
            finally
            {
                if (ReferenceEquals(activeLoadRequest, requestState))
                {
                    activeLoadRequest = null;
                }

                request.Dispose();
            }
        };
    }

    public void StopLoading()
    {
        CancelActiveLoad(true);
    }

    public void ReloadFileAsync(string filePath, Action<bool> onCompleted)
    {
        ReadFileAsync(filePath, onCompleted);
    }

    private void CancelActiveLoad(bool invokeCallback)
    {
        LoadRequestState requestState = activeLoadRequest;
        if (requestState == null)
        {
            return;
        }

        activeLoadRequest = null;
        requestState.isCancelled = true;
        requestState.invokeCallbackWhenCancelled = invokeCallback;
        requestState.request.Abort();
    }

#if UNITY_EDITOR
    private const string DemoFileName = "raw_text.json";
    private const string StreamingAssetsFolderName = "StreamingAssets";

    /// <summary>
    /// 示例原始文本 JSON 内容。
    /// </summary>
    private const string DemoJson =
@"{
    ""title"": ""原始文本示例"",
    ""content"": ""RawTextConfig 不解析字段，只保存完整文件文本。""
}";

    /// <summary>
    /// 在 StreamingAssets 目录下创建原始文本配置示例文件。
    /// </summary>
    [MenuItem("Cowart/配置模板/创建 raw_text.json 示例")]
    private static void CreateRawTextJsonDemo()
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
}
