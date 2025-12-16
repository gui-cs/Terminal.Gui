
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CommunityToolkitExample;

internal partial class LoginView : Window
{
    private Label titleLabel;
    private Label usernameLengthLabel;
    private TextField usernameInput;
    private Label passwordLengthLabel;
    private TextField passwordInput;
    private Label validationLabel;
    private Button loginButton;
    private Button clearButton;
    private Label loginProgressLabel;

    private void InitializeComponent ()
    {
        titleLabel = new Label ();
        titleLabel.Text = "Login Form";
        Add (titleLabel);
        usernameLengthLabel = new Label ();
        usernameLengthLabel.X = Pos.Left (titleLabel);
        usernameLengthLabel.Y = Pos.Top (titleLabel) + 1;
        Add (usernameLengthLabel);
        usernameInput = new TextField ();
        usernameInput.X = Pos.Right (usernameLengthLabel) + 1;
        usernameInput.Y = Pos.Top (usernameLengthLabel);
        usernameInput.Width = 40;
        Add (usernameInput);
        passwordLengthLabel = new Label ();
        passwordLengthLabel.X = Pos.Left (usernameLengthLabel);
        passwordLengthLabel.Y = Pos.Top (usernameLengthLabel) + 1;
        Add (passwordLengthLabel);
        passwordInput = new TextField ();
        passwordInput.X = Pos.Right (passwordLengthLabel) + 1;
        passwordInput.Y = Pos.Top (passwordLengthLabel);
        passwordInput.Width = 40;
        passwordInput.Secret = true;
        Add (passwordInput);
        validationLabel = new Label ();
        validationLabel.X = Pos.Left (passwordInput);
        validationLabel.Y = Pos.Top (passwordInput) + 1;
        Add (validationLabel);
        loginButton = new Button ();
        loginButton.X = Pos.Left (validationLabel);
        loginButton.Y = Pos.Top (validationLabel) + 1;
        loginButton.Text = "_Login";
        Add (loginButton);
        clearButton = new Button ();
        clearButton.X = Pos.Left (loginButton);
        clearButton.Y = Pos.Top (loginButton) + 1;
        clearButton.Text = "_Clear";
        Add (clearButton);
        loginProgressLabel = new Label ();
        loginProgressLabel.X = Pos.Left (clearButton);
        loginProgressLabel.Y = Pos.Top (clearButton) + 1;
        loginProgressLabel.Width = 40;
        loginProgressLabel.Height = 1;
        Add (loginProgressLabel);
    }
}
