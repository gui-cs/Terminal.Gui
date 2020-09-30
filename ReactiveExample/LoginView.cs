using System.Reactive.Disposables;
using System.Reactive.Linq;
using NStack;
using ReactiveUI;
using Terminal.Gui;

namespace ReactiveExample {
	public class LoginView : Window, IViewFor<LoginViewModel> {
		readonly CompositeDisposable _disposable = new CompositeDisposable();
		
		public LoginView (LoginViewModel viewModel) : base("Reactive Extensions Example") {
			ViewModel = viewModel;
			this.StackPanel (new Label ("Login Form"))
				.Append (UsernameLengthLabel ())
				.Append (UsernameInput ())
				.Append (PasswordLengthLabel ())
				.Append (PasswordInput ())
				.Append (ValidationLabel ())
				.Append (LoginButton ())
				.Append (LoginProgressLabel ());
		}
		
		public LoginViewModel ViewModel { get; set; }

		protected override void Dispose (bool disposing) {
			_disposable.Dispose ();
			base.Dispose (disposing);
		}

		TextField UsernameInput () {
			var usernameInput = new TextField (ViewModel.Username) { Width = 40 };
			ViewModel
				.WhenAnyValue (x => x.Username)
				.BindTo (usernameInput, x => x.Text)
				.DisposeWith (_disposable);
			usernameInput
				.Events ()
				.TextChanged
				.Select (old => usernameInput.Text.ToString ())
				.DistinctUntilChanged ()
				.BindTo (ViewModel, x => x.Username)
				.DisposeWith (_disposable);
			return usernameInput;
		}

		Label UsernameLengthLabel () {
			var usernameLengthLabel = new Label { Width = 40 };
			ViewModel
				.WhenAnyValue (x => x.UsernameLength)
				.Select (length => ustring.Make ($"Username ({length} characters)"))
				.BindTo (usernameLengthLabel, x => x.Text)
				.DisposeWith (_disposable);
			return usernameLengthLabel;
		}

		TextField PasswordInput () {
			var passwordInput = new TextField (ViewModel.Password) { Width = 40 };
			ViewModel
				.WhenAnyValue (x => x.Password)
				.BindTo (passwordInput, x => x.Text)
				.DisposeWith (_disposable);
			passwordInput
				.Events ()
				.TextChanged
				.Select (old => passwordInput.Text.ToString ())
				.DistinctUntilChanged ()
				.BindTo (ViewModel, x => x.Password)
				.DisposeWith (_disposable);
			return passwordInput;
		}

		Label PasswordLengthLabel () {
			var passwordLengthLabel = new Label { Width = 40 };
			ViewModel
				.WhenAnyValue (x => x.PasswordLength)
				.Select (length => ustring.Make ($"Password ({length} characters)"))
				.BindTo (passwordLengthLabel, x => x.Text)
				.DisposeWith (_disposable);
			return passwordLengthLabel;
		}

		Label ValidationLabel () {
			var error = ustring.Make("Please, enter user name and password.");
			var success = ustring.Make("The input is valid!");
			var validationLabel = new Label(error) { Width = 40 };
			ViewModel
				.WhenAnyValue (x => x.IsValid)	
				.Select (valid => valid ? success : error)
				.BindTo (validationLabel, x => x.Text)
				.DisposeWith (_disposable);
			ViewModel
				.WhenAnyValue (x => x.IsValid)	
				.Select (valid => valid ? Colors.Base : Colors.Error)
				.BindTo (validationLabel, x => x.ColorScheme)
				.DisposeWith (_disposable);
			return validationLabel;
		}

		Label LoginProgressLabel () {
			var progress = ustring.Make ("Logging in...");
			var idle = ustring.Make ("Press 'Login' to log in.");
			var loginProgressLabel = new Label(idle) { Width = 40 };
			ViewModel
				.WhenAnyObservable (x => x.Login.IsExecuting)
				.Select (executing => executing ? progress : idle)
				.BindTo (loginProgressLabel, x => x.Text)
				.DisposeWith (_disposable);
			return loginProgressLabel;
		}

		Button LoginButton () {
			var loginButton = new Button ("Login") { Width = 40 };
			loginButton
				.Events ()
				.Clicked
				.InvokeCommand (ViewModel, x => x.Login)
				.DisposeWith (_disposable);
			return loginButton;
		}
		
		object IViewFor.ViewModel {
			get => ViewModel;
			set => ViewModel = (LoginViewModel) value;
		}
	}
}