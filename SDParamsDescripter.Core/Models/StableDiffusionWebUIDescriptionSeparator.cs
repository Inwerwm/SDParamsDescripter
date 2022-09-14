using SDParamsDescripter.Core.Contracts;
using SDParamsDescripter.Core.Models.Parameters;
using SharpYaml.Serialization;

namespace SDParamsDescripter.Core.Models;
public class StableDiffusionWebUIDescriptionSeparator : IDescriptionSeparator
{
    public DescriptionReplies Separate(string filePath)
    {
        using var paramFile = File.OpenRead(filePath);

        var serializer = new Serializer(new() { NamingConvention = new SnakeCaseNamingConvention()});
        var parameters = serializer.Deserialize<StableDiffusionWebUIParameters>(paramFile);

        var numbers = $"""
            CFG Scale: {parameters.CfgScale}
            DDIM Steps: {parameters.DdimSteps}
            Size: {parameters.Width} × {parameters.Height}
            Seed: {parameters.Seed}
            Sampler: {parameters.SamplerName}
            """;
        return new($"""
            {numbers}
            
            Prompt:
            {parameters.Prompt}
            """,
            numbers,
            new PromptSplitter().Split(parameters.Prompt).Select(p => new PromptReply(p)).ToArray());
    }
}
