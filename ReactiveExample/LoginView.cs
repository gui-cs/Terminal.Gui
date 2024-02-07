using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui;

namespace ReactiveExample; 

public class LoginView : Window, IViewFor<LoginViewModel> {
    private readonly CompositeDisposable _disposable = new ();

    public LoginView (LoginViewModel viewModel) {
        Title = "Reactive Extensions Example";
        ViewModel = viewModel;
        Label usernameLengthLabel = UsernameLengthLabel (TitleLabel ());
        TextField usernameInput = UsernameInput (usernameLengthLabel);
        Label passwordLengthLabel = PasswordLengthLabel (usernameInput);
        TextField passwordInput = PasswordInput (passwordLengthLabel);
        Label validationLabel = ValidationLabel (passwordInput);
        Button loginButton = LoginButton (validationLabel);
        Button clearButton = ClearButton (loginButton);
        LoginProgressLabel (clearButton);
    }

    public LoginViewModel ViewModel { get; set; }

    object IViewFor.ViewModel { get => ViewModel; set => ViewModel = (LoginViewModel)value; }

    protected override void Dispose (bool disposing) {
        _disposable.Dispose ();
        base.Dispose (disposing);
    }

    private Button ClearButton (View previous) {
        var clearButton = new Button ("Clear") {
                                                   X = Pos.Left (previous),
                                                   Y = Pos.Top (previous) + 1,
                                                   Width = 40
                                               };
        clearButton
            .Events ()
            .Clicked
            .InvokeCommand (ViewModel, x => x.Clear)
            .DisposeWith (_disposable);
        Add (clearButton);

        return clearButton;
    }

    private Button LoginButton (View previous) {
        var loginButton = new Button {
                                         Text = "Login",
                                         X = Pos.Left (previous),
                                         Y = Pos.Top (previous) + 1,
                                         Width = 40
                                     };
        loginButton
            .Events ()
            .Clicked
            .InvokeCommand (ViewModel, x => x.Login)
            .DisposeWith (_disposable);
        Add (loginButton);

        return loginButton;
    }

    private Label LoginProgressLabel (View previous) {
        var progress = "Logging in...";
        var idle = "Press 'Login' to log in.";
        var loginProgressLabel = new Label (idle) {
                                                      X = Pos.Left (previous),
                                                      Y = Pos.Top (previous) + 1,
                                                      Width = 40
                                                  };
        ViewModel
            .WhenAnyObservable (x => x.Login.IsExecuting)
            .Select (executing => executing ? progress : idle)
            .ObserveOn (RxApp.MainThreadScheduler)
            .BindTo (loginProgressLabel, x => x.Text)
            .DisposeWith (_disposable);
        Add (loginProgressLabel);

        return loginProgressLabel;
    }

    private TextField PasswordInput (View previous) {
        var passwordInput = new TextField (ViewModel.Password) {
                                                                   X = Pos.Left (previous),
                                                                   Y = Pos.Top (previous) + 1,
                                                                   Width = 40
                                                               };
        ViewModel
            .WhenAnyValue (x => x.Password)
            .BindTo (passwordInput, x => x.Text)
            .DisposeWith (_disposable);
        passwordInput
            .Events ()
            .TextChanged
            .Select (old => passwordInput.Text)
            .DistinctUntilChanged ()
            .BindTo (ViewModel, x => x.Password)
            .DisposeWith (_disposable);
        Add (passwordInput);

        return passwordInput;
    }

    private Label PasswordLengthLabel (View previous) {
        var passwordLengthLabel = new Label {
                                                X = Pos.Left (previous),
                                                Y = Pos.Top (previous) + 1,
                                                Width = 40
                                            };
        ViewModel
            .WhenAnyValue (x => x.PasswordLength)
            .Select (length => $"Password ({length} characters)")
            .BindTo (passwordLengthLabel, x => x.Text)
            .DisposeWith (_disposable);
        Add (passwordLengthLabel);

        return passwordLengthLabel;
    }

    private Label TitleLabel () {
        var label = new Label ("Login Form");
        Add (label);

        return label;
    }

    private TextField UsernameInput (View previous) {
        var usernameInput = new TextField (ViewModel.Username) {
                                                                   X = Pos.Left (previous),
                                                                   Y = Pos.Top (previous) + 1,
                                                                   Width = 40
                                                               };
        ViewModel
            .WhenAnyValue (x => x.Username)
            .BindTo (usernameInput, x => x.Text)
            .DisposeWith (_disposable);
        usernameInput
            .Events ()
            .TextChanged
            .Select (old => usernameInput.Text)
            .DistinctUntilChanged ()
            .BindTo (ViewModel, x => x.Username)
            .DisposeWith (_disposable);
        Add (usernameInput);

        return usernameInput;
    }

    private Label UsernameLengthLabel (View previous) {
        var usernameLengthLabel = new Label {
                                                X = Pos.Left (previous),
                                                Y = Pos.Top (previous) + 1,
                                                Width = 40
                                            };
        ViewModel
            .WhenAnyValue (x => x.UsernameLength)
            .Select (length => $"Username ({length} characters)")
            .BindTo (usernameLengthLabel, x => x.Text)
            .DisposeWith (_disposable);
        Add (usernameLengthLabel);

        return usernameLengthLabel;
    }

    private Label ValidationLabel (View previous) {
        var error = "Please, enter user name and password.";
        var success = "The input is valid!";
        var validationLabel = new Label (error) {
                                                    X = Pos.Left (previous),
                                                    Y = Pos.Top (previous) + 1,
                                                    Width = 40
                                                };
        ViewModel
            .WhenAnyValue (x => x.IsValid)
            .Select (valid => valid ? success : error)
            .BindTo (validationLabel, x => x.Text)
            .DisposeWith (_disposable);
        ViewModel
            .WhenAnyValue (x => x.IsValid)
            .Select (valid => valid ? Colors.ColorSchemes["Base"] : Colors.ColorSchemes["Error"])
            .BindTo (validationLabel, x => x.ColorScheme)
            .DisposeWith (_disposable);
        Add (validationLabel);

        return validationLabel;
    }
}
