using SharpYaml.Serialization;

namespace SDParamsDescripter.Core.Models;
public class SnakeCaseNamingConvention : IMemberNamingConvention
{
    public StringComparer Comparer => StringComparer.Ordinal;

    public string Convert(string name) => string.Join("", name.SelectMany((c, i) => i > 0 && char.IsUpper(c) ? $"_{char.ToLower(c)}" : $"{char.ToLower(c)}"));
}
