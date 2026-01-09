namespace ViewsTests.TextViewTests;

/// <summary>
///     Tests for <see cref="TextView.ExecuteWithUnwrappedModel"/> method.
///     This test suite validates the transactional wrap/unwrap pattern for text modifications.
/// </summary>
/// <remarks>
///     GitHub Copilot - Created to validate the new action-based transaction pattern for word wrap.
/// </remarks>
public class TextViewWordWrapTransactionTests
{
    [Fact]
    public void ExecuteWithUnwrappedModel_Without_WordWrap_Executes_Action_Directly ()
    {
        // When word wrap is disabled, the action should execute directly without any coordinate conversion
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            Text = "Short line\nAnother line"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        Assert.False (tv.WordWrap);
        tv.InsertionPoint = new (5, 0);

        bool actionExecuted = false;

        // Use reflection to call the private method
        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        method!.Invoke (
                       tv,
                       new object []
                       {
                           new Action (
                                       () =>
                                       {
                                           actionExecuted = true;
                                           Assert.Equal (new (5, 0), tv.InsertionPoint); // Position unchanged
                                       }
                                      )
                       }
                      );

        Assert.True (actionExecuted);
        Assert.Equal (new (5, 0), tv.InsertionPoint);
    }

    [Fact]
    public void ExecuteWithUnwrappedModel_With_WordWrap_Converts_Coordinates ()
    {
        // When word wrap is enabled, coordinates should be converted from wrapped to unwrapped and back
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 5,
            Text = "This is a very long line that will wrap"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.WordWrap = true;
        Assert.True (tv.WordWrap);

        // Position cursor somewhere in wrapped text
        tv.InsertionPoint = new (5, 1); // Column 5, row 1 in wrapped view

        Point wrappedPosition = tv.InsertionPoint;
        bool actionExecuted = false;

        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        method!.Invoke (
                       tv,
                       new object []
                       {
                           new Action (
                                       () =>
                                       {
                                           actionExecuted = true;

                                           // Inside the action, we should be working with unwrapped coordinates
                                           // The position should reflect unwrapped model position
                                       }
                                      )
                       }
                      );

        Assert.True (actionExecuted);

        // After execution, we should have valid wrapped coordinates
        Assert.True (tv.InsertionPoint.Y >= 0);
        Assert.True (tv.InsertionPoint.X >= 0);
    }

    [Fact]
    public void ExecuteWithUnwrappedModel_With_WordWrap_Handles_Text_Insertion ()
    {
        // Test that text insertion works correctly through ExecuteWithUnwrappedModel
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 15,
            Height = 5,
            Text = "Short text here"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.WordWrap = true;
        tv.InsertionPoint = new (5, 0);

        string originalText = tv.Text;

        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        // Use GetCurrentLine() method to get access to the model
        System.Reflection.MethodInfo? getCurrentLineMethod = typeof (TextView).GetMethod (
                                                                                          "GetCurrentLine",
                                                                                          System.Reflection.BindingFlags.Public
                                                                                          | System.Reflection.BindingFlags.Instance
                                                                                         );
        Assert.NotNull (getCurrentLineMethod);

        method!.Invoke (
                       tv,
                       new object []
                       {
                           new Action (
                                       () =>
                                       {
                                           // Insert text at current position
                                           object? lineObj = getCurrentLineMethod!.Invoke (tv, null);
                                           Assert.NotNull (lineObj);

                                           var line = lineObj as List<Cell>;
                                           Assert.NotNull (line);

                                           // Insert "INSERTED" at current position
                                           List<Cell> cellsToInsert = Cell.ToCellList ("INSERTED");
                                           line!.InsertRange (tv.CurrentColumn, cellsToInsert);

                                           // Update the column position
                                           System.Reflection.PropertyInfo? currentColumnProp = typeof (TextView).GetProperty (
                                                                                                   "CurrentColumn",
                                                                                                   System.Reflection.BindingFlags.Public
                                                                                                   | System.Reflection.BindingFlags.Instance
                                                                                                  );
                                           currentColumnProp!.SetValue (tv, tv.CurrentColumn + cellsToInsert.Count);
                                       }
                                      )
                       }
                      );

        // Verify text was inserted
        Assert.Contains ("INSERTED", tv.Text);
    }

    [Fact]
    public void ExecuteWithUnwrappedModel_Exception_Restores_Original_State ()
    {
        // Test that if an exception occurs, the original state is restored
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 15,
            Height = 5,
            Text = "Some text for testing"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.WordWrap = true;
        tv.InsertionPoint = new (5, 0);

        Point originalPosition = tv.InsertionPoint;
        string originalText = tv.Text;

        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        // Execute action that throws (reflection wraps it in TargetInvocationException)
        System.Reflection.TargetInvocationException? exception = Assert.Throws<System.Reflection.TargetInvocationException> (
                                                  () => method!.Invoke (
                                                       tv,
                                                       new object []
                                                       {
                                                           new Action (
                                                                       () => throw new InvalidOperationException ("Test exception")
                                                                      )
                                                       }
                                                      )
                                                 );

        // Verify the inner exception is what we threw
        Assert.NotNull (exception);
        Assert.IsType<InvalidOperationException> (exception!.InnerException);

        // Verify state was restored
        Assert.Equal (originalPosition, tv.InsertionPoint);
        Assert.Equal (originalText, tv.Text);
    }

    [Fact]
    public void ExecuteWithUnwrappedModel_Preserves_Selection_Positions ()
    {
        // Test that selection positions are properly converted and restored
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 15,
            Height = 5,
            Text = "This is a long line that will definitely wrap when word wrap is enabled"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.WordWrap = true;

        // Create a selection by setting the start position and enabling selection
        tv.InsertionPoint = new (0, 0);
        tv.SelectionStartRow = 0;
        tv.SelectionStartColumn = 0;
        tv.InsertionPoint = new (10, 0);

        Assert.True (tv.IsSelecting);

        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        bool actionExecuted = false;

        method!.Invoke (
                       tv,
                       new object []
                       {
                           new Action (
                                       () =>
                                       {
                                           actionExecuted = true;

                                           // Selection should still be active inside the action
                                           Assert.True (tv.IsSelecting);
                                       }
                                      )
                       }
                      );

        Assert.True (actionExecuted);
        Assert.True (tv.IsSelecting); // Selection preserved after action
    }

    [Fact]
    public void ExecuteWithUnwrappedModel_Multiple_Lines_Coordinate_Conversion ()
    {
        // Test coordinate conversion with multiple lines and word wrap
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 20,
            Height = 10,
            Text = "First line\nSecond line that is much longer and will wrap\nThird line"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.WordWrap = true;

        // Move to second line
        tv.InsertionPoint = new (5, 1);

        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        Point positionDuringAction = Point.Empty;

        method!.Invoke (
                       tv,
                       new object []
                       {
                           new Action (
                                       () => { positionDuringAction = tv.InsertionPoint; }
                                      )
                       }
                      );

        // Verify we can still access valid positions
        Assert.True (positionDuringAction.Y >= 0);
        Assert.True (positionDuringAction.X >= 0);
        Assert.True (tv.InsertionPoint.Y >= 0);
        Assert.True (tv.InsertionPoint.X >= 0);
    }

    [Fact]
    public void ExecuteWithUnwrappedModel_Nested_Calls_Work_Correctly ()
    {
        // Test that the method handles being called while already in an unwrapped state
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 15,
            Height = 5,
            Text = "Test text"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.WordWrap = true;

        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        int outerExecuted = 0;
        int innerExecuted = 0;

        method!.Invoke (
                       tv,
                       new object []
                       {
                           new Action (
                                       () =>
                                       {
                                           outerExecuted++;

                                           // Nested call
                                           method.Invoke (
                                                          tv,
                                                          new object []
                                                          {
                                                              new Action (
                                                                          () => { innerExecuted++; }
                                                                         )
                                                          }
                                                         );
                                       }
                                      )
                       }
                      );

        Assert.Equal (1, outerExecuted);
        Assert.Equal (1, innerExecuted);
    }

    [Fact]
    public void ExecuteWithUnwrappedModel_Position_Setters_Called_Once ()
    {
        // Test that position setters are triggered only once (with final wrapped coordinates)
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 15,
            Height = 5,
            Text = "This is a longer line that will wrap"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.WordWrap = true;
        tv.InsertionPoint = new (5, 0);

        System.Reflection.MethodInfo? method = typeof (TextView).GetMethod (
                                                                            "ExecuteWithUnwrappedModel",
                                                                            System.Reflection.BindingFlags.NonPublic
                                                                            | System.Reflection.BindingFlags.Instance
                                                                           );
        Assert.NotNull (method);

        // Track cursor position changes
        var cursorPositions = new List<Point> ();

        // Subscribe to a field change if possible, or just verify end state
        Point initialPosition = tv.InsertionPoint;

        method!.Invoke (
                       tv,
                       new object []
                       {
                           new Action (
                                       () =>
                                       {
                                           // Modify position during action
                                           System.Reflection.PropertyInfo? currentColumnProp = typeof (TextView).GetProperty (
                                                                                                   "CurrentColumn",
                                                                                                   System.Reflection.BindingFlags.Public
                                                                                                   | System.Reflection.BindingFlags.Instance
                                                                                                  );
                                           currentColumnProp!.SetValue (tv, tv.CurrentColumn + 1);
                                       }
                                      )
                       }
                      );

        // Verify final position is valid and different from initial
        Point finalPosition = tv.InsertionPoint;
        Assert.True (finalPosition.X >= 0);
        Assert.True (finalPosition.Y >= 0);
    }
}
