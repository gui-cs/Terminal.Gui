using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Terminal.Gui.Input;

namespace ReactiveExample;

//
// This view model can be easily shared across different UI frameworks.
// For example, if you have a WPF or XF app with view models written
// this way, you can easily port your app to Terminal.Gui by implementing
// the views with Terminal.Gui classes and ReactiveUI bindings.
//
// We mark the view model with the [DataContract] attributes and this
// allows you to save the view model class to the disk, and then to read
// the view model from the disk, making your app state persistent.
// See also: https://www.reactiveui.net../docs/handbook/data-persistence/
//
[DataContract]
public partial class LoginViewModel : ReactiveObject
{
    [IgnoreDataMember]
    [ObservableAsProperty] private bool _isValid;

    [IgnoreDataMember]
    [ObservableAsProperty] private int _passwordLength;

    [IgnoreDataMember]
    [ObservableAsProperty] private int _usernameLength;

    [DataMember]
    [Reactive] private string _password = string.Empty;

    [DataMember]
    [Reactive] private string _username = string.Empty;

    public LoginViewModel ()
    {
        IObservable<bool> canLogin = this.WhenAnyValue
            (
                x => x.Username,
                x => x.Password,
                (username, password) =>
                    !string.IsNullOrEmpty (username) && !string.IsNullOrEmpty (password)
            );

        _isValidHelper = canLogin.ToProperty (this, x => x.IsValid);

        Login = ReactiveCommand.CreateFromTask<CommandEventArgs>
            (
                e => Task.Delay (TimeSpan.FromSeconds (1)),
                canLogin
            );

        _usernameLengthHelper = this
                          .WhenAnyValue (x => x.Username)
                          .Select (name => name.Length)
                          .ToProperty (this, x => x.UsernameLength);

        _passwordLengthHelper = this
                          .WhenAnyValue (x => x.Password)
                          .Select (password => password.Length)
                          .ToProperty (this, x => x.PasswordLength);

        ClearCommand.Subscribe (
                         unit =>
                         {
                             Username = string.Empty;
                             Password = string.Empty;
                         }
                        );
    }

    [ReactiveCommand]
    public void Clear (CommandEventArgs args) { }

    [IgnoreDataMember]
    public ReactiveCommand<CommandEventArgs, Unit> Login { get; }
}
