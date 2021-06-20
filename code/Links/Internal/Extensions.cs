using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal
{
    internal static class Extensions
    {
        public static Task ReadBlockAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            return ReadBlockAsync(stream, buffer, 0, buffer.Length, cancellationToken);
        }

        public static async Task ReadBlockAsync(this Stream stream, byte[] buffer, int offset, int length, CancellationToken cancellationToken)
        {
            if (offset < 0 || length < 1 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            while (length > 0)
            {
                var result = await stream.ReadAsync(buffer, offset, length, cancellationToken);
                if (result < 1)
                    throw new EndOfStreamException();
                offset += result;
                length -= result;
            }
        }

        public static async Task<byte[]> ReadBlockWithHeaderAsync(this Stream stream, int limits, CancellationToken cancellationToken)
        {
            var header = new byte[sizeof(int)];
            await ReadBlockAsync(stream, header, cancellationToken);
            var length = BitConverter.ToInt32(header, 0);
            if (length < 1 || length > limits)
                throw new InvalidOperationException();
            var buffer = new byte[length];
            await ReadBlockAsync(stream, buffer, cancellationToken);
            return buffer;
        }

        public static Task WriteWithHeaderAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            return WriteWithHeaderAsync(stream, buffer, 0, buffer.Length, cancellationToken);
        }

        public static async Task WriteWithHeaderAsync(this Stream stream, byte[] buffer, int offset, int length, CancellationToken cancellationToken)
        {
            if (offset < 0 || length < 1 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            var header = BitConverter.GetBytes(length);
            await stream.WriteAsync(header, 0, header.Length, cancellationToken);
            await stream.WriteAsync(buffer, offset, length, cancellationToken);
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException();
            using (var cancel = new CancellationTokenSource())
            {
                var result = await Task.WhenAny(task, Task.Delay(timeout, cancel.Token));
                if (result == task)
                    return;
                throw new TimeoutException();
            }
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException();
            using (var cancel = new CancellationTokenSource())
            {
                var result = await Task.WhenAny(task, Task.Delay(timeout, cancel.Token));
                if (result == task)
                    return await task;
                throw new TimeoutException();
            }
        }

        public static V GetOrAdd<K, V>(this Dictionary<K, V> dictionary, K key, Func<K, V> factory)
        {
            if (dictionary.TryGetValue(key, out var value))
                return value;
            var result = factory.Invoke(key);
            dictionary.Add(key, result);
            return result;
        }

        public static IPAddress[] GetBroadcastAddresses()
        {
            static IPAddress GetBroadcastAddress(UnicastIPAddressInformation information)
            {
                var maskBytes = information.IPv4Mask.GetAddressBytes();
                var addressBytes = information.Address.GetAddressBytes();
                for (var i = 0; i < 4; i++)
                    addressBytes[i] |= (byte)(~maskBytes[i]);
                return new IPAddress(addressBytes);
            }

            var inetAddresses = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .ToList();
            var infos = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                .Where(x => inetAddresses.Contains(x.Address));
            var broadcastAddresses = infos.Select(GetBroadcastAddress).ToArray();
            return broadcastAddresses.Any() ? broadcastAddresses : (new[] { IPAddress.Broadcast });
        }
    }
}
