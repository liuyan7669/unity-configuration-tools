using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Cowart.ConfigLoader
{
    /// <summary>
    /// 从本地路径或 URL 异步读取原始文本内容。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "TextLoader")]
    public sealed class TextLoader
    {
        private sealed class LoadRequestState
        {
            public UnityWebRequest request;
            public bool isCancelled;
            public bool invokeCallbackWhenCancelled;
        }

        private LoadRequestState activeLoadRequest;

        /// <summary>
        /// 最近一次成功设置或读取的原始文本内容。
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// 当前是否正在读取文本。
        /// </summary>
        public bool IsLoading => activeLoadRequest != null;

        /// <summary>
        /// 直接设置原始文本内容。空字符串属于有效文本，只有 null 会失败。
        /// </summary>
        /// <param name="content">原始文本内容。</param>
        /// <returns>设置成功返回 true；内容为 null 时返回 false。</returns>
        public bool ReadText(string content)
        {
            if (content == null)
            {
                Debug.LogError("文本内容不能为 null。");
                return false;
            }

            Content = content;
            return true;
        }

        /// <summary>
        /// 从本地完整路径或 URL 异步读取文本。
        /// </summary>
        /// <param name="filePath">文件完整路径或 URL。</param>
        /// <param name="onCompleted">读取完成回调，成功时参数为 true。</param>
        public void LoadAsync(string filePath, Action<bool> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.LogError("文本文件路径不能为空。");
                onCompleted?.Invoke(false);
                return;
            }

            CancelActiveLoad(false);

            string requestPath;
            try
            {
                requestPath = filePath.Contains("://") ||
                              filePath.StartsWith("jar:", StringComparison.OrdinalIgnoreCase)
                    ? filePath
                    : new Uri(filePath).AbsoluteUri;
            }
            catch (Exception exception)
            {
                Debug.LogError($"文本文件路径无效：{exception.Message}\n{filePath}");
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
                        Debug.LogError($"读取文本文件失败：{request.error}\n{filePath}");
                        onCompleted?.Invoke(false);
                        return;
                    }

                    Content = request.downloadHandler.text;
                    onCompleted?.Invoke(true);
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
        /// 停止当前读取并重新读取指定文本。
        /// </summary>
        public void ReloadAsync(string filePath, Action<bool> onCompleted)
        {
            LoadAsync(filePath, onCompleted);
        }

        /// <summary>
        /// 强制停止当前正在进行的文本读取，并以失败结果结束当前回调。
        /// </summary>
        public void StopLoading()
        {
            CancelActiveLoad(true);
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
