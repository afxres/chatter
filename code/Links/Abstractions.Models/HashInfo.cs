using System;
using System.IO;

namespace Mikodev.Links.Abstractions.Models
{
    /// <summary>
    /// 本地缓存信息
    /// </summary>
    internal readonly struct HashInfo
    {
        /// <summary>
        /// 散列值 (取决于散列算法)
        /// </summary>
        public string Hash { get; }

        /// <summary>
        /// 本地文件路径
        /// </summary>
        public FileInfo FileInfo { get; }

        public HashInfo(string hash, FileInfo fileInfo)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("File hash can not be null or empty!", nameof(hash));
            this.Hash = hash;
            this.FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        }
    }
}
