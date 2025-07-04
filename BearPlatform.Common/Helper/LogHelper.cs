using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BearPlatform.Common.ClassLibrary;
using BearPlatform.Common.Global;

namespace BearPlatform.Common.Helper;

/// <summary>
/// 日志操作类
/// </summary>
public class LogHelper
{
    private static UsingLock<object> _lock = new UsingLock<object>();


    /// <summary>
    /// 文本日志
    /// </summary>
    /// <param name="folder">文件夹</param>
    /// <param name="message">消息</param>   
    public static void WriteError(string message, List<string> folder)
    {
        AddLog(message, folder);
    }

    /// <summary>
    /// 写日志文件数据库日志文件
    /// </summary>
    /// <param name="folder">文件夹</param>
    /// <param name="message">消息</param> 
    public static void WriteLog(string message, List<string> folder)
    {
        AddLog(message, folder);
    }

    /// <summary>
    /// 文本日志
    /// </summary>
    /// <param name="folder">文件夹</param>
    /// <param name="message">日志存储目录名称</param>
    private static void AddLog(string message, List<string> folder)
    {
        try
        {
            var path = Path.Combine(AppSettings.ContentRootPath, "Logs");
            if (folder != null && folder.Count != 0)
            {
                path = folder.Aggregate(path, Path.Combine);
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string logFilePath = Path.Combine(path, $@"{DateTime.Now:yyyyMMdd}.log");
            //只保留30天的日志
            var deletePath = Path.Combine(path, $@"{DateTime.Now.AddDays(-30):yyyyMMdd}.log");
            if (File.Exists(deletePath))
            {
                File.Delete(deletePath);
            }

            if (!File.Exists(logFilePath))
            {
                using var fs = new FileStream(logFilePath, FileMode.Create, FileAccess.Write);
                var sw = new StreamWriter(fs);
                sw.Close();
                fs.Close();
            }

            using (_lock.Write())
            {
                using var writer = new StreamWriter(logFilePath, true);
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"));
                writer.WriteLine(message);
                writer.WriteLine(Environment.NewLine);
            }
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// SQL日志
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="dataParas"></param>
    public static void WriteSqlLog(string filename, List<string> dataParas)
    {
        try
        {
            var path = Path.Combine(AppSettings.ContentRootPath, "Logs", "Sql");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var logFilePath = Path.Combine(path, $@"{filename}.log");

            var logContent =
                DateTime.Now.ToString("yyyy-MM-dd HH:ss:mm fff") + "\r\n" +
                string.Join("", dataParas) + "\r\n\r\n\r\n\r\n";
            using (_lock.Write())
            {
                File.AppendAllText(logFilePath, logContent);
            }
        }
        catch
        {
            // ignored
        }
    }
}
