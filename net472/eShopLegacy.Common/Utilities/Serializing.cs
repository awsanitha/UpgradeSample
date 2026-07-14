using System.IO;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Serialization helper. BinaryFormatter is not available in .NET 5+.
    /// Uses System.Text.Json for JSON serialization instead.
    /// </summary>
    public class Serializing
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, _options);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public object? DeserializeBinary(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<object>(stream, _options);
        }
    }
}
