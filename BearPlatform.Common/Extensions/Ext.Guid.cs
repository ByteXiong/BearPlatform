using System;

namespace BearPlatform.Common.Extensions;

/// <summary>
/// GUID扩展
/// </summary>
public static partial class ExtObject
{
    /// <summary>
    /// 转为有序的GUID
    /// 注：长度为50字符
    /// </summary>
    /// <param name="guid">新的GUID</param>
    /// <returns></returns>
    public static string ToSequentialGuid(this Guid guid)
    {
        var timeStr = (DateTime.Now.ToCstTime().Ticks / 10000).ToString("x8");
        var newGuid = $"{timeStr.PadLeft(13, '0')}-{guid}";

        return newGuid;
    }
}
