using CommunityToolkit.Mvvm.ComponentModel;
using SDParamsDescripter.Core.Models;
using SDParamsDescripter.Core.Twitter;

namespace SDParamsDescripter.ViewModels;

public partial class ImageTask : ObservableRecipient
{
    public string ImagePath
    {
        get;
        init;
    }

    public string SavePath => Tweet.ImagePath;
    public string Description => $"""
        FileName:
            {Path.GetFileName(ImagePath)}

        SaveDirectory:
            {Path.GetDirectoryName(SavePath) ?? ""}

        Upscale Model:
            {RealEsrGan.GetModelName(DoesAnimeModel)}

        Auto Post:
            {(EnablePost ? "Enable" : "Disable")}
        """;

    public Tweet Tweet
    {
        get;
        init;
    }

    public bool DoesAnimeModel
    {
        get;
        init;
    }

    public bool EnablePost
    {
        get;
        init;
    }

    [ObservableProperty]
    private bool _isProgress;

    public ImageTask(string imagePath, Tweet tweet, bool enablePost, bool doesAnimeModel)
    {
        ImagePath = imagePath;
        Tweet = tweet;
        EnablePost = enablePost;
        _isProgress = false;
        DoesAnimeModel = doesAnimeModel;
    }
}
