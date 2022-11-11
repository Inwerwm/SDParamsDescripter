using System.Text;
using SDParamsDescripter.Core.Contracts;
using SDParamsDescripter.Core.Models.Parameters;
using SharpYaml.Serialization;

namespace SDParamsDescripter.Core.Models;
public class StableDiffusionWebUIDescriptionSeparator : IDescriptionSeparator
{
    public DescriptionReplies Separate(string filePath, params string[] excludingParameters)
    {
        using var paramFile = File.OpenRead(filePath);

        var serializer = new Serializer(new() { NamingConvention = new SnakeCaseNamingConvention() });
        var parameters = serializer.Deserialize<StableDiffusionWebUIParameters>(paramFile);

        var builder = new StringBuilder();
        if (!excludingParameters.Contains("CFG Scale"))
        {
            builder.AppendLine($"CFG Scale: {parameters.CfgScale}");
        }
        if (!excludingParameters.Contains("DDIM Steps"))
        {
            builder.AppendLine($"DDIM Steps: {parameters.DdimSteps}");
        }
        if (!excludingParameters.Contains("Size"))
        {
            builder.AppendLine($"Size: {parameters.Width} × {parameters.Height}");
        }
        if (!excludingParameters.Contains("Seed"))
        {
            builder.AppendLine($"Seed: {parameters.Seed}");
        }
        if (!excludingParameters.Contains("Sampler"))
        {
            builder.AppendLine($"Sampler: {parameters.SamplerName}");
        }

        var numbers = builder.ToString();
        return new($"""
            {numbers}
            
            Prompt:
            {parameters.Prompt}
            """,
            numbers,
            new PromptSplitter().Split(parameters.Prompt).Select(p => new PromptReply(p)).ToArray());
    }
}
