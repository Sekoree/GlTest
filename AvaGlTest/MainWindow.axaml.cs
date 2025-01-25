using Avalonia.Controls;
using Avalonia.Rendering;

namespace AvaGlTest;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
    }
}