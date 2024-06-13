using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Terminal.Gui;

namespace CommunityToolkitExample;

internal partial class LoginViewModel : ObservableObject
{
    private const string DEFAULT_LOGIN_PROGRESS_MESSAGE = "Press 'Login' to log in.";
    private const string LOGGING_IN_PROGRESS_MESSAGE = "Logging in...";
    private const string VALID_LOGIN_MESSAGE = "The input is valid!";
    private const string INVALID_LOGIN_MESSAGE = "Please enter a valid user name and password.";

    [ObservableProperty]
    private bool _canLogin;

    private string _password;

    [ObservableProperty]
    private string _passwordLengthMessage;

    private string _username;

    [ObservableProperty]
    private string _usernameLengthMessage;

    [ObservableProperty]
    private string _loginProgressMessage;

    [ObservableProperty]
    private string _validationMessage;

    [ObservableProperty]
    private ColorScheme? _validationColorScheme;

    public LoginViewModel ()
    {
        Username = string.Empty;
        Password = string.Empty;

        ClearCommand = new (Clear);
        LoginCommand = new (Execute);

        Clear ();

        return;

        async void Execute () { await Login (); }
    }

    public RelayCommand ClearCommand { get; }

    public RelayCommand LoginCommand { get; }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty (ref _password, value);
            PasswordLengthMessage = $"_Password ({_password.Length} characters):";
            ValidateLogin ();
        }
    }

    private void ValidateLogin ()
    {
        CanLogin = !string.IsNullOrEmpty (Username) && !string.IsNullOrEmpty (Password);
        SendMessage (LoginAction.Validation);
    }

    public string Username
    {
        get => _username;
        set
        {
            SetProperty (ref _username, value);
            UsernameLengthMessage = $"_Username ({_username.Length} characters):";
            ValidateLogin ();
        }
    }

    private void Clear ()
    {
        Username = string.Empty;
        Password = string.Empty;
        SendMessage (LoginAction.Validation);
        SendMessage (LoginAction.LoginProgress, DEFAULT_LOGIN_PROGRESS_MESSAGE);
    }

    private async Task Login ()
    {
        SendMessage (LoginAction.LoginProgress, LOGGING_IN_PROGRESS_MESSAGE);
        await Task.Delay (TimeSpan.FromSeconds (1));
        Clear ();
    }

    private void SendMessage (LoginAction loginAction, string message = "")
    {
        switch (loginAction)
        {
            case LoginAction.LoginProgress:
                LoginProgressMessage = message;
                break;
            case LoginAction.Validation:
                ValidationMessage = CanLogin ? VALID_LOGIN_MESSAGE : INVALID_LOGIN_MESSAGE;
                ValidationColorScheme = CanLogin ? Colors.ColorSchemes ["Base"] : Colors.ColorSchemes ["Error"];
                break;
        }
        WeakReferenceMessenger.Default.Send (new Message<LoginAction> { Value = loginAction });
    }

    public void Initialized ()
    {
        Clear ();
    }
}
