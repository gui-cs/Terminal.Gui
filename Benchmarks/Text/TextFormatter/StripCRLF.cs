using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui;

namespace Terminal.Gui.Benchmarks.Text.TextFormatter;

[MemoryDiagnoser]
public class StripCRLF
{
    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="keepNewLine"></param>
    /// <returns></returns>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public string Previous (string str, bool keepNewLine)
    {
        return RuneListToString (str, keepNewLine);
    }

    /// <summary>
    /// Benchmark for current implementation with StringBuilder and char span index of search.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public string Current (string str, bool keepNewLine)
    {
        return Tui.TextFormatter.StripCRLF (str, keepNewLine);
    }

    /// <summary>
    /// Previous implementation with intermediate list allocation.
    /// </summary>
    private static string RuneListToString (string str, bool keepNewLine = false)
    {
        List<Rune> runes = str.ToRuneList ();

        for (var i = 0; i < runes.Count; i++)
        {
            switch ((char)runes [i].Value)
            {
                case '\n':
                    if (!keepNewLine)
                    {
                        runes.RemoveAt (i);
                    }

                    break;

                case '\r':
                    if (i + 1 < runes.Count && runes [i + 1].Value == '\n')
                    {
                        runes.RemoveAt (i);

                        if (!keepNewLine)
                        {
                            runes.RemoveAt (i);
                        }

                        i++;
                    }
                    else
                    {
                        if (!keepNewLine)
                        {
                            runes.RemoveAt (i);
                        }
                    }

                    break;
            }
        }

        return StringExtensions.ToString (runes);
    }

    public IEnumerable<object []> DataSource ()
    {
        string textSource =
                """
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
				Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
				Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
				""";
        // Consistent line endings between systems keeps performance evaluation more consistent.
        textSource = textSource.ReplaceLineEndings ("\r\n");

        bool[] permutations = [true, false];
        foreach (bool keepNewLine in permutations)
        {
            yield return [textSource [..1], keepNewLine];
            yield return [textSource [..10], keepNewLine];
            yield return [textSource [..100], keepNewLine];
            yield return [textSource [..(textSource.Length / 2)], keepNewLine];
            yield return [textSource, keepNewLine];
        }
    }
}
