using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cowart.ConfigLoader.Tests
{
    public sealed class ConfigLoaderTests
    {
        [Serializable]
        private sealed class SampleConfig
        {
            public string title;
            public int count;
        }

        [Test]
        public void TextLoader_ReadText_PreservesRawContent()
        {
            TextLoader loader = new TextLoader();
            const string content = "line 1\nline 2\t{}";

            Assert.IsTrue(loader.ReadText(content));
            Assert.AreEqual(content, loader.Content);
            Assert.IsTrue(loader.ReadText(string.Empty));
            Assert.AreEqual(string.Empty, loader.Content);
        }

        [UnityTest]
        public IEnumerator TextLoader_LoadAsync_ReadsArbitraryTextFile()
        {
            string path = Path.Combine(Application.temporaryCachePath, "cowart-text-loader.data");
            const string content = "arbitrary text\n第二行";
            File.WriteAllText(path, content, new UTF8Encoding(false));

            try
            {
                TextLoader loader = new TextLoader();
                bool? succeeded = null;
                loader.LoadAsync(path, result => succeeded = result);

                yield return WaitForLoader(loader);

                Assert.AreEqual(true, succeeded);
                Assert.AreEqual(content, loader.Content);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [UnityTest]
        public IEnumerator JsonConfigLoader_LoadsThroughTextLoader()
        {
            string path = Path.Combine(Application.temporaryCachePath, "cowart-json-loader.json");
            const string content = "{\"title\":\"Demo\",\"count\":3}";
            File.WriteAllText(path, content, new UTF8Encoding(false));

            try
            {
                JsonConfigLoader<SampleConfig> loader = new JsonConfigLoader<SampleConfig>();
                bool? succeeded = null;
                loader.ReadJsonFileAsync(path, result => succeeded = result);

                yield return WaitForLoader(loader);

                Assert.AreEqual(true, succeeded);
                Assert.AreEqual(content, loader.RawContent);
                Assert.IsNotNull(loader.Config);
                Assert.AreEqual("Demo", loader.Config.title);
                Assert.AreEqual(3, loader.Config.count);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static IEnumerator WaitForLoader(TextLoader loader)
        {
            float timeout = Time.realtimeSinceStartup + 5f;
            while (loader.IsLoading && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.IsFalse(loader.IsLoading, "TextLoader timed out.");
        }

        private static IEnumerator WaitForLoader<T>(JsonConfigLoader<T> loader)
            where T : class
        {
            float timeout = Time.realtimeSinceStartup + 5f;
            while (loader.IsLoading && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.IsFalse(loader.IsLoading, "JsonConfigLoader timed out.");
        }
    }
}
