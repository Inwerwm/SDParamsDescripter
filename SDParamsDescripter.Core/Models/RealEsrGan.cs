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

    public RealEsrGan()
    {
        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "C:\\Users\\owner\\AppData\\Local\\Microsoft\\WindowsApps\\Microsoft.PowerShell_8wekyb3d8bbwe\\pwsh.exe",
                Arguments = $"-ExecutionPolicy ByPass -NoExit -Command \"& 'C:\\ProgramData\\Anaconda3\\shell\\condabin\\conda-hook.ps1' ; conda activate 'C:\\Users\\owner\\.conda\\envs\\ldm' \"",
                WorkingDirectory = "D:\\stable-diffusion\\",
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

    public async Task RunAsync(string filePath, string savePath, bool isAnime, CancellationToken token)
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

        Run(filePath, savePath, isAnime);

        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && !fileChanged)
            {
                await Task.Delay(500);
            }

            if (!fileChanged) { endWatch(); }
        }, token);
    }

    public void Run(string filePath, string savePath, bool isAnime)
    {
        StandardInput.WriteLine($"python scripts/realesrgan_only.py --file-path {filePath} --save-path {savePath} --realesrgan-model {(isAnime ? "RealESRGAN_x4plus_anime_6B" : "RealESRGAN_x4plus")}");
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
