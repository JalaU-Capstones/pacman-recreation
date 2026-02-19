using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PacmanGame.Views;

public partial class GlobalLeaderboardView : UserControl
{
    public GlobalLeaderboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
