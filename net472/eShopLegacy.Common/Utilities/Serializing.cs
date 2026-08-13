using System.IO;
using System.Text;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Provides serialization utilities. Migrated from BinaryFormatter (removed in .NET 9+) to System.Text.Json.
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

        public object? DeserializeBinary<T>(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<T>(stream);
        }
    }
}
