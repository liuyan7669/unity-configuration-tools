using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Cowart.ConfigurationTools
{
    /// <summary>
    /// 通用 JSON 配置加载器。
    /// 配置字段和 JSON 结构由调用方通过泛型类型自行定义。
    /// </summary>
    /// <typeparam name="T">使用 JsonUtility 反序列化的配置类型。</typeparam>
    public sealed class JsonConfigLoader<T> where T : class
    {
        private sealed class LoadRequestState
        {
            public UnityWebRequest request;
            public bool isCancelled;
            public bool invokeCallbackWhenCancelled;
        }

        private LoadRequestState activeLoadRequest;

        /// <summary>
        /// 最近一次成功解析的配置数据。
        /// </summary>
        public T Config { get; private set; }

        /// <summary>
        /// 当前是否正在加载配置文件。
        /// </summary>
        public bool IsLoading => activeLoadRequest != null;

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
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.LogError($"{typeof(T).Name} JSON 配置文件路径不能为空。");
                onCompleted?.Invoke(false);
                return;
            }

            CancelActiveLoad(false);

            string requestPath;
            try
            {
                requestPath = filePath.Contains("://") || filePath.StartsWith("jar:")
                    ? filePath
                    : new Uri(filePath).AbsoluteUri;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{typeof(T).Name} JSON 配置文件路径无效：{exception.Message}\n{filePath}");
                onCompleted?.Invoke(false);
                return;
            }

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
                        Debug.LogError($"读取 {typeof(T).Name} JSON 配置文件失败：{request.error}\n{filePath}");
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
        /// 强制停止当前正在进行的配置加载。
        /// </summary>
        public void StopLoading()
        {
            CancelActiveLoad(true);
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
    }
}
