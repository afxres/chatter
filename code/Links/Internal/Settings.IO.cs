using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal
{
    internal sealed partial class Settings
    {
        private static readonly int SettingsMaximumCharacters = 32 * 1024;

        private static readonly TimeSpan SettingsIOTimeout = TimeSpan.FromSeconds(10);

        private static Uri NormalizeBroadcastUri(string text, int alternativePort)
        {
            const string prefix = "udp://";
            if (string.IsNullOrEmpty(text))
                goto fail;
            var result = text.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                ? new Uri(text)
                : new Uri(prefix + text);
            if (string.IsNullOrEmpty(result.Host) || result.PathAndQuery != "/")
                goto fail;
            if (string.IsNullOrEmpty(result.GetComponents(UriComponents.Port, UriFormat.SafeUnescaped)))
                result = new Uri($"{prefix}{result.Host}:{alternativePort}");
            return result;
        fail:
            throw new FormatException($"Invalid uri: '{text}'");
        }

        public void Load(string source)
        {
            var data = JsonValue.Load(new StringReader(source));
            var client = data["client"];
            var id = (string)client["id"];
            var name = (string)client["name"];
            var text = (string)client["text"];
            var imageHash = (string)client["image-hash"];

            var net = data["net"];
            var tcpPort = (ushort)net["tcp-port"];
            var udpPort = (ushort)net["udp-port"];
            var broadcastUris = ((JsonArray)net["broadcast-uris"]).Select(x => NormalizeBroadcastUri(x, udpPort)).Distinct().ToArray();

            if (string.IsNullOrEmpty(id))
                throw new InvalidDataException("Client id can not be empty!");
            if (broadcastUris.Length == 0)
                throw new InvalidDataException("No available broadcast address!");

            this.ClientId = id;
            this.ClientName = name;
            this.ClientText = text;
            this.ClientImageHash = imageHash;

            this.TcpEndPoint = new IPEndPoint(IPAddress.Any, tcpPort);
            this.UdpEndPoint = new IPEndPoint(IPAddress.Any, udpPort);
            this.BroadcastUris = broadcastUris;
        }

        public string Save()
        {
            Debug.Assert(!string.IsNullOrEmpty(this.ClientId));
            Debug.Assert(this.BroadcastUris != null && this.BroadcastUris.Length > 0);

            var client = new Dictionary<string, JsonValue>
            {
                ["id"] = ClientId,
                ["name"] = ClientName,
                ["text"] = ClientText,
                ["image-hash"] = ClientImageHash,
            };

            var net = new Dictionary<string, JsonValue>
            {
                ["tcp-port"] = this.TcpEndPoint.Port,
                ["udp-port"] = this.UdpEndPoint.Port,
                ["broadcast-uris"] = new JsonArray(this.BroadcastUris.Select(x => (JsonValue)x.OriginalString)),
            };

            var data = new JsonObject(new Dictionary<string, JsonValue>
            {
                ["client"] = new JsonObject(client),
                ["net"] = new JsonObject(net),
            });

            var buffer = new StringBuilder();
            var writer = new StringWriter(buffer);
            data.Save(writer);
            return buffer.ToString();
        }

        public static async Task<Settings> LoadAsync(TextReader reader)
        {
            var buffer = new char[SettingsMaximumCharacters];
            var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length).TimeoutAfter(SettingsIOTimeout);
            var text = new string(buffer, 0, length);
            var settings = new Settings();
            settings.Load(text);
            return settings;
        }

        public static async Task<Settings> LoadAsync(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await LoadAsync(reader);
        }

        public async Task SaveAsync(TextWriter writer)
        {
            var item = this;
            var text = item.Save();
            await writer.WriteAsync(text).TimeoutAfter(SettingsIOTimeout);
        }

        public async Task SaveAsync(string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            await this.SaveAsync(writer);
        }
    }
}
