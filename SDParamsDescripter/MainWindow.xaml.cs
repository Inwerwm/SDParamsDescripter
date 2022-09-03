using Microsoft.UI;
using Microsoft.UI.Windowing;
using SDParamsDescripter.Helpers;
using WinRT.Interop;

namespace SDParamsDescripter;

public sealed partial class MainWindow : WindowEx
{
    private AppWindow _appWindow;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        _appWindow = GetAppWindowForCurrentWindow();
        _appWindow.Title = "Stable Diffusion Parameters Descripter";
        SetTitleBarColors();
    }

    private AppWindow GetAppWindowForCurrentWindow()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(wndId);
    }

    private bool SetTitleBarColors()
    {
        // Check to see if customization is supported.
        // Currently only supported on Windows 11.
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            if (_appWindow is null)
            {
                _appWindow = GetAppWindowForCurrentWindow();
            }
            var titleBar = _appWindow.TitleBar;

            var backgroundColor = ColorHelper.FromArgb(0xFF, 0x33, 0x33, 0x33);

            // Set active window colors
            titleBar.ForegroundColor = Colors.Gainsboro;
            titleBar.BackgroundColor = backgroundColor;
            titleBar.ButtonForegroundColor = Colors.Gainsboro;
            titleBar.ButtonBackgroundColor = backgroundColor;
            titleBar.ButtonHoverForegroundColor = Colors.Gainsboro;
            titleBar.ButtonHoverBackgroundColor = Colors.Gray;
            titleBar.ButtonPressedForegroundColor = Colors.Gray;
            titleBar.ButtonPressedBackgroundColor = backgroundColor;

            // Set inactive window colors
            titleBar.InactiveForegroundColor = Colors.Gainsboro;
            titleBar.InactiveBackgroundColor = backgroundColor;
            titleBar.ButtonInactiveForegroundColor = Colors.Gainsboro;
            titleBar.ButtonInactiveBackgroundColor = backgroundColor;
            return true;
        }
        return false;
    }
}
