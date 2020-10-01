using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NStack;
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
					!ustring.IsNullOrEmpty (username) &&
					!ustring.IsNullOrEmpty (password));
			
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
			
			Clear = ReactiveCommand.Create (() => { });
			Clear.Subscribe (unit => {
				Username = ustring.Empty;
				Password = ustring.Empty;
			});
		}
		
		[Reactive, DataMember]
		public ustring Username { get; set; } = ustring.Empty;
		
		[Reactive, DataMember]
		public ustring Password { get; set; } = ustring.Empty;
		
		[IgnoreDataMember]
		public int UsernameLength => _usernameLength.Value;
		
		[IgnoreDataMember]
		public int PasswordLength => _passwordLength.Value;

		[IgnoreDataMember]
		public ReactiveCommand<Unit, Unit> Login { get; }
		
		[IgnoreDataMember]
		public ReactiveCommand<Unit, Unit> Clear { get; }
		
		[IgnoreDataMember]
		public bool IsValid => _isValid.Value;
	}
}