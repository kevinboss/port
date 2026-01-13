using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace port.Config;

/// <summary>
/// Handles both list and dictionary formats for docker-compose environment variables.
/// List format: ["FOO=bar", "BAZ=qux"]
/// Dict format: { FOO: bar, BAZ: qux }
/// </summary>
public class EnvironmentVariableConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(List<string>);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var result = new List<string>();

        if (parser.TryConsume<SequenceStart>(out _))
        {
            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                var scalar = parser.Consume<Scalar>();
                result.Add(scalar.Value);
            }
        }
        else if (parser.TryConsume<MappingStart>(out _))
        {
            while (!parser.TryConsume<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>();
                var value = parser.Consume<Scalar>();
                result.Add($"{key.Value}={value.Value}");
            }
        }
        else
        {
            parser.MoveNext();
            return null;
        }

        return result;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        throw new NotSupportedException("Writing is not supported");
    }
}