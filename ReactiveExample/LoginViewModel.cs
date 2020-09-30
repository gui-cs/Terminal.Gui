using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ReactiveExample {
	[DataContract]
	public class LoginViewModel : ReactiveObject {
		readonly ObservableAsPropertyHelper<int> _usernameLength;
		readonly ObservableAsPropertyHelper<int> _passwordLength;
		readonly ObservableAsPropertyHelper<bool> _isValid;
		
		public LoginViewModel () {
			var canLogin = this.WhenAnyValue (
				x => x.Username, 
				x => x.Password,
				(username, password) =>
					!string.IsNullOrWhiteSpace (username) &&
					!string.IsNullOrWhiteSpace (password));
			
			_isValid = canLogin.ToProperty (this, x => x.IsValid);
			Login = ReactiveCommand.CreateFromTask (
				() => Task.Delay (TimeSpan.FromSeconds (1)),
				canLogin);
			
			_usernameLength = this
				.WhenAnyValue (x => x.Username)
				.Select (name => name.Length)
				.ToProperty (this, x => x.UsernameLength);
			_passwordLength = this
				.WhenAnyValue (x => x.Password)
				.Select (password => password.Length)
				.ToProperty (this, x => x.PasswordLength);
		}
		
		[Reactive, DataMember]
		public string Username { get; set; } = string.Empty;
		
		[Reactive, DataMember]
		public string Password { get; set; } = string.Empty;
		
		[IgnoreDataMember]
		public int UsernameLength => _usernameLength.Value;
		
		[IgnoreDataMember]
		public int PasswordLength => _passwordLength.Value;

		[IgnoreDataMember]
		public ReactiveCommand<Unit, Unit> Login { get; }
		
		[IgnoreDataMember]
		public bool IsValid => _isValid.Value;
	}
}