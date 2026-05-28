// Copilot - Claude Opus 4.7 - SPIKE (delete before mass rollout if pattern fails)

using Microsoft.Extensions.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Spike: validate that the MEC binder writes to <c>{ get; internal set; }</c> properties on records used as
///     nested bind targets via the two-pass <c>Bind(existingInstance)</c> overlay pattern.
/// </summary>
/// <remarks>
///     The sender's A2 design contract for #5416 requires immutable <c>*Settings</c> records with <c>internal set</c>,
///     atomic swap via <c>Volatile.Write</c>, and a two-pass MEC overlay. If the MEC binder honors only public setters
///     by default (which my read of <c>Microsoft.Extensions.Configuration.Binder</c> suggests), the pattern collapses
///     and the design needs revisiting.
/// </remarks>
public class MecBinderAccessibilitySpike
{
    public sealed record InternalSetPoco
    {
        public string Name { get; internal set; } = "default-name";
        public int Count { get; internal set; } = 0;
    }

    public sealed record InitOnlyPoco
    {
        public string Name { get; init; } = "default-name";
        public int Count { get; init; } = 0;
    }

    private static IConfiguration ConfigFromJson (string json)
    {
        MemoryStream stream = new ();
        StreamWriter writer = new (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return new ConfigurationBuilder ().AddJsonStream (stream).Build ();
    }

    /// <summary>
    ///     Documents that <c>{ get; internal set; }</c> properties are <b>silently ignored</b> by MEC's binder via
    ///     the default <c>Bind(existingInstance)</c> path. Both passes complete with no exception, but neither pass
    ///     mutates the POCO — both properties stay at their constructor defaults.
    /// </summary>
    /// <remarks>
    ///     This contradicts the A2 design contract proposed by the source session for #5416, which prescribed
    ///     <c>{ get; internal set; }</c>. The opposite holds: <c>internal set</c> fails; <c>init</c> works.
    /// </remarks>
    [Fact]
    public void TwoPassBind_InternalSet_SilentlyIgnoredByBinder ()
    {
        IConfiguration cfg = ConfigFromJson ("""
                                             {
                                               "Root":   { "Name": "from-root", "Count": 10 },
                                               "Themes": { "Dark": { "Poco": { "Count": 99 } } }
                                             }
                                             """);

        InternalSetPoco next = new ();
        cfg.GetSection ("Root").Bind (next);
        cfg.GetSection ("Themes:Dark:Poco").Bind (next);

        // Observed: ctor defaults preserved on both properties; binder never wrote.
        // (Default BindingFlags = Public | Instance excludes internal accessors.)
        Assert.Equal ("default-name", next.Name);
        Assert.Equal (0, next.Count);
    }

    /// <summary>
    ///     Documents that <c>{ get; init; }</c> properties <b>are</b> written by MEC's <c>Bind(existingInstance)</c>
    ///     via the two-pass overlay. The root pass populates one property, a subsequent overlay pass writes a second
    ///     property without disturbing the first.
    /// </summary>
    /// <remarks>
    ///     This is the working alternative to the failed <c>internal set</c> pattern. It is what the A2 manager
    ///     rewire should use for <see cref="ThemeDefinition"/>'s 18 nullable subsection POCOs.
    /// </remarks>
    [Fact]
    public void TwoPassBind_InitOnly_OverlaysCorrectly ()
    {
        IConfiguration cfg = ConfigFromJson ("""
                                             {
                                               "Root":   { "Name": "from-root", "Count": 10 },
                                               "Themes": { "Dark": { "Poco": { "Count": 99 } } }
                                             }
                                             """);

        InitOnlyPoco next = new ();
        cfg.GetSection ("Root").Bind (next);
        cfg.GetSection ("Themes:Dark:Poco").Bind (next);

        Assert.Equal ("from-root", next.Name);
        Assert.Equal (99, next.Count);
    }

    /// <summary>
    ///     Documents that opting into <c>BinderOptions.BindNonPublicProperties = true</c> rescues the
    ///     <c>internal set</c> pattern — the binder writes both properties. This is the escape hatch if the design
    ///     truly requires assembly-private mutability; the trade-off is an extra trim hint and a non-default code path
    ///     at every bind site.
    /// </summary>
    [Fact]
    public void TwoPassBind_InternalSet_WorksWithBindNonPublicProperties ()
    {
        IConfiguration cfg = ConfigFromJson ("""
                                             {
                                               "Root":   { "Name": "from-root", "Count": 10 },
                                               "Themes": { "Dark": { "Poco": { "Count": 99 } } }
                                             }
                                             """);

        InternalSetPoco next = new ();
        cfg.GetSection ("Root").Bind (next, o => o.BindNonPublicProperties = true);
        cfg.GetSection ("Themes:Dark:Poco").Bind (next, o => o.BindNonPublicProperties = true);

        Assert.Equal ("from-root", next.Name);
        Assert.Equal (99, next.Count);
    }
}
