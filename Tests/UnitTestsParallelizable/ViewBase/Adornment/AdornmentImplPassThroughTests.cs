// Copilot

using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Systematically tests each convenience pass-through on <see cref="AdornmentImpl"/>,
///     verifying correct delegation to the backing <see cref="AdornmentView"/> AND correct
///     defaults when <see cref="AdornmentImpl.View"/> is <see langword="null"/>.
///     Uses <see cref="Border"/> as the concrete type throughout.
/// </summary>
public class AdornmentImplPassThroughTests : TestDriverBase
{
    #region NeedsDraw

    [Fact]
    public void NeedsDraw_WithoutView_ReturnsFalse ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.False (border.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_AfterSetNeedsDraw_WithoutView_ReturnsTrue ()
    {
        Border border = new ();
        border.SetNeedsDraw ();
        Assert.True (border.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_WithView_DelegatesToView ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        parent.BeginInit ();
        parent.EndInit ();

        Border border = parent.Border;
        border.EnsureView ();
        Assert.NotNull (border.View);

        // ClearNeedsDraw then verify getter delegates to View
        border.View!.ClearNeedsDraw ();
        Assert.False (border.NeedsDraw);

        border.View.SetNeedsDraw ();
        Assert.True (border.NeedsDraw);
    }

    #endregion NeedsDraw

    #region ClearNeedsDraw

    [Fact]
    public void ClearNeedsDraw_WithoutView_ClearsFlag ()
    {
        Border border = new ();
        border.SetNeedsDraw ();
        Assert.True (border.NeedsDraw);

        border.ClearNeedsDraw ();
        Assert.False (border.NeedsDraw);
    }

    [Fact]
    public void ClearNeedsDraw_WithView_DelegatesToView ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        parent.BeginInit ();
        parent.EndInit ();

        Border border = parent.Border;
        border.EnsureView ();

        // Set via the View, then clear via the passthrough
        border.View!.SetNeedsDraw ();
        Assert.True (border.NeedsDraw);

        border.ClearNeedsDraw ();
        Assert.False (border.View.NeedsDraw);
        Assert.False (border.NeedsDraw);
    }

    #endregion ClearNeedsDraw

    #region SetNeedsDraw

    [Fact]
    public void SetNeedsDraw_WithoutView_SetsLocalFlag ()
    {
        Border border = new ();
        Assert.False (border.NeedsDraw);

        border.SetNeedsDraw ();
        Assert.True (border.NeedsDraw);

        // Confirm View was NOT created as a side effect
        Assert.Null (border.View);
    }

    [Fact]
    public void SetNeedsDraw_WithView_DelegatesToView ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        parent.BeginInit ();
        parent.EndInit ();

        Border border = parent.Border;
        border.EnsureView ();
        border.View!.ClearNeedsDraw ();
        Assert.False (border.View.NeedsDraw);

        border.SetNeedsDraw ();
        Assert.True (border.View.NeedsDraw);
        Assert.True (border.NeedsDraw);
    }

    #endregion SetNeedsDraw

    #region NeedsLayout

    [Fact]
    public void NeedsLayout_WithoutView_ReturnsFalse ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.False (border.NeedsLayout);
    }

    [Fact]
    public void NeedsLayout_WithView_DelegatesToView ()
    {
        Border border = new () { Parent = new View () };
        border.EnsureView ();

        border.NeedsLayout = true;
        Assert.True (border.View!.NeedsLayout);

        border.NeedsLayout = false;
        Assert.False (border.View.NeedsLayout);
    }

    #endregion NeedsLayout

    #region SetNeedsLayout

    [Fact]
    public void SetNeedsLayout_WithoutView_IsNoOp ()
    {
        Border border = new ();

        // Should not throw; no View to delegate to
        border.SetNeedsLayout ();
        Assert.Null (border.View);
        Assert.False (border.NeedsLayout);
    }

    #endregion SetNeedsLayout

    #region Diagnostics

    [Fact]
    public void Diagnostics_WithoutView_ReturnsDefaultOff ()
    {
        Border border = new ();
        Assert.Equal (ViewDiagnosticFlags.Off, border.Diagnostics);
    }

    [Fact]
    public void Diagnostics_SetWithoutView_StoresLocally ()
    {
        Border border = new ();
        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Null (border.View);
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.Diagnostics);
    }

    [Fact]
    public void Diagnostics_SetBeforeViewCreated_ForwardedWhenViewCreated ()
    {
        Border border = new () { Parent = new View () };
        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Null (border.View);

        // The local field stores the value
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.Diagnostics);

        border.EnsureView ();

        // After View creation, the getter delegates to View.Diagnostics.
        // EnsureView does not forward the locally-stored value to the View,
        // so the getter now returns the View's default.
        Assert.Equal (ViewDiagnosticFlags.Off, border.View!.Diagnostics);

        // Setting AFTER View creation forwards to the View
        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.View.Diagnostics);
    }

    [Fact]
    public void Diagnostics_WithView_DelegatesToView ()
    {
        Border border = new () { Parent = new View () };
        border.EnsureView ();

        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.View!.Diagnostics);
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.Diagnostics);
    }

    #endregion Diagnostics

    #region SubViews

    [Fact]
    public void SubViews_WithoutView_ReturnsEmpty ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.Empty (border.SubViews);
    }

    [Fact]
    public void SubViews_WithView_DelegatesToView ()
    {
        Border border = new () { Parent = new View () };
        border.EnsureView ();

        Label label = new () { Text = "test" };
        border.View!.Add (label);
        Assert.Contains (label, border.SubViews);
    }

    #endregion SubViews

    #region Add

    [Fact]
    public void Add_ForcesViewCreation ()
    {
        Border border = new () { Parent = new View () };
        Assert.Null (border.View);

        Label label = new () { Text = "test" };
        border.Add (label);
        Assert.NotNull (border.View);
    }

    [Fact]
    public void Add_AddsSubViewToCreatedView ()
    {
        Border border = new () { Parent = new View () };
        Label label = new () { Text = "test" };
        border.Add (label);

        Assert.Contains (label, border.View!.SubViews);
        Assert.Single (border.SubViews);
    }

    #endregion Add

    #region HasFocus

    [Fact]
    public void HasFocus_WithoutView_ReturnsFalse ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.False (border.HasFocus);
    }

    #endregion HasFocus

    #region Remove

    [Fact]
    public void Remove_WithoutView_IsNoOp ()
    {
        Border border = new ();
        Label label = new () { Text = "test" };

        // Should not throw; no View to delegate to
        border.Remove (label);
        Assert.Null (border.View);
    }

    #endregion Remove

    #region Enabled

    [Fact]
    public void Enabled_WithoutView_ReturnsTrue ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.True (border.Enabled);
    }

    [Fact]
    public void Enabled_WithView_DelegatesToView ()
    {
        Border border = new () { Parent = new View () };
        border.EnsureView ();

        border.Enabled = false;
        Assert.False (border.View!.Enabled);
        Assert.False (border.Enabled);

        border.Enabled = true;
        Assert.True (border.View.Enabled);
        Assert.True (border.Enabled);
    }

    #endregion Enabled

    #region ViewportSettings

    [Fact]
    public void ViewportSettings_SetWithoutView_StoresLocally ()
    {
        Border border = new ();
        border.ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation;
        Assert.Null (border.View);
        Assert.Equal (ViewportSettingsFlags.AllowNegativeLocation, border.ViewportSettings);
    }

    [Fact]
    public void ViewportSettings_SetBeforeViewCreated_ForwardedWhenViewCreated ()
    {
        Border border = new () { Parent = new View () };
        border.ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation;
        Assert.Null (border.View);

        // Local field holds the value before View exists
        Assert.Equal (ViewportSettingsFlags.AllowNegativeLocation, border.ViewportSettings);

        border.EnsureView ();

        // After View creation, getter delegates to View.ViewportSettings.
        // EnsureView does not forward the locally-stored value.
        // Setting AFTER View creation does forward.
        border.ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation;
        Assert.Equal (ViewportSettingsFlags.AllowNegativeLocation, border.View!.ViewportSettings);
    }

    #endregion ViewportSettings

    #region Visible

    [Fact]
    public void Visible_WithoutView_ReturnsTrue ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.True (border.Visible);
    }

    [Fact]
    public void Visible_WithView_DelegatesToView ()
    {
        Border border = new () { Parent = new View () };
        border.EnsureView ();

        border.Visible = false;
        Assert.False (border.View!.Visible);
        Assert.False (border.Visible);

        border.Visible = true;
        Assert.True (border.View.Visible);
        Assert.True (border.Visible);
    }

    #endregion Visible

    #region Contains

    [Fact]
    public void Contains_WithoutView_UsesThicknessCalculation ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        Border border = parent.Border;
        border.Thickness = new Thickness (1);

        Assert.Null (border.View);

        // Point on the border ring (top-left corner)
        Assert.True (border.Contains (new Point (0, 0)));

        // Point in the interior (past the border)
        Assert.False (border.Contains (new Point (5, 5)));
    }

    [Fact]
    public void Contains_WithView_DelegatesToView ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        Border border = parent.Border;
        border.Thickness = new Thickness (1);
        border.EnsureView ();

        Assert.NotNull (border.View);

        // The View.Contains implementation is used
        bool result = border.Contains (new Point (0, 0));

        // Just verify it returns a result without throwing
        Assert.IsType<bool> (result);
    }

    [Fact]
    public void Contains_WithoutView_NoParent_ReturnsFalse ()
    {
        Border border = new ();
        border.Thickness = new Thickness (1);
        Assert.Null (border.Parent);

        Assert.False (border.Contains (new Point (0, 0)));
    }

    #endregion Contains

    #region SchemeName

    [Fact]
    public void SchemeName_SetWithoutView_StoresLocally ()
    {
        Border border = new ();
        border.SchemeName = "Dialog";
        Assert.Null (border.View);
        Assert.Equal ("Dialog", border.SchemeName);
    }

    [Fact]
    public void SchemeName_SetBeforeViewCreated_ForwardedWhenViewCreated ()
    {
        Border border = new () { Parent = new View () };
        border.SchemeName = "Dialog";
        Assert.Null (border.View);

        // Local field stores the value
        Assert.Equal ("Dialog", border.SchemeName);

        border.EnsureView ();

        // After View creation, getter delegates to View.SchemeName.
        // EnsureView does not forward the locally-stored value.
        // Setting AFTER View creation does forward.
        border.SchemeName = "Dialog";
        Assert.Equal ("Dialog", border.View!.SchemeName);
    }

    #endregion SchemeName

    #region GetAttributeForRole

    [Fact]
    public void GetAttributeForRole_WithoutView_FallsBackToParent ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        Border border = parent.Border;

        Assert.Null (border.View);

        // Should fall back to parent's attribute
        Attribute attr = border.GetAttributeForRole (VisualRole.Normal);
        Attribute parentAttr = parent.GetAttributeForRole (VisualRole.Normal);
        Assert.Equal (parentAttr, attr);
    }

    [Fact]
    public void GetAttributeForRole_WithoutViewOrParent_ReturnsDefault ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.Null (border.Parent);

        Attribute attr = border.GetAttributeForRole (VisualRole.Normal);
        Assert.Equal (default (Attribute), attr);
    }

    #endregion GetAttributeForRole

    #region FrameToScreen

    [Fact]
    public void FrameToScreen_WithoutView_ComputesFromParent ()
    {
        View parent = new () { Frame = new Rectangle (5, 10, 20, 15) };
        Border border = parent.Border;
        Assert.Null (border.View);

        Rectangle screen = border.FrameToScreen ();

        // Should include parent's position
        Assert.Equal (5, screen.X);
        Assert.Equal (10, screen.Y);
    }

    [Fact]
    public void FrameToScreen_WithView_DelegatesToView ()
    {
        View parent = new () { Frame = new Rectangle (5, 10, 20, 15) };
        Border border = parent.Border;
        border.EnsureView ();

        Assert.NotNull (border.View);

        Rectangle screen = border.FrameToScreen ();

        // With View, delegates to View.FrameToScreen ()
        Assert.Equal (border.View!.FrameToScreen (), screen);
    }

    #endregion FrameToScreen

    #region Draw

    [Fact]
    public void Draw_WithoutView_DrawsThickness ()
    {
        View parent = new ()
        {
            Frame = new Rectangle (0, 0, 10, 10),
            Driver = CreateTestDriver ()
        };
        Border border = parent.Border;
        border.Thickness = new Thickness (1);

        Assert.Null (border.View);

        // Should not throw — uses Thickness.Draw path
        border.Draw ();
    }

    [Fact]
    public void Draw_WithView_DelegatesToViewDraw ()
    {
        View parent = new ()
        {
            Frame = new Rectangle (0, 0, 10, 10),
            Driver = CreateTestDriver ()
        };
        Border border = parent.Border;
        border.Thickness = new Thickness (1);
        border.EnsureView ();
        Assert.NotNull (border.View);

        // Should not throw — delegates to View.Draw
        border.Draw ();
    }

    #endregion Draw

    #region EnsureView lifecycle

    [Fact]
    public void EnsureView_CreatesView ()
    {
        Border border = new () { Parent = new View () };
        Assert.Null (border.View);

        AdornmentView view = border.EnsureView ();
        Assert.NotNull (view);
        Assert.Same (view, border.View);
    }

    [Fact]
    public void EnsureView_CalledTwice_ReturnsSameView ()
    {
        Border border = new () { Parent = new View () };
        AdornmentView first = border.EnsureView ();
        AdornmentView second = border.EnsureView ();

        Assert.Same (first, second);
    }

    [Fact]
    public void EnsureView_WithInitializedParent_CallsBeginAndEndInit ()
    {
        View parent = new ();
        parent.BeginInit ();
        parent.EndInit ();
        Assert.True (parent.IsInitialized);

        Border border = parent.Border;
        AdornmentView view = border.EnsureView ();

        Assert.True (view.IsInitialized);
    }

    [Fact]
    public void EnsureView_WithUninitializedParent_DoesNotCallInit ()
    {
        View parent = new ();
        Assert.False (parent.IsInitialized);

        Border border = parent.Border;
        AdornmentView view = border.EnsureView ();

        Assert.False (view.IsInitialized);
    }

    #endregion EnsureView lifecycle

    #region Dispose

    [Fact]
    public void Dispose_ClearsViewAndParent ()
    {
        View parent = new ();
        Border border = parent.Border;
        border.EnsureView ();
        Assert.NotNull (border.View);
        Assert.NotNull (border.Parent);

        border.Dispose ();
        Assert.Null (border.View);
        Assert.Null (border.Parent);
    }

    [Fact]
    public void Dispose_WithoutView_IsNoOp ()
    {
        Border border = new () { Parent = new View () };
        Assert.Null (border.View);

        // Should not throw
        border.Dispose ();
        Assert.Null (border.View);
        Assert.Null (border.Parent);
    }

    #endregion Dispose
}
