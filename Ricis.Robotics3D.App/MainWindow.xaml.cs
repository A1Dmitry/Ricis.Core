using System.Windows;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Presentation shell only. State and scene construction are owned by the ViewModel/application layer.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
    }
}
