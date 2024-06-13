using CommunityToolkit.Mvvm.Messaging;
using Terminal.Gui;

namespace CommunityToolkitExample;

internal partial class LoginView : IRecipient<Message<LoginAction>>
{
    public LoginView (LoginViewModel viewModel)
    {
        WeakReferenceMessenger.Default.Register (this);
        Title = $"Community Toolkit MVVM Example - {Application.QuitKey} to Exit";
        ViewModel = viewModel;
        InitializeComponent ();
        usernameInput.TextChanged += (_, _) =>
                                     {
                                         ViewModel.Username = usernameInput.Text;
                                         SetText ();
                                     };
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

        clearButton.Accept += (_, _) =>
                              {
                                  ViewModel.ClearCommand.Execute (null);
                                  SetText ();
                              };

        Initialized += (_, _) => { ViewModel.Initialized (); };
    }

    public LoginViewModel ViewModel { get; set; }

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
                    validationLabel.ColorScheme = ViewModel.ValidationColorScheme;
                    break;
                }
        }
        SetText();
        Application.Refresh ();
    }

    private void SetText ()
    {
        usernameInput.Text = ViewModel.Username;
        usernameLengthLabel.Text = ViewModel.UsernameLengthMessage;
        passwordInput.Text = ViewModel.Password;
        passwordLengthLabel.Text = ViewModel.PasswordLengthMessage;
    }
}