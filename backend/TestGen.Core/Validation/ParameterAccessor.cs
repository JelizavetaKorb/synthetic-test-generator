using System.Text.Json;

namespace TestGen.Core.Validation;

internal static class ParameterAccessor
{
    public static bool TryGetString(
        IDictionary<string, object> p, string key, out string value)
    {
        value = string.Empty;
        if (!p.TryGetValue(key, out var raw) || raw is null) return false;

        if (raw is string s) { value = s; return true; }

        if (raw is JsonElement je)
        {
            value = je.ValueKind == JsonValueKind.String
                ? je.GetString() ?? string.Empty
                : je.ToString();
            return true;
        }

        value = raw.ToString() ?? string.Empty;
        return true;
    }

    public static bool TryGetDouble(
        IDictionary<string, object> p, string key, out double value)
    {
        value = 0;
        if (!p.TryGetValue(key, out var raw) || raw is null) return false;

        switch (raw)
        {
            case double d:    value = d;           return true;
            case float  f:    value = f;           return true;
            case int    i:    value = i;           return true;
            case long   l:    value = l;           return true;
            case short  s:    value = s;           return true;
            case decimal dec: value = (double)dec; return true;
        }

        if (raw is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number)
                return je.TryGetDouble(out value);
            if (je.ValueKind == JsonValueKind.String)
                return double.TryParse(
                    je.GetString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value);
            return false;
        }

        if (raw is string str)
            return double.TryParse(str,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);

        try   { value = Convert.ToDouble(raw); return true; }
        catch { return false; }
    }
}
