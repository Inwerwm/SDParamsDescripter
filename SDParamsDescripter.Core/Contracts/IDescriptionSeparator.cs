using SDParamsDescripter.Core.Models;

namespace SDParamsDescripter.Core.Contracts;
public interface IDescriptionSeparator
{
    public DescriptionReplies Separate(string filePath, params string[] excludingParameters);
}
