using System;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
#endif

namespace Cowart.ConfigLoader
{
    /// <summary>
    /// 通用 JSON 配置加载器。
    /// 配置字段和 JSON 结构由调用方通过泛型类型自行定义。
    /// </summary>
    /// <typeparam name="T">使用 JsonUtility 反序列化的配置类型。</typeparam>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "JsonConfigLoader")]
    public sealed class JsonConfigLoader<T> where T : class
    {
        private readonly TextLoader textLoader = new TextLoader();

        /// <summary>
        /// 最近一次成功解析的配置数据。
        /// </summary>
        public T Config { get; private set; }

        /// <summary>
        /// 最近一次成功读取的原始 JSON 文本。
        /// </summary>
        public string RawContent => textLoader.Content;

        /// <summary>
        /// 当前是否正在加载配置文件。
        /// </summary>
        public bool IsLoading => textLoader.IsLoading;

        /// <summary>
        /// 读取并解析 JSON 配置内容。
        /// </summary>
        /// <param name="content">完整的 JSON 字符串。</param>
        /// <returns>解析成功返回 true，解析失败返回 false。</returns>
        public bool ReadJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Debug.LogError($"{typeof(T).Name} JSON 配置内容不能为空。");
                return false;
            }

            try
            {
                T config = JsonUtility.FromJson<T>(content.TrimStart('\uFEFF'));
                if (config == null)
                {
                    Debug.LogError($"解析 {typeof(T).Name} JSON 配置失败：解析结果为空。");
                    return false;
                }

                Config = config;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"解析 {typeof(T).Name} JSON 配置失败：{exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从指定地址异步读取并解析 JSON 配置。
        /// </summary>
        /// <param name="filePath">JSON 文件的完整路径或 URL。</param>
        /// <param name="onCompleted">读取完成后的回调，成功时参数为 true。</param>
        public void ReadJsonFileAsync(string filePath, Action<bool> onCompleted)
        {
            textLoader.LoadAsync(filePath, success =>
            {
                if (!success)
                {
                    onCompleted?.Invoke(false);
                    return;
                }

                onCompleted?.Invoke(ReadJson(textLoader.Content));
            });
        }

        /// <summary>
        /// 强制停止当前正在进行的配置加载。
        /// </summary>
        public void StopLoading()
        {
            textLoader.StopLoading();
        }

        /// <summary>
        /// 停止当前加载并重新读取指定的 JSON 配置。
        /// </summary>
        /// <param name="filePath">JSON 文件的完整路径或 URL。</param>
        /// <param name="onCompleted">重新加载完成后的回调，成功时参数为 true。</param>
        public void ReloadJsonFileAsync(string filePath, Action<bool> onCompleted)
        {
            ReadJsonFileAsync(filePath, onCompleted);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 创建可由 JsonConfigLoader 加载的通用 JSON 配置示例。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "JsonConfigDemoCreator")]
    internal static class JsonConfigDemoCreator
    {
        private const string DemoFileName = "game_config.json";
        private const string StreamingAssetsFolderName = "StreamingAssets";

        private const string DemoJson =
@"{
    ""title"": ""示例配置"",
    ""items"": [
        {
            ""id"": 1,
            ""name"": ""示例项目""
        }
    ]
}";

        /// <summary>
        /// 在 StreamingAssets 目录下创建通用 JSON 配置示例文件。
        /// </summary>
        [MenuItem("Cowart/配置模板/创建 game_config.json 示例")]
        private static void CreateGameConfigJsonDemo()
        {
            string folderPath = Path.Combine(Application.dataPath, StreamingAssetsFolderName);
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, DemoFileName);
            if (File.Exists(filePath))
            {
                Debug.LogWarning($"示例文件已存在，未覆盖：{filePath}");
                return;
            }

            File.WriteAllText(filePath, DemoJson, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log($"已创建示例文件：{filePath}");
        }
    }
#endif
}
