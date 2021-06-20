using Mikodev.Links.Abstractions.Models;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    /// <summary>
    /// 缓存管理
    /// </summary>
    internal interface ICache
    {
        /// <summary>
        /// 尝试根据散列值获取对应的缓存文件
        /// </summary>
        bool TryGetCache(string hash, out FileInfo fileInfo);

        /// <summary>
        /// 将一个本地文件保存到缓存
        /// </summary>
        Task<HashInfo> SetCacheAsync(FileInfo fileInfo, CancellationToken token);

        /// <summary>
        /// 向目标端点请求对应的缓存文件
        /// </summary>
        Task<FileInfo> GetCacheAsync(string hash, IPEndPoint endpoint, CancellationToken token);
    }
}
