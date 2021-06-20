using System;

namespace Mikodev.Links.Abstractions
{
    /// <summary>
    /// 消息
    /// </summary>
    public abstract class Message
    {
        /// <summary>
        /// 消息 Id
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 消息时间 (未规定为发出时间或接收时间)
        /// </summary>
        public DateTime DateTime { get; }

        /// <summary>
        /// 消息来源
        /// </summary>
        public MessageReference Reference { get; }

        /// <summary>
        /// 消息状态
        /// </summary>
        public abstract MessageStatus Status { get; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public abstract object Object { get; }

        protected Message(string messageId, string path, DateTime dateTime, MessageReference reference)
        {
            if (string.IsNullOrEmpty(messageId))
                throw new ArgumentException("Argument can not be null or empty.", nameof(messageId));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Argument can not be null or empty.", nameof(path));
            this.MessageId = messageId;
            this.Path = path;
            this.DateTime = dateTime;
            this.Reference = reference;
        }
    }
}
