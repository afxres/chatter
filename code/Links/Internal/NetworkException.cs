using System;
using System.Runtime.Serialization;

namespace Mikodev.Links.Internal
{
    [Serializable]
    internal sealed class NetworkException : Exception
    {
        public NetworkError ErrorCode { get; }

        public NetworkException(NetworkError error) : this(error, GetMessage(error)) { }

        public NetworkException(NetworkError error, string message) : base(message) => this.ErrorCode = error;

        public NetworkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.ErrorCode = (NetworkError)info.GetInt32(nameof(this.ErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(this.ErrorCode), (int)this.ErrorCode);
            base.GetObjectData(info, context);
        }

        private static string GetMessage(NetworkError error) => error switch
        {
            NetworkError.InvalidData => "Invalid data!",
            NetworkError.InvalidHost => "Invalid host!",
            NetworkError.UdpPacketTooLarge => "Udp packet too large!",
            _ => "Undefined error!",
        };
    }
}
