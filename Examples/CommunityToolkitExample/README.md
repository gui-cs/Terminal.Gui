# CommunityToolkit.MVVM Example

This small demo gives an example of using the `CommunityToolkit.MVVM` framework's `ObservableObject`, `ObservableProperty`, and `IRecipient<T>` in conjunction with `Microsoft.Extensions.DependencyInjection`. 

Right away we use IoC to load our views and view models.

``` csharp
// As a public property for access further in the application if needed. 
public static IServiceProvider? Services { get; private set; }
...
// In Main
ConfigurationManager.Enable (ConfigLocations.All);
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

Now, we start the app using the modern Terminal.Gui model and get our main view.

``` csharp
using IApplication app = Application.Create ();
app.Init ();
using var loginView = Services.GetRequiredService<LoginView> ();
app.Run (loginView);
```

Our view implements `IRecipient<T>` to demonstrate the use of the `WeakReferenceMessenger`. The binding of the view events is then created.

``` csharp
internal partial class LoginView : IRecipient<Message<LoginActions>>
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
                                     };
        loginButton.Accepting += (_, e) =>
                              {
                                  if (!ViewModel.CanLogin) { return; }
                                  ViewModel.LoginCommand.Execute (null);
                                  // When Accepting is handled, set e.Handled to true to prevent further processing.
                                  e.Handled = true;
                              };
        ...
        // Let the view model know the view is initialized.
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
    SendMessage (LoginActions.LoginProgress, LOGGING_IN_PROGRESS_MESSAGE);
    await Task.Delay (TimeSpan.FromSeconds (1));
    Clear ();
}

private void SendMessage (LoginActions loginAction, string message = "")
{
    switch (loginAction)
    {
        case LoginActions.LoginProgress:
            LoginProgressMessage = message;
            break;
        case LoginActions.Validation:
            ValidationMessage = CanLogin ? VALID_LOGIN_MESSAGE : INVALID_LOGIN_MESSAGE;
            ValidationScheme = CanLogin ? SchemeManager.GetScheme ("Base") : SchemeManager.GetScheme ("Error");
            break;
    }
    WeakReferenceMessenger.Default.Send (new Message<LoginActions> { Value = loginAction });
}

private void ValidateLogin ()
{
    CanLogin = !string.IsNullOrEmpty (Username) && !string.IsNullOrEmpty (Password);
    SendMessage (LoginActions.Validation);
}
...
```

The view's `Receive` function updates the UI based on messages from the view model. In the modern Terminal.Gui model, UI updates are automatically refreshed, so no manual `Application.Refresh()` call is needed.

``` csharp
public void Receive (Message<LoginActions> message)
{
    switch (message.Value)
    {
        case LoginActions.LoginProgress:
            {
                loginProgressLabel.Text = ViewModel.LoginProgressMessage;
                break;
            }
        case LoginActions.Validation:
            {
                validationLabel.Text = ViewModel.ValidationMessage;
                validationLabel.SetScheme (ViewModel.ValidationScheme);
                break;
            }
    }
    SetText ();
}
```
