using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui.Configuration;
using Terminal.Gui.Views;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

namespace ReactiveExample;

public class LoginView : Window, IViewFor<LoginViewModel>
{
    private const string SuccessMessage = "The input is valid!";
    private const string ErrorMessage = "Please enter a valid user name and password.";
    private const string ProgressMessage = "Logging in...";
    private const string IdleMessage = "Press 'Login' to log in.";

    private readonly CompositeDisposable _disposable = [];

    public LoginView (LoginViewModel viewModel)
    {
        Title = $"Reactive Extensions Example - {Application.QuitKey} to Exit";
        ViewModel = viewModel;
        var title = this.AddControl<Label> (x => x.Text = "Login Form");
        var unLengthLabel = title.AddControlAfter<Label> ((previous, unLength) =>
            {
                unLength.X = Pos.Left (previous);
                unLength.Y = Pos.Top (previous) + 1;

                ViewModel
                    .WhenAnyValue (x => x.UsernameLength)
                    .Select (length => $"_Username ({length} characters):")
                    .BindTo (unLength, x => x.Text)
                    .DisposeWith (_disposable);
            });
        unLengthLabel.AddControlAfter<TextField> ((previous, unInput) =>
            {
                unInput.X = Pos.Right (previous) + 1;
                unInput.Y = Pos.Top (previous);
                unInput.Width = 40;
                unInput.Text = ViewModel.Username;

                ViewModel
                    .WhenAnyValue (x => x.Username)
                    .BindTo (unInput, x => x.Text)
                    .DisposeWith (_disposable);

                unInput
                    .Events ()
                    .TextChanged
                    .Select (_ => unInput.Text)
                    .DistinctUntilChanged ()
                    .BindTo (ViewModel, x => x.Username)
                    .DisposeWith (_disposable);
            });
        unLengthLabel.AddControlAfter<Label> ((previous, pwLength) =>
            {
                pwLength.X = Pos.Left (previous);
                pwLength.Y = Pos.Top (previous) + 1;

                ViewModel
                    .WhenAnyValue (x => x.PasswordLength)
                    .Select (length => $"_Password ({length} characters):")
                    .BindTo (pwLength, x => x.Text)
                    .DisposeWith (_disposable);
            })
            .AddControlAfter<TextField> ((previous, pwInput) =>
            {
                pwInput.X = Pos.Right (previous) + 1;
                pwInput.Y = Pos.Top (previous);
                pwInput.Width = 40;
                pwInput.Text = ViewModel.Password;

                ViewModel
                    .WhenAnyValue (x => x.Password)
                    .BindTo (pwInput, x => x.Text)
                    .DisposeWith (_disposable);

                pwInput
                    .Events ()
                    .TextChanged
                    .Select (_ => pwInput.Text)
                    .DistinctUntilChanged ()
                    .BindTo (ViewModel, x => x.Password)
                    .DisposeWith (_disposable);
            })
            .AddControlAfter<Label> ((previous, validation) =>
            {
                validation.X = Pos.Left (previous);
                validation.Y = Pos.Top (previous) + 1;
                validation.Text = ErrorMessage;

                ViewModel
                    .WhenAnyValue (x => x.IsValid)
                    .Select (valid => valid ? SuccessMessage : ErrorMessage)
                    .BindTo (validation, x => x.Text)
                    .DisposeWith (_disposable);

                ViewModel
                    .WhenAnyValue (x => x.IsValid)
                    .Select (valid => valid ? SchemeManager.GetScheme ("Base") : SchemeManager.GetScheme ("Error"))
                    .Subscribe (scheme => validation.SetScheme (scheme))
                    .DisposeWith (_disposable);
            })
            .AddControlAfter<Button> ((previous, login) =>
            {
                login.X = Pos.Left (previous);
                login.Y = Pos.Top (previous) + 1;
                login.Text = "_Login";

                login
                    .Events ()
                    .Accepting
                    .InvokeCommand (ViewModel, x => x.Login)
                    .DisposeWith (_disposable);
            })
            .AddControlAfter<Button> ((previous, clear) =>
            {
                clear.X = Pos.Left (previous);
                clear.Y = Pos.Top (previous) + 1;
                clear.Text = "_Clear";

                clear
                    .Events ()
                    .Accepting
                    .InvokeCommand (ViewModel, x => x.ClearCommand)
                    .DisposeWith (_disposable);
            })
            .AddControlAfter<Label> ((previous, progress) =>
            {
                progress.X = Pos.Left (previous);
                progress.Y = Pos.Top (previous) + 1;
                progress.Width = 40;
                progress.Height = 1;
                progress.Text = IdleMessage;

                ViewModel
                    .WhenAnyObservable (x => x.Login.IsExecuting)
                    .Select (executing => executing ? ProgressMessage : IdleMessage)
                    .ObserveOn (RxApp.MainThreadScheduler)
                    .BindTo (progress, x => x.Text)
                    .DisposeWith (_disposable);
            });
    }

    public LoginViewModel ViewModel { get; set; }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (LoginViewModel)value;
    }

    protected override void Dispose (bool disposing)
    {
        _disposable.Dispose ();
        base.Dispose (disposing);
    }
}
