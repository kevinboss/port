using System.Text.Json;
using System.Text.Json.Serialization;

namespace port;

/// <summary>
/// Writes an environment list with secret values masked by <see cref="EnvironmentMasker"/>.
/// Only serialisation is affected, so callers that read the property in memory
/// (container creation) still see the real values.
/// </summary>
public sealed class MaskedEnvironmentJsonConverter : JsonConverter<IList<string>>
{
    public override IList<string>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => JsonSerializer.Deserialize<List<string>>(ref reader, options);

    public override void Write(
        Utf8JsonWriter writer,
        IList<string> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (var assignment in value)
            writer.WriteStringValue(EnvironmentMasker.MaskAssignment(assignment));
        writer.WriteEndArray();
    }
}
