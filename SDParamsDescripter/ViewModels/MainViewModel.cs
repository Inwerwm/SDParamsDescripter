using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinqToTwitter.Common;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SDParamsDescripter.Core.Contracts;
using SDParamsDescripter.Core.Models;
using SDParamsDescripter.Core.Twitter;
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
    private ObservableCollection<ImageTask> _imageTaskQueue;
    private bool IsRunningQueueLoop
    {
        get;
        set;
    }

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
    private CancellationTokenSource Cancellation
    {
        get;
    }

    private TwitterAccess Twitter
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
        _imageTaskQueue = new();
        IsRunningQueueLoop = false;

        _doesUseAnimeModel = false;
        _isUpscalingInProgress = false;
        _enableAutoPost = false;
        _retryWhenImageIsTooLarge = true;

        _isOpenTwitterErrorInfo = false;
        _twitterErrorMessage = "";

        Separator = new StableDiffusionWebUIDescriptionSeparator();
        UpScaler = new RealEsrGan();
        Cancellation = new();

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
            case ".png": AddImageTask(file.Path); break;
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

    private void AddImageTask(string imagePath)
    {
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

        ImageTaskQueue.Add(new(
            imagePath,
            new(PostText.Replace("\r\n", "\n").Replace("\r", "\n"), savePath, Replies.FullParameters, RetryWhenImageIsTooLarge),
            EnableAutoPost,
            DoesUseAnimeModel));

        RunAllImageTasks();
    }

    private async Task RunAllImageTasks()
    {
        if (IsRunningQueueLoop) { return; }
        IsRunningQueueLoop = true;

        while (ImageTaskQueue.Any())
        {
            var current = ImageTaskQueue.FirstOrDefault();
            if (current is null)
            {
                TwitterErrorMessage = "Could not take element of queue.";
                IsOpenTwitterErrorInfo = true;
                break;
            }

            current.IsProgress = true;
            await UpScaler.RunAsync(current.ImagePath, current.SavePath, current.DoesAnimeModel, Cancellation.Token);

            if (current.EnablePost)
            {
                IsOpenTwitterErrorInfo = false;
                await PostToTwitter(current.Tweet);
            }

            ImageTaskQueue.Remove(current);
        }

        IsRunningQueueLoop = false;
    }

    private async Task PostToTwitter(Tweet tweet)
    {
        if (DispatcherQueue is null) { return; }

        try
        {
            await Twitter.TweetWithMedia(tweet);
        }
        catch (TwitterQueryException ex)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                TwitterErrorMessage = TwitterAccess.ExpandExceptionMessage(ex);
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

        var dropedItems = await e.DataView.GetStorageItemsAsync();
        if (dropedItems.Count > 1)
        {
            // 複数のファイルが投げ込まれたら png ファイルだけ処理する
            foreach (var png in dropedItems.Where(file => Path.GetExtension(file.Name) == ".png"))
            {
                ReadFile(png);
            }
        }
        else
        {
            ReadFile(dropedItems.FirstOrDefault());
        }
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
        Cancellation.Cancel();
        Cancellation.Dispose();
        UpScaler.Dispose();
        Twitter.Dispose();

        var localSettings = ApplicationData.Current.LocalSettings;
        localSettings.Values["postText"] = PostText;
        localSettings.Values["upscaleImageDir"] = UpscaleImageDir;
        localSettings.Values["conceptName"] = ConceptName;
    }
}
