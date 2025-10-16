
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Combining Marks", "Illustrates how Unicode Combining Marks work (or don't).")]
[ScenarioCategory ("Text and Formatting")]
public class CombiningMarks : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var top = new Toplevel ();

        top.DrawComplete += (s, e) =>
        {
            // Forces reset _lineColsOffset because we're dealing with direct draw
            Application.ClearScreenNextIteration = true;

            var i = -1;
            top.AddStr ("Terminal.Gui only supports combining marks that normalize. See Issue #2616.");
            top.Move (0, ++i);
            top.AddStr ("\u0301<- \"\\u0301\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0301]<- \"[\\u0301]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[ \u0301]<- \"[ \\u0301]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0301 ]<- \"[\\u0301 ]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("\u0301\u0301\u0328<- \"\\u0301\\u0301\\u0328\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0301\u0301\u0328]<- \"[\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[a\u0301\u0301\u0328]<- \"[a\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddRune ('[');
            top.AddRune ('a');
            top.AddRune ('\u0301');
            top.AddRune ('\u0301');
            top.AddRune ('\u0328');
            top.AddRune (']');
            top.AddStr ("<- \"[a\\u0301\\u0301\\u0328]\" using AddRune for each.");
            top.Move (0, ++i);
            top.AddStr ("[a\u0301\u0301\u0328]<- \"[a\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[e\u0301\u0301\u0328]<- \"[e\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[e\u0328\u0301]<- \"[e\\u0328\\u0301]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("\u00ad<- \"\\u00ad\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u00ad]<- \"[\\u00ad]\" using AddStr.");
            top.Move (0, ++i);
            top.AddRune ('[');
            top.AddRune ('\u00ad');
            top.AddRune (']');
            top.AddStr ("<- \"[\\u00ad]\" using AddRune for each.");
            i++;
            top.Move (0, ++i);
            top.AddStr ("From now on we are using TextFormatter");
            TextFormatter tf = new () { Text = "[e\u0301\u0301\u0328]<- \"[e\\u0301\\u0301\\u0328]\" using TextFormatter." };
            tf.Draw (new (0, ++i, tf.Text.Length, 1), top.GetAttributeForRole (VisualRole.Normal), top.GetAttributeForRole (VisualRole.Normal));
            tf.Text = "[e\u0328\u0301]<- \"[e\\u0328\\u0301]\" using TextFormatter.";
            tf.Draw (new (0, ++i, tf.Text.Length, 1), top.GetAttributeForRole (VisualRole.Normal), top.GetAttributeForRole (VisualRole.Normal));
            i++;
            top.Move (0, ++i);
            top.AddStr ("From now on we are using Surrogate pairs with combining diacritics");
            top.Move (0, ++i);
            top.AddStr ("[\ud835\udc4b\u0302]<- \"[\\ud835\\udc4b\\u0302]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\ud83d\udc68\ud83e\uddd2]<- \"[\\ud83d\\udc68\\ud83e\\uddd2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("\u200d<- \"\\u200d\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u200d]<- \"[\\u200d]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\ud83d\udc68\u200d\ud83e\uddd2]<- \"[\\ud83d\\udc68\\u200d\\ud83e\\uddd2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F469\U0001F9D2]<- \"[\\U0001F469\\U0001F9D2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F469\u200D\U0001F9D2]<- \"[\\U0001F469\\u200D\\U0001F9D2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F468\U0001F469\U0001F9D2]<- \"[\\U0001F468\\U0001F469\\U0001F9D2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F468\u200D\U0001F469\u200D\U0001F9D2]<- \"[\\U0001F468\\u200D\\U0001F469\\u200D\\U0001F9D2]\" using AddStr.");
        };

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }
}
