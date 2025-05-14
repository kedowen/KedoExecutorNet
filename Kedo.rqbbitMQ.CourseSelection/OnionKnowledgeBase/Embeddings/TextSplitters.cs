using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kedo.rabbitMQ.BIData.OnionKnowledgeBase.Embeddings
{
    /// <summary>
    /// 文本分割器的抽象基类
    /// </summary>
    public abstract class TextSplitter
    {
        /// <summary>
        /// 分割块的最大字符数
        /// </summary>
        protected readonly int ChunkSize;

        /// <summary>
        /// 分割块之间的重叠字符数
        /// </summary>
        protected readonly int ChunkOverlap;

        /// <summary>
        /// 初始化TextSplitter
        /// </summary>
        /// <param name="chunkSize">每个文本块的最大字符数</param>
        /// <param name="chunkOverlap">相邻文本块之间的重叠字符数</param>
        protected TextSplitter(int chunkSize = 1000, int chunkOverlap = 200)
        {
            if (chunkOverlap >= chunkSize)
            {
                throw new ArgumentException("Chunk overlap must be less than chunk size");
            }

            ChunkSize = chunkSize;
            ChunkOverlap = chunkOverlap;
        }

        /// <summary>
        /// 将文本分割成多个块
        /// </summary>
        /// <param name="text">要分割的文本</param>
        /// <returns>分割后的文本块列表</returns>
        public abstract List<string> SplitText(string text);

        /// <summary>
        /// 将多个文本分割成多个块
        /// </summary>
        /// <param name="texts">要分割的文本列表</param>
        /// <returns>分割后的文本块列表</returns>
        public List<string> SplitTexts(List<string> texts)
        {
            List<string> result = new List<string>();
            foreach (var text in texts)
            {
                result.AddRange(SplitText(text));
            }
            return result;
        }

        /// <summary>
        /// 合并小块文本，确保不超过最大块大小
        /// </summary>
        /// <param name="splits">要合并的文本块</param>
        /// <returns>合并后的文本块</returns>
        protected List<string> MergeSplits(List<string> splits)
        {
            List<string> result = new List<string>();
            StringBuilder currentDoc = new StringBuilder();
            int currentLength = 0;

            foreach (var split in splits)
            {
                int splitLength = split.Length;

                if (currentLength + splitLength > ChunkSize)
                {
                    if (currentDoc.Length > 0)
                    {
                        result.Add(currentDoc.ToString());
                        // 如果有重叠，保留最后的部分
                        if (ChunkOverlap > 0 && currentDoc.Length > ChunkOverlap)
                        {
                            int overlapStart = Math.Max(0, currentDoc.Length - ChunkOverlap);
                            currentDoc = new StringBuilder(currentDoc.ToString().Substring(overlapStart));
                            currentLength = currentDoc.Length;
                        }
                        else
                        {
                            currentDoc = new StringBuilder();
                            currentLength = 0;
                        }
                    }
                }

                if (splitLength > ChunkSize)
                {
                    // 如果单个分割已超过最大块大小，直接添加
                    result.Add(split);
                }
                else
                {
                    currentDoc.Append(split);
                    currentLength += splitLength;
                }
            }

            // 添加最后一个块
            if (currentDoc.Length > 0)
            {
                result.Add(currentDoc.ToString());
            }

            return result;
        }
    }

    /// <summary>
    /// 递归字符文本分割器，按照指定的分隔符层次递归分割文本
    /// </summary>
    public class RecursiveCharacterTextSplitter : TextSplitter
    {
        /// <summary>
        /// 分隔符列表，按优先级排序
        /// </summary>
        private readonly List<string> Separators;

        /// <summary>
        /// 是否保留分隔符在结果中
        /// </summary>
        private readonly bool KeepSeparator;

        /// <summary>
        /// 初始化递归字符文本分割器
        /// </summary>
        /// <param name="separators">分隔符列表，按优先级排序</param>
        /// <param name="keepSeparator">是否在分割后的文本中保留分隔符</param>
        /// <param name="chunkSize">每个文本块的最大字符数</param>
        /// <param name="chunkOverlap">相邻文本块之间的重叠字符数</param>
        public RecursiveCharacterTextSplitter(
            List<string> separators = null,
            bool keepSeparator = true,
            int chunkSize = 1000,
            int chunkOverlap = 200)
            : base(chunkSize, chunkOverlap)
        {
            // 默认分隔符，按优先级从高到低
            Separators = separators ?? new List<string>
            {
                "\n\n", // 段落
                "\n",   // 换行
                " ",    // 空格
                "",     // 单个字符
            };
            KeepSeparator = keepSeparator;
        }

        /// <summary>
        /// 使用指定分隔符分割文本
        /// </summary>
        /// <param name="text">要分割的文本</param>
        /// <param name="separator">分隔符</param>
        /// <returns>分割后的文本列表</returns>
        private List<string> SplitTextWithSeparator(string text, string separator)
        {
            if (string.IsNullOrEmpty(separator))
            {
                // 如果分隔符为空，则按单个字符分割
                return text.Select(c => c.ToString()).ToList();
            }

            // 使用正则表达式分割，以便处理特殊字符
            string pattern = Regex.Escape(separator);
            string[] splits = Regex.Split(text, $"({pattern})");

            List<string> result = new List<string>();
            for (int i = 0; i < splits.Length; i++)
            {
                if (i % 2 == 0)
                {
                    // 非分隔符部分
                    if (!string.IsNullOrEmpty(splits[i]))
                    {
                        result.Add(splits[i]);
                    }
                }
                else
                {
                    // 分隔符部分
                    if (KeepSeparator)
                    {
                        // 将分隔符添加到前一个片段
                        if (result.Count > 0)
                        {
                            result[result.Count - 1] += splits[i];
                        }
                        else
                        {
                            result.Add(splits[i]);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 递归地将文本分割成块
        /// </summary>
        /// <param name="text">要分割的文本</param>
        /// <param name="separatorIndex">当前使用的分隔符索引</param>
        /// <returns>分割后的文本块</returns>
        private List<string> SplitTextRecursively(string text, int separatorIndex = 0)
        {
            // 如果文本长度小于块大小，直接返回
            if (text.Length <= ChunkSize)
            {
                return new List<string> { text };
            }

            // 如果已经尝试了所有分隔符，则按最后一个分隔符分割
            if (separatorIndex >= Separators.Count)
            {
                return SplitTextWithSeparator(text, Separators.Last());
            }

            // 使用当前分隔符分割
            List<string> splits = SplitTextWithSeparator(text, Separators[separatorIndex]);

            // 如果分割后只有一个元素，且它等于原文本，尝试下一个分隔符
            if (splits.Count == 1 && splits[0] == text)
            {
                return SplitTextRecursively(text, separatorIndex + 1);
            }

            // 对每个分割后的文本块递归应用分割
            List<string> result = new List<string>();
            foreach (var split in splits)
            {
                if (split.Length <= ChunkSize)
                {
                    result.Add(split);
                }
                else
                {
                    // 递归分割过长的文本块
                    result.AddRange(SplitTextRecursively(split, separatorIndex + 1));
                }
            }

            return result;
        }

        /// <summary>
        /// 将文本分割成多个块
        /// </summary>
        /// <param name="text">要分割的文本</param>
        /// <returns>分割后的文本块列表</returns>
        public override List<string> SplitText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            // 递归分割文本
            List<string> splits = SplitTextRecursively(text);

            // 合并分割后的文本块，确保不超过最大块大小并应用重叠
            return MergeSplits(splits);
        }

        /// <summary>
        /// 为特定编程语言创建优化的分割器
        /// </summary>
        /// <param name="language">编程语言</param>
        /// <returns>针对该语言优化的分割器</returns>
        public static RecursiveCharacterTextSplitter CreateForLanguage(string language, int chunkSize = 1000, int chunkOverlap = 200)
        {
            Dictionary<string, List<string>> languageSeparators = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "csharp", new List<string> {
                        "\n\n", "\n", ";", "}", "{", "]", "[", ")", "(", " ", ""
                    }
                },
                {
                    "python", new List<string> {
                        "\n\n", "\n", "    ", ".", ",", " ", ""
                    }
                },
                {
                    "javascript", new List<string> {
                        "\n\n", "\n", ";", "}", "{", "]", "[", ")", "(", " ", ""
                    }
                },
                {
                    "html", new List<string> {
                        "\n\n", "\n", ">", "}", "{", "]", "[", ")", "(", " ", ""
                    }
                }
                // 可以添加更多语言的分隔符规则
            };

            if (languageSeparators.TryGetValue(language, out var separators))
            {
                return new RecursiveCharacterTextSplitter(separators, true, chunkSize, chunkOverlap);
            }

            // 默认分隔符
            return new RecursiveCharacterTextSplitter(chunkSize: chunkSize, chunkOverlap: chunkOverlap);
        }
    }
}
