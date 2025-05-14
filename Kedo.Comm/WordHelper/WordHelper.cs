using Aspose.Words;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.WordHelper
{
    public class WordHelper
    {
        private readonly ILogger<WordHelper> _logger;

        private readonly IConfiguration _configuration;

        private readonly string _FileSavePath;
        private readonly string _FileUrl;
        public WordHelper(ILogger<WordHelper> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _FileSavePath = configuration["GPTDataFile:FileSavePath"];
            _FileUrl = configuration["GPTDataFile:FileUrl"];

            // License license = new License();

            //  license.SetLicense("");

            LoadAsposeLicense();
        }

        private void LoadAsposeLicense()
        {
            var licensePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Aspose.Total.NET.txt");
            if (File.Exists(licensePath))
            {
                try
                {
                    // 设置Aspose.Cells的许可证
                    var license = new Aspose.Words.License();
                    license.SetLicense(licensePath);
                    _logger.Log(LogLevel.Information, "Apose 组件注册成功！");
                }
                catch (Exception ex)
                {
                    // 处理许可证加载失败的情况
                    Console.WriteLine("Failed to load Aspose.Total license: " + ex.Message);

                    _logger.Log(LogLevel.Information, "Failed to load Aspose.Total license: " + ex.Message);
                }
            }
            else
            {
                // 处理许可证文件不存在的情况
                Console.WriteLine("Aspose.Total license file not found at: " + licensePath);

                _logger.Log(LogLevel.Information, "Aspose.Total license file not found at: " + licensePath);
            }
        }

        public string htmlToWord(string htmlcontent, string title)
        {
            try
            {
                Document doc = new Document();
                DocumentBuilder builder = new DocumentBuilder(doc);
                // 将Base64字符串转换为字节数组
               // byte[] base64Bytes = Convert.FromBase64String(htmlcontent);
                // 将字节数组解码为字符串
               // string htmlContents = System.Text.Encoding.UTF8.GetString(base64Bytes);
                builder.InsertHtml(htmlcontent);
                string fileSerialNo = DateTime.Now.ToString("yyyyMMdd");
                // 如：保存到网站根目录下的 uploads 目录
                var savePath = _FileSavePath + "\\" + fileSerialNo;
                doc.Save(savePath + "\\" + title + ".docx", SaveFormat.Docx);
                string fileUrl = _FileUrl + "/" + fileSerialNo + "/" + title + ".docx";
                return fileUrl;

            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

    }

}

