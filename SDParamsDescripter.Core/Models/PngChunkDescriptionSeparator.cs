using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDParamsDescripter.Core.Contracts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace SDParamsDescripter.Core.Models;
public class PngChunkDescriptionSeparator : IDescriptionSeparator
{
    public DescriptionReplies Separate(string filePath, params string[] excludingParameters)
    {
        var image = Image.Load(filePath);
        var text = image.Metadata.GetPngMetadata().TextData[0].Value;

        return Parse(text, excludingParameters);
    }

    public static DescriptionReplies Parse(string text, params string[] excludingParameters)
    {
        var lines = text.ReplaceLineEndings().Split(Environment.NewLine);

        var prompt = lines[0];
        var parameters = lines[1..]
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => excludingParameters.All(ep => !l.Contains(ep)))
            .TakeWhile(line => !line.StartsWith("Warning:"))
            .SelectMany(line => line.StartsWith("Negative prompt")
                ? new[] { line.Replace("Negative prompt: ", "Negative prompt:\n") + "\n" }
                : line.Split(", "));

        var paramsString = string.Join(Environment.NewLine, parameters);
        return new($"""
            Prompt:
            {prompt}

            {paramsString}
            """,
            paramsString,
            new PromptSplitter().Split(prompt).Select(p => new PromptReply(p)).ToArray());
    }
}
