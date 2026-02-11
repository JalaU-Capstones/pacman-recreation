using PacmanGame.Shared;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class PlayerViewModel : ViewModelBase
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private PlayerRole _role;
    public PlayerRole Role
    {
        get => _role;
        set => this.RaiseAndSetIfChanged(ref _role, value);
    }
}
