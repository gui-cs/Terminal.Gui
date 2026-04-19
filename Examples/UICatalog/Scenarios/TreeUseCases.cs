// ReSharper disable StringLiteralTypo

#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tree View", "Simple tree view examples.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class TreeUseCases : Scenario
{
    private EventLog? _eventLog;
    private Runnable? _appWindow;
    private ViewportSettingsEditor? _viewportSettingsEditor;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        _appWindow = new Runnable ();

        // MenuBar
        MenuBar menu = new ();

        menu.Add (new MenuBarItem (Strings.menuFile, [new MenuItem { Title = Strings.cmdQuit, Action = Quit }]));

        menu.Add (new MenuBarItem ("_Scenarios",
                                   [
                                       new MenuItem { Title = "_EnableForDesign", Action = LoadEnableForDesign },
                                       new MenuItem { Title = "_Rooms", Action = LoadRooms },
                                       new MenuItem { Title = "Armies With _Builder", Action = () => LoadArmies (false) },
                                       new MenuItem { Title = "Armies With _Delegate", Action = () => LoadArmies (true) }
                                   ]));

        // StatusBar
        StatusBar statusBar = new ([new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit)]);

        // EventLog on the right
        _eventLog = new EventLog
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Width = Dim.Percent (25),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Double
        };

        // ViewportSettingsEditor at the bottom-left (below tree area)
        _viewportSettingsEditor = new ViewportSettingsEditor
        {
            Title = "ViewportSettings",
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (_eventLog),
            Height = Dim.Auto (),
            CanFocus = true,
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Single
        };

        _appWindow?.Add (menu, statusBar, _eventLog, _viewportSettingsEditor);

        _appWindow?.IsModalChanged += (_, args) =>
                                      {
                                          if (args.Value)
                                          {
                                              // Start with the most basic use case
                                              LoadEnableForDesign ();
                                          }
                                      };

        app.Run (_appWindow!);
        _appWindow?.Dispose ();
    }

    private View? CurrentTree
    {
        set
        {
            if (field == value)
            {
                return;
            }

            if (field is { })
            {
                field?.Dispose ();
                _appWindow?.Remove (field);
            }

            field = value;

            if (field is null)
            {
                return;
            }

            field.X = 0;
            field.Y = 1;
            field.Width = Dim.Fill (_eventLog!);
            field.Height = Dim.Fill (_viewportSettingsEditor!);
            field.BorderStyle = LineStyle.Single;
            field.Arrangement = ViewArrangement.Resizable | ViewArrangement.Movable;
            field.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

            _appWindow?.Add (field);
            _appWindow?.MoveSubViewTowardsStart (field);

            _eventLog!.ViewToLog = field;
            _viewportSettingsEditor!.ViewToEdit = field;

            field?.SetFocus ();
        }
    }

    private void LoadArmies (bool useDelegate)
    {
        Army army = CreateMiddleEarthArmy ();

        TreeView<GameObject> tree = new ();

        if (useDelegate)
        {
            tree.TreeBuilder = new DelegateTreeBuilder<GameObject> (o => o.GetChildren (), o => o.GetChildren ().Any ());
            tree.Title = "Armies With _Builder";
        }
        else
        {
            tree.TreeBuilder = new GameObjectTreeBuilder ();
            tree.Title = "Armies With_ Delegate";
        }

        tree.AddObject (army);

        CurrentTree = tree;
    }

    private void LoadRooms ()
    {
        House myHouse = new ()
        {
            Address = "23 Nowhere Street", Rooms = [new Room { Name = "Ballroom" }, new Room { Name = "Bedroom 1" }, new Room { Name = "Bedroom 2" }]
        };

        TreeView tree = new ();
        tree.Title = "_Rooms";

        tree.AddObject (myHouse);

        CurrentTree = tree;
    }

    private void LoadEnableForDesign ()
    {
        TreeView tree = new ();
        tree.EnableForDesign ();
        tree.Title = "_EnableForDesign";

        CurrentTree = tree;
    }

    private void Quit () => _appWindow?.RequestStop ();

    private abstract class GameObject
    {
        public virtual IEnumerable<GameObject> GetChildren () => Enumerable.Empty<GameObject> ();
    }

    private class Army : GameObject
    {
        public string Designation { get; init; } = string.Empty;
        public List<CorpsObject> Corps { get; init; } = [];
        public override string ToString () => Designation;
        public override IEnumerable<GameObject> GetChildren () => Corps;
    }

    private class CorpsObject : GameObject
    {
        public string Designation { get; init; } = string.Empty;
        public List<Division> Divisions { get; init; } = [];
        public override string ToString () => Designation;
        public override IEnumerable<GameObject> GetChildren () => Divisions;
    }

    private class Division : GameObject
    {
        public string Designation { get; init; } = string.Empty;
        public List<Brigade> Brigades { get; init; } = [];
        public override string ToString () => Designation;
        public override IEnumerable<GameObject> GetChildren () => Brigades;
    }

    private class Brigade : GameObject
    {
        public string Designation { get; init; } = string.Empty;
        public List<Unit> Units { get; init; } = [];
        public override string ToString () => Designation;
        public override IEnumerable<GameObject> GetChildren () => Units;
    }

    private class Unit : GameObject
    {
        public string Name { get; init; } = string.Empty;
        public override string ToString () => Name;
    }

    private class GameObjectTreeBuilder : ITreeBuilder<GameObject>
    {
        public bool SupportsCanExpand => true;
        public bool CanExpand (GameObject model) => model.GetChildren ().Any ();

        public IEnumerable<GameObject> GetChildren (GameObject model) => model.GetChildren ();
    }

    // ── House / Room model (unchanged) ─────────────────────────────────────

    private class House : TreeNode
    {
        public string Address { get; set; } = string.Empty;

        public override IList<ITreeNode> Children => Rooms.Cast<ITreeNode> ().ToList ();
        public List<Room> Rooms { get; init; } = [];

        public override string Text { get => Address; set => Address = value; }
    }

    private class Room : TreeNode
    {
        public string Name { get; set; } = string.Empty;

        public override string Text { get => Name; set => Name = value; }
    }

    private static Army CreateMiddleEarthArmy ()
    {
        // ~500 total nodes: 1 Army → 5 Corps → ~5 Divisions → ~4 Brigades → ~5 Units each
        Army army = new ()
        {
            Designation = "The Grand Army of Middle-earth (Aragorn's Really Big Problem™)",
            Corps =
            [
                CreateCorps ("I Corps — The Shire Expeditionary Force (They Didn't Volunteer)",
                             [
                                 CreateDivision ("1st Hobbit Light Infantry Division",
                                                 [
                                                     CreateBrigade ("Buckland Border Patrol Brigade",
                                                                    [
                                                                        "Brandybuck Pitchfork Platoon",
                                                                        "Farmer Maggot's Crop Defenders",
                                                                        "Hobbiton Night Watch (Mostly Napping)",
                                                                        "Old Toby Smoke Signal Corps",
                                                                        "Bywater Sling & Stone Co."
                                                                    ]),
                                                     CreateBrigade ("Green Dragon Irregulars Brigade",
                                                                    [
                                                                        "Tookland Volunteer Skirmishers",
                                                                        "Gamgee Gardening Sappers",
                                                                        "Proudfoot Provisioners",
                                                                        "Baggins Estate Guard (Confused)",
                                                                        "Michel Delving Militia"
                                                                    ]),
                                                     CreateBrigade ("Westfarthing Home Guard Brigade",
                                                                    [
                                                                        "Hobbiton Breakfast Battalion",
                                                                        "Overhill Observation Otters",
                                                                        "Waymeet Wagon Ambushers",
                                                                        "Tuckborough Tunnel Rats",
                                                                        "Whitfurrows Scarecrow Sentries"
                                                                    ]),
                                                     CreateBrigade ("Eastfarthing Foragers Brigade",
                                                                    [
                                                                        "Frogmorton Frog-Herders",
                                                                        "Stock Road Supply Wagons",
                                                                        "Scary Quarry Slingers",
                                                                        "Budgeford Bridge Watchers",
                                                                        "Brockenbores Burrow Sappers"
                                                                    ])
                                                 ]),
                                 CreateDivision ("2nd Shire Logistics & Provisioning Division",
                                                 [
                                                     CreateBrigade ("Second Breakfast Supply Brigade",
                                                                    [
                                                                        "Elevenses Catering Corps",
                                                                        "Afternoon Tea Logistics Battalion",
                                                                        "Supper Requisition Riders",
                                                                        "Luncheon Wagon Train",
                                                                        "Pantry Replenishment Platoon"
                                                                    ]),
                                                     CreateBrigade ("Pipe-weed Morale Operations Brigade",
                                                                    [
                                                                        "Longbottom Leaf Distributers",
                                                                        "Old Toby Blenders",
                                                                        "Southern Star Smoke Ring Signallers",
                                                                        "Hornblower Snuff Scouts",
                                                                        "Pipe-weed Quality Inspectorate"
                                                                    ]),
                                                     CreateBrigade ("Ale & Provisions Transport Brigade",
                                                                    [
                                                                        "Green Dragon Keg Carriers",
                                                                        "Golden Perch Porter Brigade",
                                                                        "Ivy Bush Barrel Rollers",
                                                                        "Floating Log Fermenters",
                                                                        "Prancing Pony Resupply Run"
                                                                    ])
                                                 ])
                             ]),
                CreateCorps ("II Corps — Riders of Rohan (Now With Extra Horse Allergies)",
                             [
                                 CreateDivision ("1st Éored Cavalry Division",
                                                 [
                                                     CreateBrigade ("Edoras Royal Lancers Brigade",
                                                                    [
                                                                        "King's Spear Éored",
                                                                        "Meduseld Gate Guards",
                                                                        "Golden Hall Heralds",
                                                                        "Snowmane Memorial Lancers",
                                                                        "Théodred's Revenge Riders"
                                                                    ]),
                                                     CreateBrigade ("Helm's Deep Garrison Brigade",
                                                                    [
                                                                        "Deeping Wall Longbowmen",
                                                                        "Hornburg Gate Breakers",
                                                                        "Glittering Caves Reserves",
                                                                        "Deeping Stream Sappers",
                                                                        "Helm Hammerhand's Ghost Patrol (Honorary)"
                                                                    ]),
                                                     CreateBrigade ("Westfold Outriders Brigade",
                                                                    [
                                                                        "Fords of Isen Rapid Response",
                                                                        "Westfold Farmstead Defenders",
                                                                        "Gap of Rohan Scouts",
                                                                        "Dunharrow Beacon Lighters",
                                                                        "Snowbourn River Patrol"
                                                                    ]),
                                                     CreateBrigade ("Eastfold Éored Brigade",
                                                                    [
                                                                        "Aldburg Mounted Archers",
                                                                        "Entwash Patrol Riders",
                                                                        "Firien Wood Rangers",
                                                                        "Halifirien Signal Corps",
                                                                        "East Emnet Horse Archers"
                                                                    ])
                                                 ]),
                                 CreateDivision ("2nd Rohirric Reconnaissance Division",
                                                 [
                                                     CreateBrigade ("Wold & Foothills Scout Brigade",
                                                                    [
                                                                        "Fenmarch Fen Navigators",
                                                                        "Eastemnet Grass Readers",
                                                                        "Wold Hill Watchers",
                                                                        "Limlight Crossing Guards",
                                                                        "Entwood Border Scouts"
                                                                    ]),
                                                     CreateBrigade ("Rohirric Beacon Network Brigade",
                                                                    [
                                                                        "Amon Dîn Flame Keepers",
                                                                        "Eilenach Fire Starters",
                                                                        "Nardol Night Watchers",
                                                                        "Erelas Ember Tenders",
                                                                        "Halifirien Torch Relay"
                                                                    ])
                                                 ])
                             ]),
                CreateCorps ("III Corps — Gondorian Regulars (Denethor's Finest, Allegedly)",
                             [
                                 CreateDivision ("1st Minas Tirith Garrison Division",
                                                 [
                                                     CreateBrigade ("Tower Guard of the Citadel Brigade",
                                                                    [
                                                                        "White Tree Honour Guard",
                                                                        "Fountain Court Sentinels",
                                                                        "Tower of Ecthelion Watch",
                                                                        "Citadel Gate Wardens",
                                                                        "Palantír Chamber Guards (Nervous)"
                                                                    ]),
                                                     CreateBrigade ("First Circle Urban Defense Brigade",
                                                                    [
                                                                        "Gate of the City Defenders",
                                                                        "Lampwrights Street Barricaders",
                                                                        "Rath Celerdain Smithy Militia",
                                                                        "Old Guesthouse Volunteers",
                                                                        "Stonewain Street Phalanx"
                                                                    ]),
                                                     CreateBrigade ("Upper Circles Reserve Brigade",
                                                                    [
                                                                        "Houses of Healing Medics",
                                                                        "Fen Hollen Secret Passage Guards",
                                                                        "Silent Street Pallbearers (Dual-role)",
                                                                        "Hallows Hill Archers",
                                                                        "Fifth Circle Catapult Crews"
                                                                    ]),
                                                     CreateBrigade ("Pelennor Field Artillery Brigade",
                                                                    [
                                                                        "Rammas Echor Wall Crews",
                                                                        "Trebuchet Battery Alpha (Named 'Grond's Nightmare')",
                                                                        "North Gate Boulder Launchers",
                                                                        "Causeway Fort Ballista Teams",
                                                                        "Oil Cauldron Engineers"
                                                                    ])
                                                 ]),
                                 CreateDivision ("2nd Ithilien Ranger Division",
                                                 [
                                                     CreateBrigade ("North Ithilien Ambush Brigade",
                                                                    [
                                                                        "Henneth Annûn Window Watchers",
                                                                        "Forbidden Pool Fish Guards",
                                                                        "Harad Road Ambushers",
                                                                        "Crossroads Monument Defenders",
                                                                        "Morgul Vale Border Patrol (Hazard Pay)"
                                                                    ]),
                                                     CreateBrigade ("South Ithilien Infiltration Brigade",
                                                                    [
                                                                        "Poros River Raiders",
                                                                        "Harondor Desert Ghillie Snipers",
                                                                        "Anduin East Bank Scouts",
                                                                        "Emyn Arnen Hillside Lurkers",
                                                                        "Minas Morgul Perimeter Watchers (Very Nervous)"
                                                                    ])
                                                 ]),
                                 CreateDivision ("3rd Coastal Defense Division",
                                                 [
                                                     CreateBrigade ("Pelargir Naval Infantry Brigade",
                                                                    [
                                                                        "Corsair Ship-Boarders",
                                                                        "Anduin River Marines",
                                                                        "Harlond Dock Defenders",
                                                                        "Lebennin Shore Patrol",
                                                                        "Ethir Anduin Delta Watch"
                                                                    ]),
                                                     CreateBrigade ("Dol Amroth Swan Knight Brigade",
                                                                    [
                                                                        "Prince's Household Cavalry",
                                                                        "Sea-ward Tower Sentinels",
                                                                        "Belfalas Bay Cutlass Company",
                                                                        "Cobas Haven Longbowmen",
                                                                        "Edhellond Harbour Guard"
                                                                    ]),
                                                     CreateBrigade ("Lamedon Mountain Brigade",
                                                                    [
                                                                        "Calembel Hill Fighters",
                                                                        "Ringló Vale Pikemen",
                                                                        "Ciril Ford Wardens",
                                                                        "Tarlang's Neck Pass Guards",
                                                                        "Erech Stone Oath-Reciter Platoon (Spooky)"
                                                                    ])
                                                 ])
                             ]),
                CreateCorps ("IV Corps — Elven Contingent (Fashionably Late Since the First Age)",
                             [
                                 CreateDivision ("1st Lothlórien Light Division",
                                                 [
                                                     CreateBrigade ("Galadhrim Archers Brigade",
                                                                    [
                                                                        "Caras Galadhon Treetop Snipers",
                                                                        "Nimrodel Stream Guards",
                                                                        "Celebrant Silverbow Company",
                                                                        "Cerin Amroth Twilight Watch",
                                                                        "Mallorn Canopy Rangers"
                                                                    ]),
                                                     CreateBrigade ("Mirror Guard Brigade",
                                                                    [
                                                                        "Galadriel's Mirror Pool Sentinels",
                                                                        "Phial of Light Sappers",
                                                                        "Nenya Frost Ward Company",
                                                                        "Lothlórien Border Weavers",
                                                                        "Elanor Meadow Camouflage Corps"
                                                                    ])
                                                 ]),
                                 CreateDivision ("2nd Mirkwood Expeditionary Division",
                                                 [
                                                     CreateBrigade ("Woodland Realm Scout Brigade",
                                                                    [
                                                                        "Thranduil's Palace Guard",
                                                                        "Forest River Patrol Boats",
                                                                        "Enchanted Stream Warning Party",
                                                                        "Spider Bane Extermination Company",
                                                                        "Elven-king's Wine Cellar Escape Artists"
                                                                    ]),
                                                     CreateBrigade ("Northern Greenwood Reclamation Brigade",
                                                                    [
                                                                        "Amon Lanc Ruin Explorers",
                                                                        "Dol Guldur Perimeter Gawkers",
                                                                        "East Bight Frontier Force",
                                                                        "Old Forest Road Clearers",
                                                                        "Mountains of Mirkwood Snow Elves"
                                                                    ])
                                                 ]),
                                 CreateDivision ("3rd Rivendell Advisory Division",
                                                 [
                                                     CreateBrigade ("Last Homely House Strategy Brigade",
                                                                    [
                                                                        "Council of Elrond Debating Corps",
                                                                        "Bruinen Flood Engineers",
                                                                        "Imladris Archive Keepers",
                                                                        "Vilya Wind Callers",
                                                                        "Half-Elven Diplomacy Detachment"
                                                                    ]),
                                                     CreateBrigade ("Grey Havens Rear Guard Brigade",
                                                                    [
                                                                        "Círdan's Shipwright Marines",
                                                                        "Mithlond Harbour Watch",
                                                                        "Gulf of Lhûn Coastal Battery",
                                                                        "Tower Hills Palantír Peepers",
                                                                        "Harlond Retirement Processing Centre"
                                                                    ])
                                                 ])
                             ]),
                CreateCorps ("V Corps — Miscellaneous Allies (The 'We Also Showed Up' Brigade)",
                             [
                                 CreateDivision ("1st Dwarvish Engineering Division",
                                                 [
                                                     CreateBrigade ("Erebor Heavy Siege Brigade",
                                                                    [
                                                                        "Iron Hills Mattock Assault Company",
                                                                        "Dáin's Ram Riders",
                                                                        "Raven Hill Signal Corps",
                                                                        "Dale Crossbow Auxiliaries",
                                                                        "Erebor Tunnel Demolition Team"
                                                                    ]),
                                                     CreateBrigade ("Moria Reclamation Brigade",
                                                                    [
                                                                        "Durin's Bane Avoidance Platoon",
                                                                        "Twenty-first Hall Prospectors",
                                                                        "Mirrormere Sapping Company",
                                                                        "Bridge of Khazad-dûm Safety Inspectors",
                                                                        "Mithril Vein Security Detail"
                                                                    ]),
                                                     CreateBrigade ("Glittering Caves Fortification Brigade",
                                                                    [
                                                                        "Aglarond Crystal Miners",
                                                                        "Helm's Deep Expansion Crew",
                                                                        "Gemstone Grenadiers",
                                                                        "Cavern Acoustic Artillery",
                                                                        "Gimli's Personal Beautification Squad"
                                                                    ])
                                                 ]),
                                 CreateDivision ("2nd Entish Slow-March Division",
                                                 [
                                                     CreateBrigade ("Fangorn Assault Brigade",
                                                                    [
                                                                        "Treebeard's Boomstompers",
                                                                        "Huorn Stealth Forest",
                                                                        "Quickbeam's Impatient Vanguard",
                                                                        "Entwash Dam Breakers",
                                                                        "Derndingle Moot Organizers"
                                                                    ]),
                                                     CreateBrigade ("Isengard Demolition Brigade",
                                                                    [
                                                                        "Orthanc Flooding Division",
                                                                        "Dam Break Engineers",
                                                                        "Saruman's Tower Besiegers",
                                                                        "Ring of Isengard Gardeners (Post-War)",
                                                                        "Orc Pit Composters"
                                                                    ])
                                                 ]),
                                 CreateDivision ("3rd Eagle Air Support Division",
                                                 [
                                                     CreateBrigade ("Great Eagle Strike Brigade",
                                                                    [
                                                                        "Gwaihir's Windlord Squadron",
                                                                        "Meneldor Dive-Bombers",
                                                                        "Landroval Extraction Flight",
                                                                        "Thorondor Memorial Wing",
                                                                        "Eagles Who Definitely Could Have Carried the Ring (But Chose Not To)"
                                                                    ]),
                                                     CreateBrigade ("Beorning Ground-Air Assault Brigade",
                                                                    [
                                                                        "Bear-form Berserkers",
                                                                        "Beorn's Honey Logistics",
                                                                        "Carrock Ford Watchers",
                                                                        "Anduin Vale Shape-shifter Scouts",
                                                                        "Giant Bee Close Air Support"
                                                                    ])
                                                 ]),
                                 CreateDivision ("4th Dead Men of Dunharrow Division (Temporary Contract)",
                                                 [
                                                     CreateBrigade ("Oath-Breaker Vanguard Brigade",
                                                                    [
                                                                        "Dimholt Door Ghosts",
                                                                        "Paths of the Dead Tour Guides",
                                                                        "Stone of Erech Oath Renewers",
                                                                        "Blackroot Vale Spectral Cavalry",
                                                                        "Pelargir Ghost Ship Boarders"
                                                                    ]),
                                                     CreateBrigade ("Haunted Mountain Reserves Brigade",
                                                                    [
                                                                        "Dwimorberg Wailing Battalion",
                                                                        "White Mountains Phantom Patrol",
                                                                        "Haunted Pass Chill Brigade",
                                                                        "Spectral Siege Tower Pushers",
                                                                        "Ghostly Accountants (Counting Remaining Oath Days)"
                                                                    ])
                                                 ])
                             ])
            ]
        };

        return army;

        static CorpsObject CreateCorps (string name, List<Division> divisions) => new () { Designation = name, Divisions = divisions };

        static Division CreateDivision (string name, List<Brigade> brigades) => new () { Designation = name, Brigades = brigades };

        static Brigade CreateBrigade (string name, List<string> unitNames)
        {
            List<Unit> units = unitNames.Select (n => new Unit { Name = n }).ToList ();

            return new Brigade { Designation = name, Units = units };
        }
    }
}
