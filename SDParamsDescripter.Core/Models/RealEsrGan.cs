using System.Diagnostics;

namespace SDParamsDescripter.Core.Models;
public class RealEsrGan
{
    private bool disposedValue;

    private Process Process { get; }

    public StreamWriter StandardInput => Process.StandardInput;

    public RealEsrGan()
    {
        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "C:\\Users\\owner\\AppData\\Local\\Microsoft\\WindowsApps\\Microsoft.PowerShell_8wekyb3d8bbwe\\pwsh.exe",
                Arguments = $"-ExecutionPolicy ByPass -NoExit -Command \"& 'C:\\ProgramData\\Anaconda3\\shell\\condabin\\conda-hook.ps1' ; conda activate 'C:\\Users\\owner\\anaconda3\\envs\\ldm' \"",
                WorkingDirectory = "D:\\stable-diffusion\\",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false,
            }
        };
        Process.Start();
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
