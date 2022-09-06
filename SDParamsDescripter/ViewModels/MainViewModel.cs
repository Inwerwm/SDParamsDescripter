using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinqToTwitter.Common;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SDParamsDescripter.Core.Contracts;
using SDParamsDescripter.Core.Models;
using SDParamsDescripter.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace SDParamsDescripter.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty]
    private DescriptionReplies _replies;
    [ObservableProperty]
    private ObservableCollection<PromptReply> _prompts;
    [ObservableProperty]
    private string _postText;

    [ObservableProperty]
    private string _upscaleImageDir;
    [ObservableProperty]
    private string _conceptName;

    [ObservableProperty]
    private bool _doesUseAnimeModel;
    [ObservableProperty]
    private bool _isUpscalingInProgress;
    [ObservableProperty]
    private bool _enableAutoPost;
    [ObservableProperty]
    private bool _retryWhenImageIsTooLarge;

    [ObservableProperty]
    private bool _isOpenTwitterErrorInfo;
    [ObservableProperty]
    private string _twitterErrorMessage;

    private IDescriptionSeparator Separator
    {
        get;
    }
    private RealEsrGan UpScaler
    {
        get;
    }
    private FileSystemWatcher Watcher
    {
        get;
    }

    private Twitter Twitter
    {
        get;
    }
    public DispatcherQueue? DispatcherQueue
    {
        get; set;
    }

    public MainViewModel()
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        _replies = new("", "", Array.Empty<PromptReply>());
        _prompts = new ObservableCollection<PromptReply>();
        _postText = localSettings.Values["postText"] as string ?? "#StableDiffusion\nThe #prompt is in ALT.";

        _upscaleImageDir = localSettings.Values["upscaleImageDir"] as string ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _conceptName = localSettings.Values["conceptName"] as string ?? "";

        _doesUseAnimeModel = false;
        _isUpscalingInProgress = false;
        _enableAutoPost = false;
        _retryWhenImageIsTooLarge = true;

        _isOpenTwitterErrorInfo = false;
        _twitterErrorMessage = "";

        Separator = new StableDiffusionWebUIDescriptionSeparator();
        UpScaler = new RealEsrGan();
        Watcher = new()
        {
            NotifyFilter = NotifyFilters.FileName,
            Filter = "*.png"
        };

        Twitter = new(
            Properties.Resources.APIKey,
            Properties.Resources.APIKeySecret,
            Properties.Resources.AccessToken,
            Properties.Resources.AccessTokenSecret);

        App.MainWindow.Closed += DisposeMembers;
    }

    private void ReadFile(IStorageItem? file)
    {
        if (file is null) { return; }

        switch (Path.GetExtension(file.Name))
        {
            case ".yaml": ReadYaml(file.Path); break;
            case ".png": UpScale(file.Path); break;
            default: break;
        }
    }

    public void ReadYaml(string yamlFilePath)
    {
        if (!File.Exists(yamlFilePath)) { return; }

        Replies = Separator.Separate(yamlFilePath);
        Prompts.Clear();
        foreach (var p in Replies.PromptReplies)
        {
            Prompts.Add(p);
        }
    }

    public void UpScale(string imagePath)
    {
        if (DispatcherQueue is null) { return; }
        if (IsUpscalingInProgress) { return; }
        if (!File.Exists(imagePath)) { return; }

        // Make path
        var saveDir = Path.Combine(UpscaleImageDir, ConceptName);
        var savePath = Path.Combine(saveDir, Path.GetFileName(imagePath));
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        // Copy and read same name yaml file
        static string yamlName(string path) => Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + ".yaml");
        File.Copy(yamlName(imagePath), yamlName(savePath), true);
        ReadYaml(yamlName(imagePath));

        // Start watching file generation
        async void onFilesChanged(object _, FileSystemEventArgs e)
        {
            if (e.FullPath != savePath) { return; }

            if (EnableAutoPost)
            {
                IsOpenTwitterErrorInfo = false;
                await PostToTwitter(savePath);
            }

            DispatcherQueue.TryEnqueue(() => IsUpscalingInProgress = false);

            Watcher.EnableRaisingEvents = false;
            Watcher.Created -= onFilesChanged;
            Watcher.Changed -= onFilesChanged;
        }
        Watcher.Path = saveDir;
        Watcher.Created += onFilesChanged;
        Watcher.Changed += onFilesChanged;
        Watcher.EnableRaisingEvents = true;

        // Run upscaler
        IsUpscalingInProgress = true;
        UpScaler.Run(imagePath, savePath, DoesUseAnimeModel);
    }

    private async Task PostToTwitter(string imagePath)
    {
        if (DispatcherQueue is null) { return; }

        try
        {
            await Twitter.TweetWithMedia(PostText, imagePath, Replies.FullParameters, RetryWhenImageIsTooLarge);
        }
        catch (TwitterQueryException ex)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                TwitterErrorMessage = Twitter.ExpandExceptionMessage(ex);
                IsOpenTwitterErrorInfo = true;
            });
        }
        catch (Exception ex)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                TwitterErrorMessage = ex.Message;
                IsOpenTwitterErrorInfo = true;
            });
        }
    }

    public void AcceptDropFile(object _, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    public async void ReadDropedFile(object _, DragEventArgs e)
    {
        ReadFile((await e.DataView.GetStorageItemsAsync()).FirstOrDefault());
    }

    [ICommand]
    private async Task ReadFile()
    {
        ReadFile(await StorageHelper.PickSingleFileAsync(".yaml", ".png"));
    }

    [ICommand]
    private void CopyText(string text)
    {
        var data = new DataPackage();
        data.SetText(text);
        Clipboard.SetContent(data);
    }

    public void DisposeMembers(object sender, WindowEventArgs e)
    {
        UpScaler.Dispose();
        Watcher.Dispose();
        Twitter.Dispose();

        var localSettings = ApplicationData.Current.LocalSettings;
        localSettings.Values["postText"] = PostText;
        localSettings.Values["upscaleImageDir"] = UpscaleImageDir;
        localSettings.Values["conceptName"] = ConceptName;
    }
}
