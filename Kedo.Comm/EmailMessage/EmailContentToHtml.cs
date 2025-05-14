
using Markdig;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kedo.Comm.EmailMessage
{
    public class EmailContentToHtml
    {
       public static string ConvertMarkdownToEmailHtml(string markdown)
        {
            // 配置Markdig
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UsePipeTables()
                .Build();

            // 将Markdown转换为HTML
            string htmlContent = Markdown.ToHtml(markdown, pipeline);
            // 应用邮件友好的样式
            htmlContent = ApplyEmailFriendlyStyles(htmlContent);
            // 创建完整的HTML文档
            string emailHtml = $@"<!DOCTYPE html>
                                            <html>
                                            <head>
                                                <meta charset='utf-8'>
                                                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                                <title>Email Content</title>
                                            </head>
                                            <body style='font-family: Arial, Helvetica, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px;'>
                                                {htmlContent}
                                            </body>
                                            </html>";

            return emailHtml;
        }

        static string ApplyEmailFriendlyStyles(string html)
        {
            // 替换表格为邮件友好的HTML表格
            html = html.Replace("<table>", "<table cellspacing='0' cellpadding='8' border='1' style='width:100%; border-collapse:collapse; margin-bottom:20px;'>");

            // 替换表头单元格
            html = Regex.Replace(html, "<th>", "<th style='background-color:#f2f2f2; color:#333; font-weight:bold; text-align:left; padding:8px; border:1px solid #ddd;'>");

            // 替换普通单元格
            html = html.Replace("<td>", "<td style='padding:8px; border:1px solid #ddd;'>");

            // 添加标题样式
            html = html.Replace("<h1>", "<h1 style='color:#2c3e50; margin-top:24px; margin-bottom:16px; font-weight:600; line-height:1.25; font-size:2em;'>");
            html = html.Replace("<h2>", "<h2 style='color:#2c3e50; margin-top:24px; margin-bottom:16px; font-weight:600; line-height:1.25; font-size:1.5em;'>");
            html = html.Replace("<h3>", "<h3 style='color:#2c3e50; margin-top:20px; margin-bottom:14px; font-weight:600; line-height:1.25; font-size:1.25em;'>");
            html = html.Replace("<h4>", "<h4 style='color:#2c3e50; margin-top:16px; margin-bottom:12px; font-weight:600; line-height:1.25; font-size:1em;'>");

            // 添加段落样式
            html = html.Replace("<p>", "<p style='margin-top:0; margin-bottom:16px;'>");

            // 添加链接样式
            html = html.Replace("<a ", "<a style='color:#0366d6; text-decoration:none;' ");

            // 添加列表样式
            html = html.Replace("<ul>", "<ul style='padding-left:2em; margin-top:0; margin-bottom:16px;'>");
            html = html.Replace("<ol>", "<ol style='padding-left:2em; margin-top:0; margin-bottom:16px;'>");
            html = html.Replace("<li>", "<li style='margin-bottom:0.25em;'>");

            // 添加水平线样式
            html = html.Replace("<hr>", "<hr style='height:1px; background-color:#e1e4e8; border:none; margin:24px 0;'>");

            // 添加引用样式
            html = html.Replace("<blockquote>", "<blockquote style='padding:0 1em; color:#6a737d; border-left:0.25em solid #dfe2e5; margin:0 0 16px 0;'>");

            // 添加代码样式
            html = html.Replace("<code>", "<code style='font-family:monospace; padding:0.2em 0.4em; margin:0; font-size:85%; background-color:rgba(27,31,35,0.05); border-radius:3px;'>");

            // 添加强调样式
            html = html.Replace("<strong>", "<strong style='font-weight:600;'>");
            html = html.Replace("<em>", "<em style='font-style:italic;'>");

            return html;
        }
    }
}
