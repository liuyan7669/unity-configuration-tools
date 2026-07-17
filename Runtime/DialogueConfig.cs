using System;
using System.Collections.Generic;
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
    /// 对话配置数据。
    /// 对应 StreamingAssets/dialogues.json 的根节点数据。
    /// </summary>
    /// <remarks>
    /// 序列化字段名与 JSON 字段一一对应，不应随 C# 命名风格调整。
    /// </remarks>
    [Serializable]
    public class DialogueConfig
    {
        private sealed class LoadRequestState
        {
            public UnityWebRequest request;
            public bool isCancelled;
            public bool invokeCallbackWhenCancelled;
        }

        private LoadRequestState activeLoadRequest;

        /// <summary>
        /// 对话表列表，每张表通过 tableName 标识使用范围。
        /// </summary>
        [SerializeField]
        private List<DialogueTableData> tables = new List<DialogueTableData>();

        /// <summary>
        /// 获取只读的对话表列表。
        /// </summary>
        public IReadOnlyList<DialogueTableData> Tables
        {
            get
            {
                EnsureTables();
                return tables;
            }
        }

        /// <summary>
        /// 当前是否正在加载对话配置。
        /// </summary>
        public bool IsLoading => activeLoadRequest != null;

        /// <summary>
        /// 读取对话 JSON 配置内容。
        /// </summary>
        /// <param name="content">dialogues.json 的完整 JSON 字符串。</param>
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
                DialogueConfig config = JsonUtility.FromJson<DialogueConfig>(content);
                tables = config?.tables ?? new List<DialogueTableData>();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Parse dialogues.json failed: {exception.Message}");
                tables = new List<DialogueTableData>();
                return false;
            }
        }

        /// <summary>
        /// 从指定地址读取并解析对话 JSON 配置。
        /// </summary>
        /// <param name="filePath">JSON 文件的完整路径或 URL。</param>
        /// <param name="onCompleted">读取完成后的回调，成功时参数为 true。</param>
        public void ReadJsonFileAsync(string filePath, Action<bool> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.LogError("对话 JSON 文件路径不能为空。");
                onCompleted?.Invoke(false);
                return;
            }

            CancelActiveLoad(false);

            string requestPath = filePath.Contains("://") || filePath.StartsWith("jar:")
                ? filePath
                : new Uri(filePath).AbsoluteUri;

            UnityWebRequest request = UnityWebRequest.Get(requestPath);
            LoadRequestState requestState = new LoadRequestState
            {
                request = request
            };
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
                        Debug.LogError($"读取对话 JSON 文件失败：{request.error}\n{filePath}");
                        onCompleted?.Invoke(false);
                        return;
                    }

                    onCompleted?.Invoke(ReadJson(request.downloadHandler.text));
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

        /// <summary>
        /// 强制停止当前正在进行的对话配置加载。
        /// </summary>
        public void StopLoading()
        {
            CancelActiveLoad(true);
        }

        /// <summary>
        /// 停止当前加载并重新读取指定的对话配置。
        /// </summary>
        /// <param name="filePath">JSON 文件的完整路径或 URL。</param>
        /// <param name="onCompleted">重新加载完成后的回调，成功时参数为 true。</param>
        public void ReloadJsonFileAsync(string filePath, Action<bool> onCompleted)
        {
            ReadJsonFileAsync(filePath, onCompleted);
        }

        /// <summary>
        /// 取消当前加载。
        /// </summary>
        /// <param name="invokeCallback">取消后是否调用当前加载的完成回调。</param>
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

        /// <summary>
        /// 获取对话表数量。
        /// </summary>
        public int Count => tables == null ? 0 : tables.Count;

        /// <summary>
        /// 按使用范围标识获取对话表数据。
        /// </summary>
        /// <param name="tableName">使用范围标识。</param>
        /// <returns>找到时返回对话表数据，否则返回 null。</returns>
        public DialogueTableData GetTableByName(string tableName)
        {
            TryGetTableByName(tableName, out DialogueTableData table);
            return table;
        }

        /// <summary>
        /// 尝试按使用范围标识获取对话表数据。
        /// </summary>
        /// <param name="tableName">使用范围标识。</param>
        /// <param name="table">找到的对话表数据。</param>
        /// <returns>找到匹配使用范围标识时返回 true，否则返回 false。</returns>
        public bool TryGetTableByName(string tableName, out DialogueTableData table)
        {
            EnsureTables();

            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] != null && tables[i].tableName == tableName)
                {
                    table = tables[i];
                    return true;
                }
            }

            table = null;
            return false;
        }

        /// <summary>
        /// 按使用范围标识获取对话列表。
        /// </summary>
        /// <param name="tableName">使用范围标识。</param>
        /// <returns>找到时返回该范围下的对话列表，否则返回空列表。</returns>
        public List<DialogueItemData> GetDialoguesByTableName(string tableName)
        {
            if (TryGetTableByName(tableName, out DialogueTableData table))
            {
                return table.GetDialogues();
            }

            return new List<DialogueItemData>();
        }

        /// <summary>
        /// 确保对话表列表不为空引用。
        /// </summary>
        private void EnsureTables()
        {
            if (tables == null)
            {
                tables = new List<DialogueTableData>();
            }
        }

#if UNITY_EDITOR
        private const string DemoFileName = "dialogues.json";
        private const string StreamingAssetsFolderName = "StreamingAssets";

        /// <summary>
        /// 示例对话 JSON 内容。
        /// </summary>
        private const string DemoJson =
    @"{
    ""tables"": [
        {
            ""tableName"": ""Opening"",
            ""dialogues"": [
                {
                    ""name"": ""教师"",
                    ""speaker"": ""主角"",
                    ""content"": ""欢迎来到文商精神探秘的世界！"",
                    ""audioPath"": """",
                    ""idleAnimation"": ""Idle"",
                    ""talkAnimation"": ""Talk"",
                    ""isPlayer"": true
                },
                {
                    ""name"": ""教师"",
                    ""speaker"": ""教师"",
                    ""content"": ""请跟随提示完成接下来的探索。"",
                    ""audioPath"": """",
                    ""idleAnimation"": ""Idle"",
                    ""talkAnimation"": ""Talk"",
                    ""isPlayer"": false
                }
            ]
        },
        {
            ""tableName"": ""Scene_001"",
            ""dialogues"": [
                {
                    ""name"": ""学生"",
                    ""speaker"": ""学生"",
                    ""content"": ""这里就是第一个探索场景吗？"",
                    ""audioPath"": """",
                    ""idleAnimation"": ""Idle"",
                    ""talkAnimation"": ""Talk"",
                    ""isPlayer"": true
                }
            ]
        }
    ]
}";

        /// <summary>
        /// 在 StreamingAssets 目录下创建对话配置示例文件。
        /// </summary>
        [MenuItem("Tools/配置示例/创建 dialogues.json 示例")]
        private static void CreateDialoguesJsonDemo()
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
    /// 单张对话表数据。
    /// </summary>
    /// <remarks>
    /// 公共字段名属于 dialogues.json 的序列化契约。
    /// </remarks>
    [Serializable]
    public class DialogueTableData
    {
        /// <summary>
        /// 使用范围标识，不同对话表之间不重复。
        /// </summary>
        public string tableName;

        /// <summary>
        /// 当前使用范围下的对话列表。
        /// </summary>
        public List<DialogueItemData> dialogues = new List<DialogueItemData>();

        /// <summary>
        /// 获取当前对话表中的全部对话。
        /// </summary>
        /// <returns>对话列表。</returns>
        public List<DialogueItemData> GetDialogues()
        {
            if (dialogues == null)
            {
                dialogues = new List<DialogueItemData>();
            }

            return dialogues;
        }
    }

    /// <summary>
    /// 单条对话数据。
    /// </summary>
    /// <remarks>
    /// 公共字段名属于 dialogues.json 的序列化契约。
    /// </remarks>
    [Serializable]
    public class DialogueItemData
    {
        /// <summary>
        /// 对外展示的名字，用于 UI 界面。
        /// </summary>
        public string name;

        /// <summary>
        /// 人物模型物体的名字，用于定位说话模型。
        /// </summary>
        public string speaker;

        /// <summary>
        /// 显示的文字内容。
        /// </summary>
        public string content;

        /// <summary>
        /// 对话音频路径。
        /// </summary>
        public string audioPath;

        /// <summary>
        /// 未说话或说话完成时播放的动画。
        /// </summary>
        public string idleAnimation;

        /// <summary>
        /// 播放音频及文字时播放的动画。
        /// </summary>
        public string talkAnimation;

        /// <summary>
        /// 当前对话是否是主角在说。
        /// </summary>
        public bool isPlayer;
    }
}
