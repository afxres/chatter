using System;
using System.Net;

namespace Mikodev.Links.Abstractions
{
    /// <summary>
    /// 资料
    /// </summary>
    public abstract class Profile
    {
        /// <summary>
        /// 资料 Id
        /// </summary>
        public string ProfileId { get; }

        /// <summary>
        /// 昵称
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// 签名
        /// </summary>
        public abstract string Text { get; set; }

        /// <summary>
        /// 未读消息计数
        /// </summary>
        public abstract int UnreadCount { get; set; }

        /// <summary>
        /// 在线状态
        /// </summary>
        public abstract ProfileOnlineStatus OnlineStatus { get; }

        /// <summary>
        /// 头像缓存路径
        /// </summary>
        public abstract string ImagePath { get; }

        /// <summary>
        /// IP 地址 (广播地址)
        /// </summary>
        public abstract IPAddress IPAddress { get; }

        protected Profile(string profileId)
        {
            if (profileId is null)
                throw new ArgumentNullException(nameof(profileId));
            this.ProfileId = profileId;
        }
    }
}
