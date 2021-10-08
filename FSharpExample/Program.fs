open System
open System.Diagnostics
open System.Globalization
open System.IO
open NStack
open Terminal.Gui

let ustr (x: string) = ustring.Make(x)
let mutable ml2 = Unchecked.defaultof<Label>
let mutable ml = Unchecked.defaultof<Label>
let mutable menu = Unchecked.defaultof<MenuBar>
let mutable menuKeysStyle = Unchecked.defaultof<CheckBox>
let mutable menuAutoMouseNav = Unchecked.defaultof<CheckBox>

type Box10x (x: int, y: int) =
    inherit View (Rect(x, y, 20, 10))
    let w = 40
    let h = 50

    new () =
        new Box10x ()

    member _.GetContentSize () =
        Size (w, h)

    member _.SetCursorPosition (_ : Point) =
        raise (NotImplementedException())

    override this.Redraw (_: Rect) =
        Application.Driver.SetAttribute this.ColorScheme.Focus
        do
        let mutable y = 0
        while y < h do
        this.Move (0, y)
        Application.Driver.AddStr (ustr (string y))
        do
        let mutable x = 0
        while x < w - (y.ToString ()).Length do
            if (string y).Length < w
            then Application.Driver.AddStr (ustr " ")
            x <- x + 1
        y <- y + 1

type Filler (rect: Rect) =
    inherit View(rect)
    new () =
        new Filler ()

    override this.Redraw (_: Rect) =
        Application.Driver.SetAttribute this.ColorScheme.Focus
        let mutable f = this.Frame
        do
        let mutable y = 0
        while y < f.Width do
        this.Move (0, y)
        do
        let mutable x = 0
        while x < f.Height do
            let r =
                match x % 3 with
                | 0 ->
                    Application.Driver.AddRune ((Rune ((string y).ToCharArray (0, 1)).[0]))
                    if y > 9 then
                        Application.Driver.AddRune ((Rune ((string y).ToCharArray (1, 1)).[0]))
                    Rune '.'
                | 1 -> Rune 'o'
                | _ -> Rune 'O'
            Application.Driver.AddRune r
            x <- x + 1
        y <- y + 1

let ShowTextAlignments () =
    let okButton = new Button (ustr "Ok", true)
    okButton.add_Clicked (Action (Application.RequestStop))
    let cancelButton = new Button (ustr "Cancel", true)
    cancelButton.add_Clicked (Action (Application.RequestStop))

    let container = new Dialog (ustr "Text Alignments", 50, 20, okButton, cancelButton)
    let txt = "Hello world, how are you doing today"
    container.Add (
        new Label (Rect(0, 1, 40, 3), ustr ((sprintf "%O-%O" 1) txt), TextAlignment = TextAlignment.Left),
        new Label (Rect(0, 3, 40, 3), ustr ((sprintf "%O-%O" 2) txt), TextAlignment = TextAlignment.Right),
        new Label (Rect(0, 5, 40, 3), ustr ((sprintf "%O-%O" 3) txt), TextAlignment = TextAlignment.Centered),
        new Label (Rect(0, 7, 40, 3), ustr ((sprintf "%O-%O" 4) txt), TextAlignment = TextAlignment.Justified))
    Application.Run container

let ShowEntries (container: View) =
    let scrollView = 
        new ScrollView (Rect (50, 10, 20, 8),
            ContentSize = Size (20, 50),
            ShowVerticalScrollIndicator = true,
            ShowHorizontalScrollIndicator = true)
    scrollView.Add (new Filler (Rect (0, 0, 40, 40)))
    let scrollView2 = 
        new ScrollView (Rect (72, 10, 3, 3),
            ContentSize = Size (100, 100),
            ShowVerticalScrollIndicator = true,
            ShowHorizontalScrollIndicator = true)
    scrollView2.Add (new Box10x (0, 0))
    let progress = new ProgressBar (Rect(68, 1, 10, 1))
    let timer = Func<MainLoop, bool> (fun _ ->
        progress.Pulse ()
        true)

    Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300.), timer) |> ignore

    let login =
        new Label (ustr "Login: ",
            X = Pos.At 3,
            Y = Pos.At 6)
    let password =
        new Label (ustr "Password: ",
            X = Pos.Left login,
            Y = Pos.Bottom login + Pos.At 1)
    let loginText =
        new TextField (ustr "",
            X = Pos.Right password,
            Y = Pos.Top login,
            Width = Dim.op_Implicit 40)
    let passText =
        new TextField (ustr "",
            Secret = true,
            X = Pos.Left loginText,
            Y = Pos.Top password,
            Width = Dim.Width loginText)
    let tf = new Button (3, 19, ustr "Ok")
    container.Add (login, loginText, password, passText,
        new FrameView (Rect (3, 10, 25, 6), ustr "Options",
            [| new CheckBox (1, 0, ustr "Remember me")
               new RadioGroup (1, 2, 
                [| ustr "_Personal"; ustr "_Company"|])|]),
        new ListView (Rect (59, 6, 16, 4),
                [| "First row"
                   "<>"
                   "This is a very long row that should overflow what is shown"
                   "4th"
                   "There is an empty slot on the second row"
                   "Whoa"
                   "This is so cool" |]),
        scrollView, scrollView2, tf,
        new Button (10, 19, ustr "Cancel"),
        new TimeField (3, 20, DateTime.Now.TimeOfDay),
        new TimeField (23, 20, DateTime.Now.TimeOfDay, true),
        new DateField (3, 22, DateTime.Now),
        new DateField (23, 22, DateTime.Now, true),
        progress,
        new Label (3, 24, ustr "Press F9 (on Unix, ESC+9 is an alias) to activate the menubar"),
        menuKeysStyle,
        menuAutoMouseNav)
    container.SendSubviewToBack tf

let NewFile () =
    let okButton = new Button (ustr "Ok", true)
    okButton.add_Clicked (Action (Application.RequestStop))
    let cancelButton = new Button (ustr "Cancel", true)
    cancelButton.add_Clicked (Action (Application.RequestStop))

    let d = new Dialog (ustr "New File", 50, 20, okButton, cancelButton)
    ml2 <- new Label (1, 1, ustr "Mouse Debug Line")
    d.Add ml2
    Application.Run d

let GetFileName () =
    let mutable fname = Unchecked.defaultof<_>
    for s in [| "/etc/passwd"; "c:\\windows\\win.ini" |] do
        if File.Exists s
        then fname <- s
    fname

let Editor (top: Toplevel) =
    let tframe = top.Frame
    let ntop = new Toplevel(tframe)
    let menu = 
        new MenuBar(
            [| MenuBarItem (ustr "_File",
                 [| MenuItem (ustr "_Close", ustring.Empty, (fun () -> Application.RequestStop ())) |]);
                    MenuBarItem (ustr "_Edit",
                        [| MenuItem (ustr "_Copy", ustring.Empty, Unchecked.defaultof<_>)
                           MenuItem (ustr "C_ut", ustring.Empty, Unchecked.defaultof<_>)
                           MenuItem (ustr "_Paste", ustring.Empty, Unchecked.defaultof<_>) |]) |])
    ntop.Add menu
    let fname = GetFileName ()
    let win = 
        new Window (
            ustr (if not (isNull fname) then fname else "Untitled"),
            X = Pos.At 0,
            Y = Pos.At 1,
            Width = Dim.Fill (),
            Height = Dim.Fill ())
    ntop.Add win
    let text = new TextView (Rect(0, 0, (tframe.Width - 2), (tframe.Height - 3)))
    if fname <> Unchecked.defaultof<_>
    then text.Text <- ustr (File.ReadAllText fname)
    win.Add text
    Application.Run ntop

let Quit () =
    MessageBox.Query (50, 7, ustr "Quit Demo", ustr "Are you sure you want to quit this demo?", ustr "Yes", ustr "No") = 0

let Close () =
    MessageBox.ErrorQuery (50, 7, ustr "Error", ustr "There is nothing to close", ustr "Ok")
    |> ignore

let Open () =
    let d = new OpenDialog (ustr "Open", ustr "Open a file", AllowsMultipleSelection = true)
    Application.Run d
    if not d.Canceled
        then MessageBox.Query (50, 7, ustr "Selected File", ustr (String.Join (", ", d.FilePaths)), ustr "Ok") |> ignore

let ShowHex (top: Toplevel) =
    let tframe = top.Frame
    let ntop = new Toplevel (tframe)
    let menu = 
        new MenuBar (
            [| MenuBarItem (ustr "_File",
                 [| MenuItem (ustr "_Close", ustring.Empty, (fun () -> Application.RequestStop ())) |]) |])
    ntop.Add menu
    let win =
        new Window (ustr "/etc/passwd",
            X = Pos.At 0,
            Y = Pos.At 1,
            Width = Dim.Fill (),
            Height = Dim.Fill ())
    ntop.Add win
    let fname = GetFileName ()
    let source = File.OpenRead fname
    let hex = 
        new HexView (source,
            X = Pos.At 0,
            Y = Pos.At 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ())
    win.Add hex
    Application.Run ntop

type MenuItemDetails () =
    inherit MenuItem ()
    new (title: ustring, help: ustring, action: Action) as this =
        MenuItemDetails ()
        then
            this.Title <- title
            this.Help <- help
            this.Action <- action

    static member Instance (mi: MenuItem) =
        (mi.GetMenuItem ()) :?> MenuItemDetails

type MenuItemDelegate = delegate of MenuItemDetails -> MenuItem

let ShowMenuItem (mi: MenuItemDetails) =
    MessageBox.Query (70, 7, ustr (mi.Title.ToString ()),
        ustr ((sprintf "%O selected. Is from submenu: %O" (mi.Title.ToString ())) (mi.GetMenuBarItem ())), ustr "Ok")
    |> ignore

let MenuKeysStyleToggled (_: bool) =
    menu.UseKeysUpDownAsKeysLeftRight <- menuKeysStyle.Checked

let MenuAutoMouseNavToggled (_: bool) =
    menu.WantMousePositionReports <- menuAutoMouseNav.Checked

let Copy () =
    let textField = menu.LastFocused :?> TextField
    if textField <> Unchecked.defaultof<_> && textField.SelectedLength <> 0
    then textField.Copy ()

let Cut () =
    let textField = menu.LastFocused :?> TextField
    if textField <> Unchecked.defaultof<_> && textField.SelectedLength <> 0
    then textField.Cut ()

let Paste () =
    let textField = menu.LastFocused :?> TextField
    if textField <> Unchecked.defaultof<_>
    then textField.Paste ()

let Help () =
    MessageBox.Query (50, 7, ustr "Help", ustr "This is a small help\nBe kind.", ustr "Ok")
    |> ignore

let Load () =
    MessageBox.Query (50, 7, ustr "Load", ustr "This is a small load\nBe kind.", ustr "Ok")
    |> ignore

let Save () =
    MessageBox.Query (50, 7, ustr "Save ", ustr "This is a small save\nBe kind.", ustr "Ok")
    |> ignore

let ListSelectionDemo (multiple: bool) =
    let okButton = new Button (ustr "Ok", true)
    okButton.add_Clicked (Action (Application.RequestStop))
    let cancelButton = new Button (ustr "Cancel")
    cancelButton.add_Clicked (Action (Application.RequestStop))

    let d = new Dialog (ustr "Selection Demo", 60, 20, okButton, cancelButton)
    let animals = ResizeArray<_> ()
    animals.AddRange([| "Alpaca"; "Llama"; "Lion"; "Shark"; "Goat" |])
    let msg =
        new Label (ustr "Use space bar or control-t to toggle selection",
            X = Pos.At 1,
            Y = Pos.At 1,
            Width = Dim.Fill () - Dim.op_Implicit 1,
            Height = Dim.op_Implicit 1)
    let list =
        new ListView (animals,
            X = Pos.At 1,
            Y = Pos.At 3,
            Width = Dim.Fill () - Dim.op_Implicit 4,
            Height = Dim.Fill () - Dim.op_Implicit 4,
            AllowsMarking = true,
            AllowsMultipleSelection = multiple)
    d.Add (msg, list)
    Application.Run d
    let mutable result = ""
    do
        let mutable i = 0
        while i < animals.Count do
        if list.Source.IsMarked i
        then result <- result + animals.[i] + " "
        i <- i + 1
    MessageBox.Query (60, 10, ustr "Selected Animals", ustr (if result = "" then "No animals selected" else result), ustr "Ok") |> ignore

let OnKeyDownPressUpDemo () =
    let closeButton = new Button (ustr "Close")
    closeButton.add_Clicked (Action (Application.RequestStop))

    let container = new Dialog (ustr "KeyDown & KeyPress & KeyUp demo", 80, 20, closeButton, Width = Dim.Fill (), Height = Dim.Fill ())
    
    let list = ResizeArray<_> ()
    let listView = 
        new ListView (list,
            X = Pos.At 0,
            Y = Pos.At 0,
            Width = Dim.Fill () - Dim.op_Implicit 1,
            Height = Dim.Fill () - Dim.op_Implicit 2,
            ColorScheme = Colors.TopLevel)
    container.Add (listView)
    
    let keyDownPressUp (keyEvent: KeyEvent, updown: string) =
        match updown with
        | "Down"
        | "Up"
        | "Press" -> 
            list.Add (keyEvent.ToString ())    
        | _ -> failwithf "Unknown: %s" updown
        listView.MoveDown ()

    container.add_KeyDown(Action<View.KeyEventEventArgs> (fun (e: View.KeyEventEventArgs) -> keyDownPressUp (e.KeyEvent, "Down") |> ignore))
    container.add_KeyPress(Action<View.KeyEventEventArgs> (fun (e: View.KeyEventEventArgs) -> keyDownPressUp (e.KeyEvent, "Press") |> ignore))
    container.add_KeyUp(Action<View.KeyEventEventArgs> (fun (e: View.KeyEventEventArgs) -> keyDownPressUp (e.KeyEvent, "Up") |> ignore))
    Application.Run (container)

let Main () =
    if Debugger.IsAttached then
        CultureInfo.DefaultThreadCurrentUICulture <- CultureInfo.GetCultureInfo ("en-US")
    Application.Init()
    let top = Application.Top
    let margin = 3
    let win = 
        new Window (ustr "Hello",
            X = Pos.At 1,
            Y = Pos.At 1,
            Width = Dim.Fill () - Dim.op_Implicit margin,
            Height = Dim.Fill () - Dim.op_Implicit margin)
    let menuItems =
        [|MenuItemDetails (ustr "F_ind",ustr "", Unchecked.defaultof<_>);
            MenuItemDetails (ustr "_Replace", ustr "", Unchecked.defaultof<_>);
            MenuItemDetails (ustr "_Item1", ustr "", Unchecked.defaultof<_>);
            MenuItemDetails (ustr "_Also From Sub Menu", ustr "", Unchecked.defaultof<_>)|]
    menuItems.[0].Action <- fun _ -> ShowMenuItem (menuItems.[0])
    menuItems.[1].Action <- fun _ -> ShowMenuItem (menuItems.[1])
    menuItems.[2].Action <- fun _ -> ShowMenuItem (menuItems.[2])
    menuItems.[3].Action <- fun _ -> ShowMenuItem (menuItems.[3])
    menu <-
        new MenuBar (
            [| MenuBarItem(ustr "_File",
                    [| MenuItem (ustr "Text _Editor Demo", ustring.Empty, (fun () -> Editor top))
                       MenuItem (ustr "_New", ustr "Creates new file", fun () -> NewFile())
                       MenuItem (ustr "_Open", ustring.Empty, fun () -> Open())
                       MenuItem (ustr "_Hex", ustring.Empty, (fun () -> ShowHex top))
                       MenuItem (ustr "_Close", ustring.Empty, (fun () -> Close()))
                       MenuItem (ustr "_Disabled", ustring.Empty, (fun () -> ()), (fun () -> false))
                       Unchecked.defaultof<_>
                       MenuItem (ustr "_Quit", ustring.Empty, (fun () -> if Quit() then top.Running <- false)) |])
               MenuBarItem (ustr "_Edit",
                    [| MenuItem (ustr "_Copy", ustring.Empty, fun () -> Copy())
                       MenuItem (ustr "C_ut", ustring.Empty, fun () -> Cut())
                       MenuItem (ustr "_Paste", ustring.Empty, fun () -> Paste())
                       MenuBarItem (ustr "_Find and Replace",
                           [| menuItems.[0] :> MenuItem
                              menuItems.[1] :> MenuItem |]) :> MenuItem
                       menuItems.[3] :> MenuItem
                    |])
               MenuBarItem (ustr "_List Demos", 
                    [| MenuItem (ustr "Select _Multiple Items", ustring.Empty, (fun () -> ListSelectionDemo true))
                       MenuItem (ustr "Select _Single Item", ustring.Empty, (fun () -> ListSelectionDemo false)) |])   
               MenuBarItem (ustr "A_ssorted",
                    [| MenuItem (ustr "_Show text alignments", ustring.Empty, (fun () -> ShowTextAlignments())) 
                       MenuItem (ustr "_OnKeyDown/Press/Up", ustring.Empty, (fun () -> OnKeyDownPressUpDemo())) |])
               MenuBarItem (ustr "_Test Menu and SubMenus",
                    [| MenuBarItem (ustr "SubMenu1Item_1",
                            [| MenuBarItem (ustr "SubMenu2Item_1",
                                    [| MenuBarItem (ustr "SubMenu3Item_1",
                                            [| menuItems.[2] :> MenuItem |]) :> MenuItem
                                    |]) :> MenuItem
                            |]) :> MenuItem
                    |])
               MenuBarItem (ustr "_About...", ustr "Demonstrates top-level menu item", (fun () -> MessageBox.ErrorQuery (50, 7, ustr "Error", ustr "This is a demo app for gui.cs", ustr "Ok") |> ignore)) |])
    menuKeysStyle <- new CheckBox (3, 25, ustr "UseKeysUpDownAsKeysLeftRight", true)
    menuKeysStyle.add_Toggled (Action<bool> (MenuKeysStyleToggled))
    menuAutoMouseNav <- new CheckBox (40, 25, ustr "UseMenuAutoNavigation", true)
    menuAutoMouseNav.add_Toggled (Action<bool> (MenuAutoMouseNavToggled))
    ShowEntries win
    let mutable count = 0
    ml <- new Label (Rect (3, 17, 47, 1), ustr "Mouse: ")
    Application.RootMouseEvent <- Action<MouseEvent> (
            fun (me: MouseEvent) ->
                ml.Text <- ustr (
                     (((sprintf "Mouse: (%O,%O) - %O %O" me.X) me.Y) me.Flags) (count <- count + 1; count)))
    let test = new Label (3, 18, ustr "Se iniciará el análisis")
    win.Add test
    win.Add ml
    let drag = new Label (ustr "Drag: ", X = Pos.At 70, Y = Pos.At 24)
    let dragText = 
        new TextField (ustr "",
            X = Pos.Right drag,
            Y = Pos.Top drag,
            Width = Dim.op_Implicit 40)
    let statusBar = new StatusBar ([|
        StatusItem(Key.F1, ustr "~F1~ Help", Action Help)
        StatusItem(Key.F2, ustr "~F2~ Load", Action Load)
        StatusItem(Key.F3, ustr "~F3~ Save", Action Save)
        StatusItem(Key.Q, ustr "~^Q~ Quit", fun () -> if (Quit()) then top.Running <- false) |])
    win.Add (drag, dragText)
    let bottom = new Label (ustr "This should go on the bottom of the same top-level!")
    win.Add bottom
    let bottom2 = new Label (ustr "This should go on the bottom of another top-level!")
    top.Add bottom2
    Application.Resized <- Action<Application.ResizedEventArgs> (
        fun _ ->
            bottom.X <- win.X
            bottom.Y <- Pos.Bottom win - Pos.Top win - Pos.At margin
            bottom2.X <- Pos.Left win
            bottom2.Y <- Pos.Bottom win)
    top.Add win
    top.Add (menu, statusBar)
    Application.Run ()

module Demo =
    [<EntryPoint>]
    let main _ =
        Main ()
        0