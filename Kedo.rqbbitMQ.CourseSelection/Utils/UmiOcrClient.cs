using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Kedo.rabbitMQ.BIData.Utils;

public class UmiOcrClient
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly IConfiguration _configuration;
    public string mFileId = "";

    public UmiOcrClient(IConfiguration configuration)
    {
        _client = new HttpClient();
        _configuration = configuration;
        _baseUrl = configuration["OCR:OCRUrl"]?.ToString() ?? string.Empty;
    }

    // 图片OCR
    public async Task<string> RecognizeImageAsync(string base64Image)
    {
        // 使用字典而不是匿名类型
        var options = new Dictionary<string, string>
        {
            { "ocr.language", "简体中文" },
            { "tbpu.parser", "multi_para" },
            { "data.format", "text" }
        };

        var payload = new Dictionary<string, object>
        {
            { "base64", base64Image },
            { "options", options }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync($"{_baseUrl}/api/ocr", content);
        return await response.Content.ReadAsStringAsync();
    }

    // PDF OCR

    public async Task<string> RecognizePdfAsync(string filePath)
    {
        string newFilePath = CreateNewFile(filePath);
        int maxRetries = 5;
        int baseDelayMs = 1000; // 初始延迟1秒

        // 使用MultipartFormDataContent上传PDF文件
        using (var formData = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            formData.Add(fileContent, "file", Path.GetFileName(filePath));

            // 添加OCR选项
            var options = new Dictionary<string, string>
        {
            { "doc.extractionMode", "mixed" },
            { "tbpu.parser", "multi_para" }
        };

            formData.Add(new StringContent(JsonConvert.SerializeObject(options)), "json");

            // 上传文件并获取任务ID - 添加重试机制
            string taskId = null;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var response = await _client.PostAsync($"{_baseUrl}/api/doc/upload", formData);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"上传失败，HTTP状态码: {response.StatusCode}，正在重试 ({attempt + 1}/{maxRetries})");
                        await Task.Delay(baseDelayMs * (int)Math.Pow(2, attempt)); // 指数退避
                        continue;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    taskId = responseJson["data"]?.ToString();

                    if (!string.IsNullOrEmpty(taskId))
                        break;

                    Console.WriteLine($"获取任务ID失败，正在重试 ({attempt + 1}/{maxRetries})");
                    await Task.Delay(baseDelayMs * (int)Math.Pow(2, attempt));
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"网络错误: {ex.Message}，正在重试 ({attempt + 1}/{maxRetries})");
                    await Task.Delay(baseDelayMs * (int)Math.Pow(2, attempt));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"未预期的错误: {ex.Message}，正在重试 ({attempt + 1}/{maxRetries})");
                    await Task.Delay(baseDelayMs * (int)Math.Pow(2, attempt));
                }
            }

            if (string.IsNullOrEmpty(taskId))
            {
                throw new Exception("上传PDF文件失败，已达到最大重试次数");
            }

            // 轮询获取结果 - 添加重试机制
            var payload = new Dictionary<string, object>
        {
            { "id", taskId },
            { "is_data", true },
            { "format", "text" }
        };

            int pollAttempt = 0;
            int maxPollRetries = 10; // 轮询最大重试次数

            while (pollAttempt < maxPollRetries)
            {
                try
                {
                    var content = new StringContent(
                        JsonConvert.SerializeObject(payload),
                        Encoding.UTF8,
                        "application/json");

                    var responseData = await _client.PostAsync($"{_baseUrl}/api/doc/result", content);

                    if (!responseData.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"获取结果失败，HTTP状态码: {responseData.StatusCode}，正在重试 ({pollAttempt + 1}/{maxPollRetries})");
                        await Task.Delay(baseDelayMs * (int)Math.Pow(1.5, pollAttempt)); // 轻微指数退避
                        pollAttempt++;
                        continue;
                    }

                    var responseContent = await responseData.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                    // 写入结果到文件
                    if (File.Exists(newFilePath))
                    {
                        using (StreamWriter writer = new StreamWriter(newFilePath, true))
                        {
                            writer.WriteLine(result["data"]?.ToString() ?? string.Empty);
                            Console.WriteLine("内容已成功追加到文件中。");
                        }
                    }
                    else
                    {
                        Console.WriteLine("文件不存在。");
                    }

                    // 检查任务是否完成
                    if (Convert.ToBoolean(result["is_done"]))
                        return newFilePath;

                    // 如果任务未完成，正常轮询间隔
                    await Task.Delay(1000);
                    pollAttempt = 0; // 成功获取结果后重置重试计数
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"轮询网络错误: {ex.Message}，正在重试 ({pollAttempt + 1}/{maxPollRetries})");
                    await Task.Delay(baseDelayMs * (int)Math.Pow(2, pollAttempt));
                    pollAttempt++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"轮询未预期的错误: {ex.Message}，正在重试 ({pollAttempt + 1}/{maxPollRetries})");
                    await Task.Delay(baseDelayMs * (int)Math.Pow(2, pollAttempt));
                    pollAttempt++;
                }
            }

            throw new Exception("获取OCR结果失败，已达到最大轮询重试次数");
        }
    }
    //public async Task<string> RecognizePdfAsync(string filePath)
    //{

    //    string newFilePath = CreateNewFile(filePath);

    //    // 使用MultipartFormDataContent上传PDF文件
    //    using (var formData = new MultipartFormDataContent())
    //    {
    //        var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
    //        formData.Add(fileContent, "file", Path.GetFileName(filePath));

    //        // 添加OCR选项 - 使用字典
    //        var options = new Dictionary<string, string>
    //        {
    //            { "doc.extractionMode", "mixed" },
    //            { "tbpu.parser", "multi_para" }
    //        };

    //        formData.Add(new StringContent(JsonConvert.SerializeObject(options)), "json");

    //        var response = await _client.PostAsync($"{_baseUrl}/api/doc/upload", formData);
    //        var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
    //        var taskId = responseJson["data"].ToString();

    //        if (!string.IsNullOrEmpty(taskId))
    //        {
    //            // 轮询获取结果 - 使用字典
    //            var payload = new Dictionary<string, object>
    //    {
    //        { "id", taskId },
    //        { "is_data", true },
    //        { "format", "text" }
    //    };

    //            while (true)
    //            {
    //                var content = new StringContent(
    //                    JsonConvert.SerializeObject(payload),
    //                    Encoding.UTF8,
    //                    "application/json");

    //                var responseData = await _client.PostAsync($"{_baseUrl}/api/doc/result", content);
    //                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(await responseData.Content.ReadAsStringAsync());

    //                //  HttpEmbeddings httpEmbeddings = new HttpEmbeddings();

    //                //  await httpEmbeddings.EmbeddingsContent(result["data"]?.ToString() ?? string.Empty, mFileId);
    //                // await httpEmbeddings.EmbeddingsContent(embeddingInput);

    //                if (File.Exists(newFilePath))
    //                {
    //                    using (StreamWriter writer = new StreamWriter(newFilePath, true))
    //                    {
    //                        writer.WriteLine(result["data"]?.ToString() ?? string.Empty);
    //                        Console.WriteLine("内容已成功追加到文件中。");
    //                    }
    //                }
    //                else
    //                {
    //                    Console.WriteLine("文件不存在。");
    //                }

    //                if (Convert.ToBoolean(result["is_done"]))
    //                    return newFilePath;

    //            }
    //        }


    //        // HttpEmbeddings httpEmbeddings = new HttpEmbeddings();

    //        // await httpEmbeddings.EmbeddingsContent(result["data"]?.ToString() ?? string.Empty, mFileId);

    //        return newFilePath;
    //    }
    //}

    //private async Task<string> PollDocumentResultAsync(string taskId)
    //{
    //    // 轮询获取结果 - 使用字典
    //    var payload = new Dictionary<string, object>
    //    {
    //        { "id", taskId },
    //        { "is_data", true },
    //        { "format", "text" }
    //    };

    //    while (true)
    //    {
    //        var content = new StringContent(
    //            JsonConvert.SerializeObject(payload),
    //            Encoding.UTF8,
    //            "application/json");

    //        var response = await _client.PostAsync($"{_baseUrl}/api/doc/result", content);
    //        var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());

    //        //此处将文件序列化到一个  txt文件中
    //        //EmbeddingInput embeddingInput = new EmbeddingInput();
    //        //embeddingInput.Texts = result["data"]?.ToString() ?? string.Empty; ;
    //        //embeddingInput.DocId = mFileId;
    //        HttpEmbeddings httpEmbeddings = new HttpEmbeddings();

    //        await httpEmbeddings.EmbeddingsContent(result["data"]?.ToString() ?? string.Empty, mFileId);
    //        // await httpEmbeddings.EmbeddingsContent(embeddingInput);

    //        if (Convert.ToBoolean(result["is_done"]))
    //            return result["data"]?.ToString() ?? string.Empty; ;
    //        await Task.Delay(500);
    //    }
    //}


    private string CreateNewFile(string originalFilePath)
    {
        // 获取文件目录
        string directoryPath = Path.GetDirectoryName(originalFilePath);

        // 获取文件名（不包括扩展名）
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);

        // 获取当前日期
        string currentDate = DateTime.Now.ToString("yyyyMMdd");

        // 生成新的文件名
        string newFileName = $"{fileNameWithoutExtension}_{currentDate}.txt";

        // 组合新的文件路径
        string newFilePath = Path.Combine(directoryPath, newFileName);

        // 读取原始文件内容
        // string fileContent = File.ReadAllText(originalFilePath);

        //  // 将内容写入新的txt文件
        // File.WriteAllText(newFilePath, fileContent);

        return newFilePath;
    }
}