using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
public class LoginViewModel : ReactiveObject
{
    public LoginViewModel ()
    {
        IObservable<bool> canLogin = this.WhenAnyValue (
                                                        x => x.Username,
                                                        x => x.Password,
                                                        (username, password) =>
                                                            !string.IsNullOrEmpty (username) && !string.IsNullOrEmpty (password)
                                                       );

        _isValid = canLogin.ToProperty (this, x => x.IsValid);

        Login = ReactiveCommand.CreateFromTask<EventArgs> (
                                                           e => Task.Delay (TimeSpan.FromSeconds (1)),
                                                           canLogin
                                                          );

        _usernameLength = this
                          .WhenAnyValue (x => x.Username)
                          .Select (name => name.Length)
                          .ToProperty (this, x => x.UsernameLength);

        _passwordLength = this
                          .WhenAnyValue (x => x.Password)
                          .Select (password => password.Length)
                          .ToProperty (this, x => x.PasswordLength);

        Clear = ReactiveCommand.Create<EventArgs> (e => { });

        Clear.Subscribe (
                         unit =>
                         {
                             Username = string.Empty;
                             Password = string.Empty;
                         }
                        );
    }

    private readonly ObservableAsPropertyHelper<bool> _isValid;
    private readonly ObservableAsPropertyHelper<int> _passwordLength;
    private readonly ObservableAsPropertyHelper<int> _usernameLength;

    [IgnoreDataMember]
    public ReactiveCommand<EventArgs, Unit> Clear { get; }

    [IgnoreDataMember]
    public bool IsValid => _isValid.Value;

    [IgnoreDataMember]
    public ReactiveCommand<EventArgs, Unit> Login { get; }

    [Reactive]
    [DataMember]
    public string Password { get; set; } = string.Empty;

    [IgnoreDataMember]
    public int PasswordLength => _passwordLength.Value;

    [Reactive]
    [DataMember]
    public string Username { get; set; } = string.Empty;

    [IgnoreDataMember]
    public int UsernameLength => _usernameLength.Value;
}
