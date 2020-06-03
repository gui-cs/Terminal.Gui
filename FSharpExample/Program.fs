// Learn more about F# at http://fsharp.org

open Terminal.Gui
open System
open System.Collections.Generic
open System.Diagnostics
open System.Globalization
open System.Reflection
open NStack

type Demo() = class end
    let ustr (x:string) = ustring.Make(x)
    let mutable ml2 = Unchecked.defaultof<Label>
    let mutable ml = Unchecked.defaultof<Label>
    let mutable menu = Unchecked.defaultof<MenuBar>
    let mutable menuKeysStyle = Unchecked.defaultof<CheckBox>
    let mutable menuAutoMouseNav = Unchecked.defaultof<CheckBox>

    type Box10x() =
        inherit View()
        member val w = 40 with get, set
        member val h = 50 with get, set
        member val WantCursorPosition = Unchecked.defaultof<System.Boolean> with get, set
        new(x : int, y : int) as this =
            (Box10x())
            then
            ()
        member this.GetContentSize() =
            new Size(this.w, this.h)
        member this.SetCursorPosition(pos : Point) =
            raise (new NotImplementedException())
        override this.Redraw(region : Rect) =
            Application.Driver.SetAttribute (Application.Current.ColorScheme.Focus)
            do
            let mutable (y : int) = 0
            while (y < this.h) do
            this.Move (0, y)
            Application.Driver.AddStr (ustr (y.ToString()))
            do
            let mutable (x : int) = 0
            while (x < this.w - (y.ToString ()).Length) do
                if (y.ToString ()).Length < this.w
                then Application.Driver.AddStr (ustr " ")
                x <- x + 1
            x
            y <- y + 1
            y
            ()

    type Filler() =
        inherit View()
        new(rect : Rect) as this =
            (Filler ())
            then
            ()
        override this.Redraw(region : Rect) =
            Application.Driver.SetAttribute (Application.Current.ColorScheme.Focus)
            let mutable f = this.Frame
            do
            let mutable (y : int) = 0
            while (y < f.Width) do
            this.Move (0, y)
            do
            let mutable (x : int) = 0
            while (x < f.Height) do
                let mutable (r : Rune) = Unchecked.defaultof<Rune>
                match (x % 3) with
                | 0 ->
                    Application.Driver.AddRune ((Rune ((y.ToString ()).ToCharArray (0, 1)).[0]))
                    if y > 9
                    then Application.Driver.AddRune ((Rune ((y.ToString ()).ToCharArray (1, 1)).[0]))
                    r <- (Rune '.')
                | 1 ->
                    r <- (Rune 'o')
                | _ ->
                    r <- (Rune 'O')
                Application.Driver.AddRune (r)
                x <- x + 1
            x
            y <- y + 1
            y
            ()

    let ShowTextAlignments() =
        let mutable container = new Dialog(
            ustr "Text Alignments", 50, 20,
            new Button (ustr "Ok", true, Clicked = Action(Application.RequestStop)),
            new Button (ustr "Cancel", true, Clicked = Action(Application.RequestStop))
            )
        let mutable (i : int) = 0
        let mutable (txt : string) = "Hello world, how are you doing today"
        container.Add (
            new Label (new Rect (0, 1, 40, 3), ustr ((sprintf "%O-%O" (i + 1)) txt), TextAlignment = TextAlignment.Left),
            new Label (new Rect (0, 3, 40, 3), ustr ((sprintf "%O-%O" (i + 2)) txt), TextAlignment = TextAlignment.Right),
            new Label (new Rect (0, 5, 40, 3), ustr ((sprintf "%O-%O" (i + 3)) txt), TextAlignment = TextAlignment.Centered),
            new Label (new Rect (0, 7, 40, 3), ustr ((sprintf "%O-%O" (i + 4)) txt), TextAlignment = TextAlignment.Justified)
            )
        Application.Run (container)

    let ShowEntries(container : View) =
        let mutable scrollView = new ScrollView (new Rect (50, 10, 20, 8),
            ContentSize = new Size (20, 50),
            ShowVerticalScrollIndicator = true,
            ShowHorizontalScrollIndicator = true
            )
        scrollView.Add (new Filler(new Rect(0, 0, 40, 40)))
        let mutable scrollView2 = new ScrollView (new Rect (72, 10, 3, 3),
            ContentSize = new Size (100, 100),
            ShowVerticalScrollIndicator = true,
            ShowHorizontalScrollIndicator = true
            )
        scrollView2.Add (new Box10x(0, 0))
        let mutable progress = new ProgressBar(new Rect(68, 1, 10, 1))
        let timer = Func<MainLoop, bool> (fun (caller) ->
            progress.Pulse ();
            true)

        Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300.0), timer) |> ignore

        let mutable login = Label (ustr "Login: ",
            X = Pos.At(3),
            Y = Pos.At(6)
            )
        let mutable password = new Label (ustr "Password: ",
            X = Pos.Left (login),
            Y = Pos.Bottom (login) + Pos.At(1)
            )
        let mutable loginText = new TextField (ustr "",
            X = Pos.Right (password),
            Y = Pos.Top (login),
            Width = Dim.op_Implicit(40)
            )
        let mutable passText = new TextField (ustr "",
            Secret = true,
            X = Pos.Left (loginText),
            Y = Pos.Top (password),
            Width = Dim.Width (loginText)
            )
        let mutable tf = new Button(3, 19, ustr "Ok")
        container.Add (login, loginText, password, passText,
            new FrameView (new Rect (3, 10, 25, 6), ustr "Options",
                [|new CheckBox (1, 0, ustr "Remember me");
                new RadioGroup (1, 2, [|"_Personal"; "_Company"|])|]
                ),
            new ListView (new Rect(59, 6, 16, 4),
                    [|"First row";
                    "<>";
                    "This is a very long row that should overflow what is shown";
                    "4th";
                    "There is an empty slot on the second row";
                    "Whoa";
                    "This is so cool"|]
                ),
            scrollView, scrollView2, tf,
            new Button(10, 19, ustr "Cancel"),
            new TimeField(3, 20, DateTime.Now),
            new TimeField(23, 20, DateTime.Now, true),
            new DateField(3, 22, DateTime.Now),
            new DateField(23, 22, DateTime.Now, true),
            progress,
            new Label(3, 24, ustr "Press F9 (on Unix, ESC+9 is an alias) to activate the menubar"),
            menuKeysStyle,
            menuAutoMouseNav
        )
        container.SendSubviewToBack (tf)
        ()
    let NewFile() =
        let mutable d = new Dialog (ustr "New File", 50, 20,
                            new Button (ustr "Ok", true, Clicked = Action(Application.RequestStop)),
                            new Button (ustr "Cancel", true, Clicked = Action(Application.RequestStop))
        )
        ml2 <- new Label(1, 1, ustr "Mouse Debug Line")
        d.Add (ml2)
        Application.Run (d)

    let Editor(top : Toplevel) =
        let mutable tframe = top.Frame
        let mutable ntop = new Toplevel(tframe)
        let mutable menu = new MenuBar([|new MenuBarItem(ustr "_File",
            [|new MenuItem(ustr "_Close", "", (fun () -> Application.RequestStop ()))|]);
            new MenuBarItem(ustr "_Edit", [|new MenuItem(ustr "_Copy", "", Unchecked.defaultof<_>);
            new MenuItem(ustr "C_ut", "", Unchecked.defaultof<_>);
            new MenuItem(ustr "_Paste", "", Unchecked.defaultof<_>)|])|]
            )
        ntop.Add (menu)
        let mutable (fname : string) = Unchecked.defaultof<_>
        for s in [|"/etc/passwd"; "c:\\windows\\win.ini"|] do
            if System.IO.File.Exists (s)
            then
                fname <- s
        let mutable win = new Window (ustr(if fname <> null then fname else "Untitled"),
            X = Pos.At(0),
            Y = Pos.At(1),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        )
        ntop.Add (win)
        let mutable text = new TextView(new Rect(0, 0, (tframe.Width - 2), (tframe.Height - 3)))
        if fname <> Unchecked.defaultof<_>
        then text.Text <- ustr (System.IO.File.ReadAllText (fname))
        win.Add (text)
        Application.Run (ntop)

    let Quit() =
        let mutable n = MessageBox.Query (50, 7, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No")
        n = 0

    let Close() =
        MessageBox.ErrorQuery (50, 7, "Error", "There is nothing to close", "Ok")
        |> ignore

    let Open() =
        let mutable d = new OpenDialog (ustr "Open", ustr "Open a file", AllowsMultipleSelection = true)
        Application.Run (d)
        if not d.Canceled
            then MessageBox.Query (50, 7, "Selected File", (String.Join (", ", d.FilePaths)), "Ok") |> ignore

    let ShowHex(top : Toplevel) =
        let mutable tframe = top.Frame
        let mutable ntop = new Toplevel(tframe)
        let mutable menu = new MenuBar([|new MenuBarItem(ustr "_File",
            [|new MenuItem(ustr "_Close", "", (fun () -> Application.RequestStop ()))|])|])
        ntop.Add (menu)
        let mutable win = new Window (ustr "/etc/passwd",
            X = Pos.At(0),
            Y = Pos.At(1),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
            )
        ntop.Add (win)
        let mutable source = System.IO.File.OpenRead ("/etc/passwd")
        let mutable hex = new HexView (source,
            X = Pos.At(0),
            Y = Pos.At(0),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
            )
        win.Add (hex)
        Application.Run (ntop)

    type MenuItemDetails() =
        inherit MenuItem()
        new(title : ustring, help : string, action : Action) as this =
            (MenuItemDetails ())
            then
                this.Title <- title
                this.Help <- ustr help
                this.Action <- action
        static member Instance(mi : MenuItem) =
            (mi.GetMenuItem ()) :?> MenuItemDetails

    type MenuItemDelegate = delegate of MenuItemDetails -> MenuItem

    let ShowMenuItem(mi : MenuItemDetails) =
        let mutable (flags : BindingFlags) = BindingFlags.Public ||| BindingFlags.Static
        let mutable (minfo : MethodInfo) = typeof<MenuItemDetails>.GetMethod ("Instance", flags)
        let mutable (mid : Delegate) = Delegate.CreateDelegate (typeof<MenuItemDelegate>, minfo)
        MessageBox.Query (70, 7, (mi.Title.ToString ()),
            ((sprintf "%O selected. Is from submenu: %O" (mi.Title.ToString ())) (mi.GetMenuBarItem ())), "Ok")
        |> ignore

    let MenuKeysStyle_Toggled(e : EventArgs) =
        menu.UseKeysUpDownAsKeysLeftRight <- menuKeysStyle.Checked

    let MenuAutoMouseNav_Toggled(e : EventArgs) =
        menu.WantMousePositionReports <- menuAutoMouseNav.Checked

    let Copy() =
        let mutable (textField : TextField) = menu.LastFocused :?> TextField
        if textField <> Unchecked.defaultof<_> && textField.SelectedLength <> 0
        then textField.Copy ()
        ()

    let Cut() =
        let mutable (textField : TextField) = menu.LastFocused :?> TextField
        if textField <> Unchecked.defaultof<_> && textField.SelectedLength <> 0
        then textField.Cut ()
        ()

    let Paste() =
        let mutable (textField : TextField) = menu.LastFocused :?> TextField
        if textField <> Unchecked.defaultof<_>
        then textField.Paste ()
        ()

    let Help() =
        MessageBox.Query (50, 7, "Help", "This is a small help\nBe kind.", "Ok")
        |> ignore

    let ListSelectionDemo(multiple : System.Boolean) =
        let mutable d = new Dialog (ustr "Selection Demo", 60, 20,
            new Button (ustr "Ok", true, Clicked = fun () -> Application.RequestStop ()),
            new Button (ustr "Cancel", Clicked = fun () -> Application.RequestStop ())
            )
        let mutable animals = new List<string> ()
        animals.AddRange([|"Alpaca"; "Llama"; "Lion"; "Shark"; "Goat"|])
        let mutable msg = new Label (ustr "Use space bar or control-t to toggle selection",
            X = Pos.At(1),
            Y = Pos.At(1),
            Width = Dim.Fill () - Dim.op_Implicit(1),
            Height = Dim.op_Implicit(1)
            )
        let mutable list = new ListView (animals,
            X = Pos.At(1),
            Y = Pos.At(3),
            Width = Dim.Fill () - Dim.op_Implicit(4),
            Height = Dim.Fill () - Dim.op_Implicit(4),
            AllowsMarking = true,
            AllowsMultipleSelection = multiple
            )
        d.Add (msg, list)
        Application.Run (d)
        let mutable result = ""
        do
            let mutable (i : int) = 0
            while (i < animals.Count) do
            if list.Source.IsMarked (i)
            then result <- result + animals.[i] + " "
            i <- i + 1
            i
            ()
        MessageBox.Query (60, 10, "Selected Animals", (if result = "" then "No animals selected" else result), "Ok") |> ignore

    let OnKeyDownPressUpDemo() =
        let mutable container = new Dialog (ustr "KeyDown & KeyPress & KeyUp demo", 80, 20,            
            new Button (ustr "Close", Clicked = fun () -> Application.RequestStop ()),
                Width = Dim.Fill (),
                Height = Dim.Fill ()
            )
        
        let mutable list = new List<string> ()
        let mutable listView = new ListView (list,
            X = Pos.At(0),
            Y = Pos.At(0),
            Width = Dim.Fill () - Dim.op_Implicit(1),
            Height = Dim.Fill () - Dim.op_Implicit(2),
            ColorScheme = Colors.TopLevel
            )
        container.Add (listView)
        
        let KeyDownPressUp(keyEvent : KeyEvent, updown : string) =
            let ident : int = -5
            match updown with
            | "Down"
            | "Up"
            | "Press" -> 
                list.Add (keyEvent.ToString ())    
            listView.MoveDown ();
    
        container.KeyDown.Add(fun (e : View.KeyEventEventArgs) -> KeyDownPressUp (e.KeyEvent, "Down") |> ignore)
        container.KeyPress.Add(fun (e : View.KeyEventEventArgs) -> KeyDownPressUp (e.KeyEvent, "Press") |> ignore)
        container.KeyUp.Add(fun (e : View.KeyEventEventArgs) -> KeyDownPressUp (e.KeyEvent, "Up") |> ignore)
        Application.Run (container)

    let Main() =
        if Debugger.IsAttached
        then CultureInfo.DefaultThreadCurrentUICulture <- CultureInfo.GetCultureInfo ("en-US")
        Application.Init ()
        let mutable top = Application.Top
        let mutable (margin : int) = 3
        let mutable win = new Window (ustr "Hello",
            X = Pos.At(1),
            Y = Pos.At(1),

            Width = Dim.Fill () - Dim.op_Implicit(margin),
            Height = Dim.Fill () - Dim.op_Implicit(margin)
            )
        let mutable (menuItems : MenuItemDetails[]) = [|new MenuItemDetails(ustr "F_ind", "", Unchecked.defaultof<_>);
            new MenuItemDetails(ustr "_Replace", "", Unchecked.defaultof<_>);
            new MenuItemDetails(ustr "_Item1", "", Unchecked.defaultof<_>);
            new MenuItemDetails(ustr "_Not From Sub Menu", "", Unchecked.defaultof<_>)|]
        menuItems.[0].Action <- fun () -> ShowMenuItem (menuItems.[0])
        menuItems.[1].Action <- fun () -> ShowMenuItem (menuItems.[1])
        menuItems.[2].Action <- fun () -> ShowMenuItem (menuItems.[2])
        menuItems.[3].Action <- fun () -> ShowMenuItem (menuItems.[3])
        menu <-
            new MenuBar ([|new MenuBarItem(ustr "_File",
                [|new MenuItem (ustr "Text _Editor Demo", "", (fun () -> Editor (top)));
                    new MenuItem (ustr "_New", "Creates new file", fun () -> NewFile());
                    new MenuItem (ustr "_Open", "", fun () -> Open());
                    new MenuItem (ustr "_Hex", "", (fun () -> ShowHex (top)));
                    new MenuItem (ustr "_Close", "", (fun () -> Close()));
                    new MenuItem (ustr "_Disabled", "", (fun () -> ()), (fun () -> false));
                    Unchecked.defaultof<_>;
                    new MenuItem (ustr "_Quit", "", (fun () -> if Quit() then top.Running <- false))|]);
                new MenuBarItem (ustr "_Edit", [|new MenuItem(ustr "_Copy", "", fun () -> Copy());
                    new MenuItem(ustr "C_ut", "", fun () -> Cut()); new MenuItem(ustr "_Paste", "", fun () -> Paste());
                    new MenuItem(ustr "_Find and Replace", new MenuBarItem([|(menuItems.[0]);
                    (menuItems.[1])|])); (menuItems.[3])|]);
                new MenuBarItem(ustr "_List Demos", [|new MenuItem(ustr "Select _Multiple Items", "", (fun () -> ListSelectionDemo (true)));
                    new MenuItem(ustr "Select _Single Item", "", (fun () -> ListSelectionDemo (false)))|]);
                    new MenuBarItem(ustr "A_ssorted", [|new MenuItem(ustr "_Show text alignments", "", (fun () -> ShowTextAlignments ()));
                new MenuItem(ustr "_OnKeyDown/Press/Up", "", (fun () -> OnKeyDownPressUpDemo ()))|]);
                new MenuBarItem(ustr "_Test Menu and SubMenus",
                    [|new MenuItem(ustr "SubMenu1Item_1", new MenuBarItem([|new MenuItem(ustr "SubMenu2Item_1",
                    new MenuBarItem([|new MenuItem(ustr "SubMenu3Item_1", new MenuBarItem([|(menuItems.[2])|]))|]))|]))|]);
                new MenuBarItem(ustr "_About...", "Demonstrates top-level menu item",
                    (fun () -> MessageBox.ErrorQuery (50, 7, "About Demo", "This is a demo app for gui.cs", "Ok") |> ignore))|])
        menuKeysStyle <- new CheckBox(3, 25, ustr "UseKeysUpDownAsKeysLeftRight", true)
        menuKeysStyle.Toggled.Add(MenuKeysStyle_Toggled)
        menuAutoMouseNav <- new CheckBox(40, 25, ustr "UseMenuAutoNavigation", true)
        menuAutoMouseNav.Toggled.Add(MenuAutoMouseNav_Toggled)
        ShowEntries (win)
        let mutable (count : int) = 0
        ml <- new Label(new Rect(3, 17, 47, 1), ustr "Mouse: ")
        Application.RootMouseEvent <- Action<MouseEvent> (
                fun (me : MouseEvent) ->
                    ml.TextColor <- Colors.TopLevel.Normal
                    ml.Text <- ustr (
                         (((sprintf "Mouse: (%O,%O) - %O %O" me.X) me.Y) me.Flags) (
                            count <- count + 1
                            count))
                            )
        let mutable test = new Label(3, 18, ustr "Se iniciará el análisis")
        win.Add (test)
        win.Add (ml)
        let mutable drag = new Label (ustr "Drag: ", X = Pos.At(70), Y = Pos.At(24))
        let mutable dragText = new TextField (ustr "",
            X = Pos.Right (drag),
            Y = Pos.Top (drag),
            Width = Dim.op_Implicit(40)
            )
        let mutable statusBar = new StatusBar ([|
            new StatusItem(Key.F1, ustr "~F1~ Help", Action(Help));
            new StatusItem(Key.F2, ustr "~F2~ Load", null);
            new StatusItem(Key.F3, ustr "~F3~ Save", null);
            new StatusItem(Key.ControlX, ustr "~^X~ Quit", fun () -> if (Quit ()) then top.Running <- false)
            |],
            Parent = null
            )
        win.Add (drag, dragText)
        let mutable bottom = new Label(ustr "This should go on the bottom of the same top-level!")
        win.Add (bottom)
        let mutable bottom2 = new Label(ustr "This should go on the bottom of another top-level!")
        top.Add (bottom2)
        Application.Loaded.Add (
            fun (_) ->
                bottom.X <- win.X
                bottom.Y <- Pos.Bottom (win) - Pos.Top (win) - Pos.At(margin)
                bottom2.X <- Pos.Left (win)
                bottom2.Y <- Pos.Bottom (win)
                )
        top.Add (win)
        top.Add (menu, statusBar)
        Application.Run ()

module Demo__run =
    [<EntryPoint>]
    let main argv =
        Main ()
        0