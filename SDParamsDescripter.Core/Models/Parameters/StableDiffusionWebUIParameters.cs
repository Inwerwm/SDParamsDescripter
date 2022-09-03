namespace SDParamsDescripter.Core.Models.Parameters;
internal record StableDiffusionWebUIParameters(int BatchSize, float CfgScale, float DdimEta, int DdimSteps, int Width, int Height, int NIter, string Prompt, string SamplerName, string Seed, string Target, List<int> Toggles)
{
    public StableDiffusionWebUIParameters() : this(default, default, default, default, default, default, default, "", "", "", "", new List<int>()) { }
}
