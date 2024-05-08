using System.CodeDom.Compiler;
using System.Text;

namespace Terminal.Gui.Analyzers.Internal.Tests;

[TestFixture]
[Category ("Extension Methods")]
[TestOf (typeof (IndentedTextWriterExtensions))]
[Parallelizable (ParallelScope.Children)]
public class IndentedTextWriterExtensionsTests
{
    [Test]
    public void Pop_Decrements ()
    {
        StringBuilder sb = new (0);
        using var sw = new StringWriter (sb);
        using var writer = new IndentedTextWriter (sw);
        writer.Indent = 5;

        Assume.That (writer.Indent, Is.EqualTo (5));

        writer.Pop ();
        Assert.That (writer.Indent, Is.EqualTo (4));
    }

    [Test]
    public void Pop_WithClosing_WritesAndPops ([Values ("}", ")", "]")] string scopeClosing)
    {
        StringBuilder sb = new (256);
        using var sw = new StringWriter (sb);
        using var writer = new IndentedTextWriter (sw, "  ");
        writer.Indent = 5;
        writer.Flush ();
        Assume.That (writer.Indent, Is.EqualTo (5));
        Assume.That (sb.Length, Is.Zero);

        // Need to write something first, or IndentedTextWriter won't emit the indentation for the first call.
        // So we'll write an empty line.
        writer.WriteLine ();

        for (ushort indentCount = 5; indentCount > 0;)
        {
            writer.Pop (scopeClosing);
            Assert.That (writer.Indent, Is.EqualTo (--indentCount));
        }

        writer.Flush ();
        var result = sb.ToString ().Replace ("\r\n", "\n");

        Assert.That (
                     result,
                     Is.EqualTo (
                                 @$"
        {scopeClosing}
      {scopeClosing}
    {scopeClosing}
  {scopeClosing}
{scopeClosing}
".Replace ("\r\n", "\n")
                                ));
    }

    [Test]
    public void Push_Increments ()
    {
        StringBuilder sb = new (32);
        using var sw = new StringWriter (sb);
        using var writer = new IndentedTextWriter (sw, "  ");

        for (int indentCount = 0; indentCount < 5; indentCount++)
        {
            writer.Push ();
            Assert.That (writer.Indent, Is.EqualTo (indentCount + 1));
        }
    }

    [Test]
    public void Push_WithOpening_WritesAndPushes ([Values ('{', '(', '[')] char scopeOpening)
    {
        StringBuilder sb = new (256);
        using var sw = new StringWriter (sb);
        using var writer = new IndentedTextWriter (sw, "  ");

        for (ushort indentCount = 0; indentCount < 5;)
        {
            writer.Push ("Opening UninterestingEnum", scopeOpening);
            Assert.That (writer.Indent, Is.EqualTo (++indentCount));
        }

        writer.Flush ();
        var result = sb.ToString ().Replace ("\r\n", "\n").Trim ();

        Assert.That (
                     result,
                     Is.EqualTo (
                                 @$"
Opening UninterestingEnum
{scopeOpening}
  Opening UninterestingEnum
  {scopeOpening}
    Opening UninterestingEnum
    {scopeOpening}
      Opening UninterestingEnum
      {scopeOpening}
        Opening UninterestingEnum
        {scopeOpening}".Replace ("\r\n", "\n")
                       .Trim ()
                                ));
    }
}
