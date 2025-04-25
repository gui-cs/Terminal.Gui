open Terminal.Gui

type ExampleWindow() as this =
    inherit Window()
    
    do
        this.Title <- sprintf "Example App (%O to quit)" Application.QuitKey

        // Create input components and labels
        let usernameLabel = new Label(Text = "Username:")

        let userNameText = new TextField(X = Pos.Right(usernameLabel) + Pos.op_Implicit(1), Width = Dim.Fill())

        let passwordLabel = new Label(Text = "Password:", X = Pos.Left(usernameLabel), Y = Pos.Bottom(usernameLabel) +  Pos.op_Implicit(1))

        let passwordText = new TextField(Secret = true, X = Pos.Left(userNameText), Y = Pos.Top(passwordLabel), Width = Dim.Fill())

        // Create login button
        let btnLogin = new Button(Text = "Login", Y = Pos.Bottom(passwordLabel) +  Pos.op_Implicit(1), X = Pos.Center(), IsDefault = true)

        // When login button is clicked display a message popup
        btnLogin.Accepting.Add(fun _ ->
            if userNameText.Text = "admin" && passwordText.Text = "password" then
                MessageBox.Query("Logging In", "Login Successful", "Ok") |> ignore
                ExampleWindow.UserName <- userNameText.Text.ToString()
                Application.RequestStop()
            else
                MessageBox.ErrorQuery("Logging In", "Incorrect username or password", "Ok") |> ignore
        )

        // Add the views to the Window
        this.Add(usernameLabel, userNameText, passwordLabel, passwordText, btnLogin)

    static member val UserName = "" with get, set

[<EntryPoint>]
let main argv =
    Application.Init()
    Application.Run<ExampleWindow>().Dispose()
    
    // Before the application exits, reset Terminal.Gui for clean shutdown
    Application.Shutdown()
    
    // To see this output on the screen it must be done after shutdown,
    // which restores the previous screen.
    printfn "Username: %s" ExampleWindow.UserName
    
    0 // return an integer exit code
