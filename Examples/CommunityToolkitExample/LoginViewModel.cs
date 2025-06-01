using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;

namespace CommunityToolkitExample;

internal partial class LoginViewModel : ObservableObject
{
    private const string DEFAULT_LOGIN_PROGRESS_MESSAGE = "Press 'Login' to log in.";
    private const string INVALID_LOGIN_MESSAGE = "Please enter a valid user name and password.";
    private const string LOGGING_IN_PROGRESS_MESSAGE = "Logging in...";
    private const string VALID_LOGIN_MESSAGE = "The input is valid!";
    
    [ObservableProperty]
    private bool _canLogin;

    [ObservableProperty]
    private string _loginProgressMessage;

    private string _password;

    [ObservableProperty]
    private string _passwordLengthMessage;

    private string _username;

    [ObservableProperty]
    private string _usernameLengthMessage;
    
    [ObservableProperty]
    private Scheme? _validationScheme;

    [ObservableProperty]
    private string _validationMessage;
    public LoginViewModel ()
    {
        _loginProgressMessage = string.Empty;
        _password = string.Empty;
        _passwordLengthMessage = string.Empty;
        _username = string.Empty;
        _usernameLengthMessage = string.Empty;
        _validationMessage = string.Empty;

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

    public void Initialized ()
    {
        Clear ();
    }

    private void Clear ()
    {
        Username = string.Empty;
        Password = string.Empty;
        SendMessage (LoginActions.Clear, DEFAULT_LOGIN_PROGRESS_MESSAGE);
    }

    private async Task Login ()
    {
        SendMessage (LoginActions.LoginProgress, LOGGING_IN_PROGRESS_MESSAGE);
        await Task.Delay (TimeSpan.FromSeconds (1));
        Clear ();
    }

    private void SendMessage (LoginActions loginAction, string message = "")
    {
        switch (loginAction)
        {
             case LoginActions.Clear:
                LoginProgressMessage = message;
                ValidationMessage = INVALID_LOGIN_MESSAGE;
                ValidationScheme = SchemeManager.GetScheme ("Error");
                break;
            case LoginActions.LoginProgress:
                LoginProgressMessage = message;
                break;
            case LoginActions.Validation:
                ValidationMessage = CanLogin ? VALID_LOGIN_MESSAGE : INVALID_LOGIN_MESSAGE;
                ValidationScheme = CanLogin ? SchemeManager.GetScheme ("Base") : SchemeManager.GetScheme("Error");
                break;
        }
        WeakReferenceMessenger.Default.Send (new Message<LoginActions> { Value = loginAction });
    }

    private void ValidateLogin ()
    {
        CanLogin = !string.IsNullOrEmpty (Username) && !string.IsNullOrEmpty (Password);
        SendMessage (LoginActions.Validation);
    }
}
