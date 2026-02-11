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

    private bool _isYou;
    public bool IsYou
    {
        get => _isYou;
        set => this.RaiseAndSetIfChanged(ref _isYou, value);
    }

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        set => this.RaiseAndSetIfChanged(ref _isAdmin, value);
    }
}
