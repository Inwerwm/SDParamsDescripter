using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private Twitter? Twitter
    {
        get; set;
    }
    public DispatcherQueue? DispatcherQueue
    {
        get; set;
    }

    public MainViewModel()
    {
        Separator = new StableDiffusionWebUIDescriptionSeparator();
        _replies = new("", "", Array.Empty<PromptReply>());
        _prompts = new ObservableCollection<PromptReply>();
        _postText = "#StableDiffusion\r\n#Prompt は ALT にあります。";
        _upscaleImageDir = "F:\\Generated\\";
        _conceptName = "";
        _isUpscalingInProgress = false;
        _enableAutoPost = false;
        UpScaler = new RealEsrGan();
        Watcher = new()
        {
            NotifyFilter = NotifyFilters.FileName,
            Filter = "*.png"
        };

        App.MainWindow.Closed += DisposeMembers;
    }

    private void ReadFile(IStorageItem? file)
    {
        if (file is null) return;

        switch (Path.GetExtension(file.Name))
        {
            case ".yaml": ReadYaml(file.Path); break;
            case ".png": UpScale(file.Path); break;
            default: break;
        }
    }

    public void ReadYaml(string yamlFilePath)
    {
        if (!File.Exists(yamlFilePath)) return;

        Replies = Separator.Separate(yamlFilePath);
        Prompts.Clear();
        foreach (var p in Replies.PromptReplies)
        {
            Prompts.Add(p);
        }
    }

    public void UpScale(string imagePath)
    {
        if (DispatcherQueue is null) return;
        if (IsUpscalingInProgress) return;
        if (!File.Exists(imagePath)) return;

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
            var isGeneratedTarget = e.FullPath == savePath;
            if (!isGeneratedTarget) return;

            if (Twitter is not null && EnableAutoPost)
            {
                await Twitter.Post(PostText, savePath, Replies.FullParameters);
            }
            DispatcherQueue.TryEnqueue(() => IsUpscalingInProgress = !isGeneratedTarget);

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

    public void AcceptDropFile(object _, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    public async void ReadDropedFile(object _, DragEventArgs e)
    {
        var storageItems = await e.DataView.GetStorageItemsAsync();
        ReadFile(storageItems.FirstOrDefault());
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
    }
}
