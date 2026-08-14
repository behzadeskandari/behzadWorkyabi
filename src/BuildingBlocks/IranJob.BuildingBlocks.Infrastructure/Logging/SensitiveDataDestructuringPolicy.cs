using Serilog.Core;
using Serilog.Events;

namespace IranJob.BuildingBlocks.Infrastructure.Logging;

public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordhash",
        "token",
        "accesstoken",
        "refreshtoken",
        "jwt",
        "authorization",
        "secret",
        "apikey",
        "merchantid",
        "nationalcode",
        "ssn",
        "creditcard",
        "cardnumber"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null!;

        if (value is not IEnumerable<KeyValuePair<string, object?>> dictionary)
        {
            return false;
        }

        var sanitized = dictionary
            .Select(pair => new LogEventProperty(
                pair.Key,
                SensitivePropertyNames.Contains(pair.Key)
                    ? new ScalarValue("[REDACTED]")
                    : propertyValueFactory.CreatePropertyValue(pair.Value, destructureObjects: true)))
            .ToList();

        result = new StructureValue(sanitized);
        return true;
    }
}
