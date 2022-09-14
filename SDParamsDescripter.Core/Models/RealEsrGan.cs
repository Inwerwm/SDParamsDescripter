using System.Diagnostics;

namespace SDParamsDescripter.Core.Models;
public class RealEsrGan : IDisposable
{
    private bool disposedValue;

    private Process Process { get; }

    public StreamWriter StandardInput => Process.StandardInput;

    private FileSystemWatcher Watcher
    {
        get;
    }

    public static string GetModelName() => "4x-UltraSharp";

    public RealEsrGan()
    {
        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                WorkingDirectory = "D:\\stable-diffusion\\ESRGAN",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false,
            }
        };
        Process.Start();

        Watcher = new()
        {
            NotifyFilter = NotifyFilters.FileName,
        };
    }

    public async Task RunAsync(string filePath, string savePath, CancellationToken token)
    {
        Watcher.Filter = Path.GetFileName(savePath);
        Watcher.Path = Path.GetDirectoryName(savePath);
        var fileChanged = false;
        void endWatch()
        {
            Watcher.EnableRaisingEvents = false;
            Watcher.Created -= onFilesChanged;
            Watcher.Changed -= onFilesChanged;
            Watcher.Error -= onError;
        }

        void onFilesChanged(object _, FileSystemEventArgs e)
        {
            fileChanged = true;
            endWatch();
        }
        void onError(object _, ErrorEventArgs e)
        {
            onFilesChanged(null, null);
        }

        Watcher.Created += onFilesChanged;
        Watcher.Changed += onFilesChanged;
        Watcher.Error += onError;

        Watcher.EnableRaisingEvents = true;

        Run(filePath, savePath);

        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && !fileChanged)
            {
                await Task.Delay(500);
            }

            if (!fileChanged) { endWatch(); }
        }, token);
    }

    public void Run(string filePath, string savePath)
    {
        StandardInput.WriteLine($"python upscale.py --source-path \"{filePath}\" --save-path \"{savePath}\"");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Process?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
