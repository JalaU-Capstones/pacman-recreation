using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PacmanGame.ViewModels;

namespace PacmanGame.Views;

public partial class CustomLevelsLibraryView : UserControl
{
    public CustomLevelsLibraryView()
    {
        InitializeComponent();
    }

    public CustomLevelsLibraryViewModel? ViewModel => DataContext as CustomLevelsLibraryViewModel;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
