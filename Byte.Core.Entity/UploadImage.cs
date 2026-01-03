using Byte.Core.SqlSugar;
using Byte.Core.Tools;
using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel.DataAnnotations.Schema;

namespace Byte.Core.Entity
{
    /// <summary>
    /// 上传图片
    /// </summary>
    [SugarTable("upload_image")]
    
    public class UploadImage : BaseEntity<long>
    {
        /// <summary>
        /// 图片名称
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = false)]
        public string Name { get; set; }

        /// <summary>
        /// 图片URL
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Url { get; set; }

        /// <summary>
        /// 图片格式
        /// </summary>
        [SugarColumn(Length = 10, IsNullable = false)]
        public string Extension { get; set; }

        /// <summary>
        /// 图片大小(字节)
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 图片宽度
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// 图片高度
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// 文件MD5
        /// </summary>
        [SugarColumn(Length = 32, IsNullable = true)]
        public string MD5 { get; set; }


        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Status { get; set; } = true;

        public int Category { get; set; }
        /// <summary>
        /// 下载次数
        /// </summary>
        public int DownloadCount { get; set; } = 0;
    }
}
