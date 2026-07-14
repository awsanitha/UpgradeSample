using System.IO;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Provides JSON-based serialization. BinaryFormatter was removed in .NET 9+.
    /// </summary>
    public class Serializing
    {
        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, input.GetType());
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public object? DeserializeBinary(Stream stream, System.Type type)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize(stream, type);
        }
    }
}
