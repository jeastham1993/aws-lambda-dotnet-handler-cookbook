using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Events;

public class EventJsonConverter : JsonConverter<Event>
{
    public override Event Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
    }

    public override void Write(Utf8JsonWriter writer, Event evt, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)evt, options);
    }
}