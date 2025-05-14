//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Kedo.rabbitMQ.BIData.Utils
//{
//    public class FileReadHandleUtil
//    {

//        public async void WordHandle(string filePath, string fileId)
//        {




//            EmbeddingInput embeddingInput = new EmbeddingInput();
//            embeddingInput.Texts = result["data"]?.ToString() ?? string.Empty; ;
//            embeddingInput.DocId = fileId;
//            HttpEmbeddings httpEmbeddings = new HttpEmbeddings();
//            await httpEmbeddings.EmbeddingsContent(embeddingInput);
//        }
//    }
//}
using System;
using System.IO;
using Aspose.Words;
using System.Text;

namespace Kedo.rabbitMQ.BIData.Utils
{
    /// <summary>
    /// 文件读取器类，支持读取Word文档和文本文件
    /// </summary>
    public class FileReaderUtil
    {
        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容字符串</returns>
        public string ReadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("找不到指定的文件", filePath);

            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".doc":
                case ".docx":
                    return ReadWordDocument(filePath);
                case ".txt":
                    return ReadTextFile(filePath);
                default:
                    throw new NotSupportedException($"不支持的文件类型: {extension}");
            }
        }

        /// <summary>
        /// 使用Aspose.Words读取Word文档(.doc, .docx)
        /// </summary>
        /// <param name="filePath">Word文档路径</param>
        /// <returns>文档内容字符串</returns>
        private string ReadWordDocument(string filePath)
        {
            try
            {
                // 加载Word文档
                Document doc = new Document(filePath);

                // 将Word文档转换为文本
                string text = doc.ToString(SaveFormat.Text);

                return text;
            }
            catch (Exception ex)
            {
                throw new Exception($"读取Word文档时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 读取文本文件(.txt)
        /// </summary>
        /// <param name="filePath">文本文件路径</param>
        /// <returns>文件内容字符串</returns>
        private string ReadTextFile(string filePath)
        {
            try
            {
                // 使用StreamReader读取文本文件
                using (StreamReader reader = new StreamReader(filePath, Encoding.Default))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"读取文本文件时发生错误: {ex.Message}", ex);
            }
        }
    }
}