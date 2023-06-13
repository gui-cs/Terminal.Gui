using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "AccordionExample", Description: "Demonstrates use of the AccordionView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
public class AccordionExample : Scenario {
    public override void Setup ()
    {
        var editLabel = new Label ("This is a regular label") {
            X = 0,
            Y = 0,
        };
        Win.Add (editLabel);

        var accordion = new AccordionView{
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var tbl = new TableView () {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
        };

        ProcessTable.CreateProcessTable(tbl);

        accordion.AddSection("Section 1",new Label ("Hello!!"));
        accordion.AddSection("Section 2",new Label ("Hello2"));
        accordion.AddSection("Section 3",tbl);
        Win.Add(accordion);
    }
}
