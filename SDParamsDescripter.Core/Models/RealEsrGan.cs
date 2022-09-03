using System.Diagnostics;
using SDParamsDescripter.Core.Contracts;

namespace SDParamsDescripter.Core.Models;
public class RealEsrGan : IShell
{
    private bool disposedValue;
    private CancellationTokenSource _canceller;

    private Process Process { get; }

    public StreamWriter StandardInput => Process.StandardInput;
    public StreamReader StandardOutput => Process.StandardOutput;

    public RealEsrGan()
    {
        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "C:\\Users\\owner\\AppData\\Local\\Microsoft\\WindowsApps\\Microsoft.PowerShell_8wekyb3d8bbwe\\pwsh.exe",
                Arguments = $"-ExecutionPolicy ByPass -NoExit -Command \"& 'C:\\ProgramData\\Anaconda3\\shell\\condabin\\conda-hook.ps1' ; conda activate 'C:\\Users\\owner\\anaconda3\\envs\\ldo' \"",
                WorkingDirectory = "D:\\stable-diffusion\\",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        Process.Start();
        _canceller = new();
        StartReadOutput(_canceller.Token);
    }

    void StartReadOutput(CancellationToken token)
    {
        Task.Run(() =>
        {
            while (!(token.IsCancellationRequested || (StandardOutput?.EndOfStream ?? true)))
            {
                Console.WriteLine(StandardOutput?.ReadLine());
            }
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
                _canceller.Cancel();
                _canceller?.Dispose();
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
