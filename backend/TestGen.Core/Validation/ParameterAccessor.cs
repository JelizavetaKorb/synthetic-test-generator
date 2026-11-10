using System.Text.Json;

namespace TestGen.Core.Validation;

internal static class ParameterAccessor
{
    private static readonly System.Text.RegularExpressions.Regex BracketKey =
        new(@"^(\w+)\[(\d+)\]\.(.+)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static IReadOnlyList<(int Index, IDictionary<string, object> Fields)> GetItems(
        IDictionary<string, object> p, string listKey)
    {
        var items = new SortedDictionary<int, IDictionary<string, object>>();

        if (p.TryGetValue(listKey, out var raw) && raw is JsonElement { ValueKind: JsonValueKind.Array } array)
        {
            var i = 0;
            foreach (var element in array.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var fields = new Dictionary<string, object>();
                    Flatten(element, string.Empty, fields);
                    items[i] = fields;
                }
                i++;
            }
        }

        foreach (var (key, value) in p)
        {
            var m = BracketKey.Match(key);
            if (!m.Success || m.Groups[1].Value != listKey) continue;
            var index = int.Parse(m.Groups[2].Value);
            if (!items.TryGetValue(index, out var fields)) items[index] = fields = new Dictionary<string, object>();
            fields[m.Groups[3].Value] = value;
        }

        return items.Select(static kv => (kv.Key, kv.Value)).ToList();
    }

    public static bool AnyItemHas(IDictionary<string, object> p, string listKey, string field, string expected) =>
        GetItems(p, listKey).Any(item =>
            TryGetString(item.Fields, field, out var value) &&
            value.Equals(expected, StringComparison.OrdinalIgnoreCase));

    private static void Flatten(JsonElement obj, string prefix, Dictionary<string, object> into)
    {
        foreach (var field in obj.EnumerateObject())
        {
            var key = prefix + field.Name;
            if (field.Value.ValueKind == JsonValueKind.Object)
                Flatten(field.Value, key + ".", into);
            else
                into[key] = field.Value.Clone();
        }
    }

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
