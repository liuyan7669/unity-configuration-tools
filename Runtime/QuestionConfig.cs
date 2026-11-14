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
    /// 选择题配置数据。
    /// 对应 StreamingAssets/questions.json 的根节点数据。
    /// </summary>
    /// <remarks>
    /// 序列化字段名与 questions.json 字段一一对应，不应随 C# 命名风格调整。
    /// </remarks>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "QuestionConfig")]
    public class QuestionConfig
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
        /// 选择题列表。
        /// </summary>
        [SerializeField]
        private List<QuestionData> questions = new List<QuestionData>();

        /// <summary>
        /// 获取只读的选择题列表。
        /// </summary>
        public IReadOnlyList<QuestionData> Questions
        {
            get
            {
                EnsureQuestions();
                return questions;
            }
        }

        /// <summary>
        /// 当前是否正在加载选择题配置。
        /// </summary>
        public bool IsLoading => Loader.IsLoading;

        /// <summary>
        /// 读取选择题 JSON 配置内容。
        /// </summary>
        /// <param name="content">questions.json 的完整 JSON 字符串。</param>
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
                QuestionConfig config = JsonUtility.FromJson<QuestionConfig>(content);
                questions = config?.questions ?? new List<QuestionData>();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Parse questions.json failed: {exception.Message}");
                questions = new List<QuestionData>();
                return false;
            }
        }

        /// <summary>
        /// 从指定地址异步读取并解析选择题 JSON 配置。
        /// </summary>
        /// <param name="filePath">JSON 文件的完整路径或 URL。</param>
        /// <param name="onCompleted">读取完成后的回调，成功时参数为 true。</param>
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

        /// <summary>
        /// 强制停止当前正在进行的选择题配置加载。
        /// </summary>
        public void StopLoading()
        {
            Loader.StopLoading();
        }

        /// <summary>
        /// 停止当前加载并重新读取指定的选择题配置。
        /// </summary>
        /// <param name="filePath">JSON 文件的完整路径或 URL。</param>
        /// <param name="onCompleted">重新加载完成后的回调，成功时参数为 true。</param>
        public void ReloadJsonFileAsync(string filePath, Action<bool> onCompleted)
        {
            ReadJsonFileAsync(filePath, onCompleted);
        }

        /// <summary>
        /// 获取选择题数量。
        /// </summary>
        public int Count => questions == null ? 0 : questions.Count;

        /// <summary>
        /// 尝试按题目编号获取选择题数据。
        /// </summary>
        /// <param name="id">题目编号。</param>
        /// <param name="question">找到的选择题数据。</param>
        /// <returns>找到匹配题目编号时返回 true，否则返回 false。</returns>
        public bool TryGetQuestionById(string id, out QuestionData question)
        {
            EnsureQuestions();

            for (int i = 0; i < questions.Count; i++)
            {
                if (questions[i] != null && questions[i].id == id)
                {
                    question = questions[i];
                    return true;
                }
            }

            question = null;
            return false;
        }

        /// <summary>
        /// 按题目编号设置当前得分。
        /// </summary>
        /// <param name="id">题目编号。</param>
        /// <param name="currentScore">当前得分。</param>
        /// <returns>设置成功返回 true，未找到题目时返回 false。</returns>
        public bool SetCurrentScoreById(string id, int currentScore)
        {
            if (!TryGetQuestionById(id, out QuestionData question))
            {
                return false;
            }

            question.currentScore = currentScore;
            return true;
        }

        /// <summary>
        /// 按题目编号获取当前得分。
        /// </summary>
        /// <param name="id">题目编号。</param>
        /// <param name="defaultValue">未找到题目时返回的默认分数。</param>
        /// <returns>找到题目时返回当前得分，否则返回默认分数。</returns>
        public int GetCurrentScoreById(string id, int defaultValue = 0)
        {
            return TryGetQuestionById(id, out QuestionData question)
                ? question.currentScore
                : defaultValue;
        }

        /// <summary>
        /// 按题目编号设置当前选择的答案。
        /// </summary>
        /// <param name="id">题目编号。</param>
        /// <param name="selectAnswer">当前选择的答案。</param>
        /// <returns>设置成功返回 true，未找到题目时返回 false。</returns>
        public bool SetSelectAnswerById(string id, string selectAnswer)
        {
            if (!TryGetQuestionById(id, out QuestionData question))
            {
                return false;
            }

            question.selectAnswer = selectAnswer;
            return true;
        }

        /// <summary>
        /// 按题目编号获取当前选择的答案。
        /// </summary>
        /// <param name="id">题目编号。</param>
        /// <param name="defaultValue">未找到题目时返回的默认答案。</param>
        /// <returns>找到题目时返回当前选择的答案，否则返回默认答案。</returns>
        public string GetSelectAnswerById(string id, string defaultValue = "")
        {
            return TryGetQuestionById(id, out QuestionData question)
                ? question.selectAnswer
                : defaultValue;
        }

        /// <summary>
        /// 按指定方式比较题目的标准答案和当前答案。
        /// </summary>
        /// <param name="id">题目编号。</param>
        /// <param name="comparisonMode">答案比较方式。</param>
        /// <returns>答案符合指定比较规则时返回 true，否则返回 false。</returns>
        public bool IsAnswerCorrectById(
            string id,
            AnswerComparisonMode comparisonMode = AnswerComparisonMode.UnorderedExact)
        {
            if (!TryGetQuestionById(id, out QuestionData question)
                || string.IsNullOrEmpty(question.standardAnswer)
                || string.IsNullOrEmpty(question.selectAnswer))
            {
                return false;
            }

            if (comparisonMode == AnswerComparisonMode.Exact)
            {
                return string.Equals(
                    question.standardAnswer,
                    question.selectAnswer,
                    StringComparison.Ordinal);
            }

            HashSet<char> standardAnswers = CreateAnswerSet(question.standardAnswer);
            HashSet<char> selectedAnswers = CreateAnswerSet(question.selectAnswer);
            if (standardAnswers.Count == 0 || selectedAnswers.Count == 0)
            {
                return false;
            }

            switch (comparisonMode)
            {
                case AnswerComparisonMode.UnorderedExact:
                    return standardAnswers.SetEquals(selectedAnswers);

                case AnswerComparisonMode.StandardSubset:
                    return selectedAnswers.IsSubsetOf(standardAnswers);

                default:
                    Debug.LogError($"不支持的答案比较方式：{comparisonMode}");
                    return false;
            }
        }

        /// <summary>
        /// 将答案字符串转换为单字符选项集合。
        /// </summary>
        private static HashSet<char> CreateAnswerSet(string answer)
        {
            HashSet<char> answers = new HashSet<char>();
            for (int i = 0; i < answer.Length; i++)
            {
                char value = answer[i];
                if (!char.IsWhiteSpace(value)
                    && value != ','
                    && value != '，'
                    && value != ';'
                    && value != '；'
                    && value != '|'
                    && value != '、')
                {
                    answers.Add(value);
                }
            }

            return answers;
        }

        private void EnsureQuestions()
        {
            if (questions == null)
            {
                questions = new List<QuestionData>();
            }
        }

#if UNITY_EDITOR
        private const string DemoFileName = "questions.json";
        private const string StreamingAssetsFolderName = "StreamingAssets";

        /// <summary>
        /// 示例选择题 JSON 内容。
        /// </summary>
        private const string DemoJson =
    @"{
    ""questions"": [
        {
            ""id"": ""Q001"",
            ""question"": ""示例题干：请选择正确答案。"",
            ""options"": [
                { ""text"": ""选项A"", ""standardScore"": 0, ""isCorrect"": false, ""analysis"": ""该选项不是正确答案。"" },
                { ""text"": ""选项B"", ""standardScore"": 10, ""isCorrect"": true, ""analysis"": ""该选项是正确答案。"" },
                { ""text"": ""选项C"", ""standardScore"": 0, ""isCorrect"": false, ""analysis"": ""该选项不是正确答案。"" },
                { ""text"": ""选项D"", ""standardScore"": 0, ""isCorrect"": false, ""analysis"": ""该选项不是正确答案。"" }
            ],
            ""standardScore"": 10,
            ""standardAnswer"": ""B"",
            ""analysis"": ""这里填写题目整体解析。"",
            ""notes"": ""这里填写备注说明。"",
            ""isGlobal"": false,
            ""currentScore"": 0,
            ""selectAnswer"": """"
        }
    ]
}";

        /// <summary>
        /// 在 StreamingAssets 目录下创建选择题配置示例文件。
        /// </summary>
        [MenuItem("Cowart/配置/模板/创建 questions.json 示例")]
        private static void CreateQuestionsJsonDemo()
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
    /// 选择题答案比较方式。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "AnswerComparisonMode")]
    public enum AnswerComparisonMode
    {
        /// <summary>
        /// 标准答案和当前答案的字符串完全一致。
        /// </summary>
        Exact,

        /// <summary>
        /// 忽略选项顺序后，两边包含的选项完全一致。
        /// </summary>
        UnorderedExact,

        /// <summary>
        /// 当前答案只能包含标准答案中的选项，但允许少选。
        /// </summary>
        StandardSubset
    }

    /// <summary>
    /// 单道选择题数据。
    /// </summary>
    /// <remarks>
    /// 公共字段名属于 questions.json 的序列化契约。
    /// </remarks>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "QuestionData")]
    public class QuestionData
    {
        /// <summary>
        /// 题目编号。
        /// </summary>
        public string id;

        /// <summary>
        /// 题干内容。
        /// </summary>
        public string question;

        /// <summary>
        /// 选项列表。
        /// </summary>
        public List<QuestionOptionData> options = new List<QuestionOptionData>();

        /// <summary>
        /// 题目标准分。
        /// </summary>
        public int standardScore;

        /// <summary>
        /// 标准答案。
        /// </summary>
        public string standardAnswer;

        /// <summary>
        /// 题目解析。
        /// </summary>
        public string analysis;

        /// <summary>
        /// 备注说明。
        /// </summary>
        public string notes;

        /// <summary>
        /// 是否为全局题目。
        /// </summary>
        public bool isGlobal;

        /// <summary>
        /// 当前得分。
        /// </summary>
        public int currentScore;

        /// <summary>
        /// 当前选择的答案。
        /// </summary>
        public string selectAnswer;
    }

    /// <summary>
    /// 选择题选项数据。
    /// </summary>
    /// <remarks>
    /// 公共字段名属于 questions.json 的序列化契约。
    /// </remarks>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.ConfigurationTools", sourceAssembly: "Cowart.ConfigurationTools", sourceClassName: "QuestionOptionData")]
    public class QuestionOptionData
    {
        /// <summary>
        /// 选项文本。
        /// </summary>
        public string text;

        /// <summary>
        /// 选项标准分。
        /// </summary>
        public int standardScore;

        /// <summary>
        /// 该选项是否为正确答案。
        /// </summary>
        public bool isCorrect;

        /// <summary>
        /// 选项解析。
        /// </summary>
        public string analysis;
    }
}
