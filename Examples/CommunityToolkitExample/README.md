# CommunityToolkit.MVVM Example

This small demo gives an example of using the `CommunityToolkit.MVVM` framework's `ObservableObject`, `ObservableProperty`, and `IRecipient<T>` in conjunction with `Microsoft.Extensions.DependencyInjection`. 

Right away we use IoC to load our views and view models.

``` csharp
// As a public property for access further in the application if needed. 
public static IServiceProvider Services { get; private set; }
...
// In Main
Services = ConfigureServices ();
...
private static IServiceProvider ConfigureServices ()
{
    var services = new ServiceCollection ();
    services.AddTransient<LoginView> ();
    services.AddTransient<LoginViewModel> ();
    return services.BuildServiceProvider ();
}
```

Now, we start the app and get our main view.

``` csharp
Application.Run (Services.GetRequiredService<LoginView> ());
```

Our view implements `IRecipient<T>` to demonstrate the use of the `WeakReferenceMessenger`. The binding of the view events is then created.

``` csharp
internal partial class LoginView : IRecipient<Message<LoginAction>>
{
    public LoginView (LoginViewModel viewModel)
    {
        // Initialize our Receive method
        WeakReferenceMessenger.Default.Register (this);
        ...
        ViewModel = viewModel;
        ...
        passwordInput.TextChanged += (_, _) =>
                                     {
                                         ViewModel.Password = passwordInput.Text;
                                         SetText ();
                                     };
        loginButton.Accept += (_, _) =>
                              {
                                  if (!ViewModel.CanLogin) { return; }
                                  ViewModel.LoginCommand.Execute (null);
                              };
        ...
        // Let the view model know the view is intialized.
        Initialized += (_, _) => { ViewModel.Initialized (); };
    }
    ...
}
```

Momentarily slipping over to the view model, all bindable properties use some form of `ObservableProperty` with the class deriving from `ObservableObject`. Commands are of the `RelayCommand` type. The use of `ObservableProperty` generates the code for handling `INotifyPropertyChanged` and `INotifyPropertyChanging`.

``` csharp
internal partial class LoginViewModel : ObservableObject
{
    ...
    [ObservableProperty]
    private bool _canLogin;

    private string _password;
    ...
    public LoginViewModel ()
    {
        ...
        Password = string.Empty;
        ...   
        LoginCommand = new (Execute);

        Clear ();

        return;

        async void Execute () { await Login (); }
    }
    ...
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
```

The use of `WeakReferenceMessenger` provides one method of signaling the view from the view model. It's just one way to handle cross-thread messaging in this framework.

``` csharp
...
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
            ValidationScheme = CanLogin ? Colors.Schemes ["Base"] : Colors.Schemes ["Error"];
            break;
    }
    WeakReferenceMessenger.Default.Send (new Message<LoginAction> { Value = loginAction });
}

private void ValidateLogin ()
{
    CanLogin = !string.IsNullOrEmpty (Username) && !string.IsNullOrEmpty (Password);
    SendMessage (LoginAction.Validation);
}
...
```

And the view's `Receive` function which provides an `Application.Refresh()` call to update the UI immediately.

``` csharp
public void Receive (Message<LoginAction> message)
{
    switch (message.Value)
    {
        case LoginAction.LoginProgress:
            {
                loginProgressLabel.Text = ViewModel.LoginProgressMessage;
                break;
            }
        case LoginAction.Validation:
            {
                validationLabel.Text = ViewModel.ValidationMessage;
                validationLabel.Scheme = ViewModel.ValidationScheme;
                break;
            }
    }
    SetText();
    Application.Refresh ();
}
```
