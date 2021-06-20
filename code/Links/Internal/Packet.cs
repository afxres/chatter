using Mikodev.Binary;
using System.Collections.Generic;

namespace Mikodev.Links.Internal
{
    internal sealed class Packet
    {
        /// <summary>
        /// packet id (maybe null)
        /// </summary>
        public string PacketId { get; }

        /// <summary>
        /// client id
        /// </summary>
        public string SenderId { get; }

        /// <summary>
        /// packet data (maybe null)
        /// </summary>
        public Token Data { get; }

        /// <summary>
        /// message handler path
        /// </summary>
        public string Path { get; }

        public Packet(IGenerator generator, byte[] buffer)
        {
            var token = new Token(generator, buffer);
            this.PacketId = token.Children.GetValueOrDefault("packetId")?.As<string>();
            this.Data = token.Children.GetValueOrDefault("data");
            this.Path = token["path"].As<string>();
            this.SenderId = token["senderId"].As<string>();
        }

        public override string ToString() => $"{nameof(Packet)}(Sender: {this.SenderId}, Path: {this.Path})";
    }
}
