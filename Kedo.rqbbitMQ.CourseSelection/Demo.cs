////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Text;
////using System.Threading.Tasks;

////namespace Kedo.rabbitMQ.BIData
////{
////    internal class Demo
////    {
////    }
////}
////完整使用示例
////以下是使用这个智能分段工具的完整示例：
////csharp
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text.Json;
//using System.Threading.Tasks;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        Console.WriteLine("=== 智能文本分段工具演示 ===");

//        // 示例文本
//        string sampleText = @"# 智能文本分段算法

//智能文本分段是一种高级文本处理技术，能够根据文本内容的语义结构自动将长文本分割成合适大小的段落。

//## 主要特点

//1. 自动识别文本类型
//2. 智能寻找断句点
//3. 根据需要合并或拆分段落
//4. 保留段落间的上下文关系

//这种技术特别适用于知识库构建、搜索引擎优化和内容推荐系统。";

//        // 创建分段器实例
//        var segmenter = new SmartTextSegmenter(enableDetailedLogging: true);

//        // 执行分段
//        Console.WriteLine("正在分段文本...");
//        var segments = await segmenter.SegmentTextWithMinLength(sampleText, 100, 500);

//        // 输出结果
//        Console.WriteLine($"\n分段结果 ({segments.Count} 个段落):");
//        for (int i = 0; i < segments.Count; i++)
//        {
//            var segment = segments[i];
//            Console.WriteLine($"=== 段落 {i + 1}/{segments.Count} ===");
//            Console.WriteLine($"ID: {segment.SegmentId}");
//            Console.WriteLine($"类型: {segment.DocType}");
//            Console.WriteLine($"长度: {segment.Text.Length} 字符");
//            Console.WriteLine($"合并数: {segment.MergedCount}");
//            Console.WriteLine("内容:");
//            Console.WriteLine(segment.Text);
//            Console.WriteLine(new string('-', 50));
//        }

//        // 演示从文件加载并分段
//        if (args.Length > 0 && File.Exists(args[0]))
//        {
//            Console.WriteLine($"\n处理文件: {args[0]}");
//            string fileContent = File.ReadAllText(args[0]);

//            Console.WriteLine("正在分段文件内容...");
//            var fileSegments = await segmenter.SegmentTextWithMinLength(fileContent, 150, 800);

//            Console.WriteLine($"\n文件分段结果 ({fileSegments.Count} 个段落):");
//            Console.WriteLine($"原始文本长度: {fileContent.Length} 字符");
//            Console.WriteLine($"平均段落长度: {fileSegments.Sum(s => s.Text.Length) / fileSegments.Count} 字符");

//            // 保存分段结果
//            string outputPath = Path.ChangeExtension(args[0], ".segments.txt");
//            using (StreamWriter writer = new StreamWriter(outputPath))
//            {
//                writer.WriteLine($"=== 分段结果: {fileSegments.Count} 个段落 ===\n");

//                for (int i = 0; i < fileSegments.Count; i++)
//                {
//                    writer.WriteLine($"--- 段落 {i + 1}/{fileSegments.Count} ---");
//                    writer.WriteLine($"类型: {fileSegments[i].DocType}");
//                    writer.WriteLine($"长度: {fileSegments[i].Text.Length} 字符");
//                    writer.WriteLine(fileSegments[i].Text);
//                    writer.WriteLine(new string('-', 50));
//                }
//            }

//            Console.WriteLine($"分段结果已保存至: {outputPath}");
//        }
//    }
//}
////进阶用法：批量处理文件
////csharp
//// 批量处理多个文件
//public static async Task BatchProcessFiles(string directoryPath, string searchPattern = "*.txt")
//{
//    if (!Directory.Exists(directoryPath))
//    {
//        Console.WriteLine($"目录不存在: {directoryPath}");
//        return;
//    }

//    string[] files = Directory.GetFiles(directoryPath, searchPattern);
//    Console.WriteLine($"找到 {files.Length} 个文件准备处理");

//    var segmenter = new SmartTextSegmenter(enableDetailedLogging: false);

//    foreach (string file in files)
//    {
//        Console.WriteLine($"处理文件: {Path.GetFileName(file)}");
//        string content = File.ReadAllText(file);

//        try
//        {
//            var segments = await segmenter.SegmentTextWithMinLength(content, 150, 800);

//            // 保存分段结果到JSON
//            string outputPath = Path.ChangeExtension(file, ".segments.json");

//            // 创建结果对象
//            var result = new
//            {
//                FileName = Path.GetFileName(file),
//                OriginalSize = content.Length,
//                SegmentCount = segments.Count,
//                AverageLength = segments.Sum(s => s.Text.Length) / segments.Count,
//                Segments = segments.Select(s => new
//                {
//                    Id = s.SegmentId,
//                    Text = s.Text,
//                    Length = s.Text.Length,
//                    Type = s.DocType,
//                    Position = s.OriginalPosition
//                }).ToArray()
//            };

//            // 序列化为JSON
//            string json = JsonSerializer.Serialize(result, new JsonSerializerOptions
//            {
//                WriteIndented = true,
//                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
//            });

//            // 保存到文件
//            File.WriteAllText(outputPath, json);

//            Console.WriteLine($"✅ 处理完成: {segments.Count} 个段落, 已保存到 {outputPath}");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"❌ 处理失败: {ex.Message}");
//        }
//    }
//}
////这个完整的C#智能分段工具实现了原JavaScript版本的所有功能，包括：
////1.智能文本分类 - 区分FAQ、一般知识和长文档
////2.基于语义的断句 - 确保在合适的位置分段
////3.长段落分割 - 处理超长文本
////4.短段落合并 - 确保段落长度合适
////5.多级优化 - 通过多轮处理提高分段质量
////6.详细的日志记录 - 跟踪分段过程
////7.错误处理与恢复 - 确保稳定性
////8.批量处理能力 - 支持处理多个文件