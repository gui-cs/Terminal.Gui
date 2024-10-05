using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("AnsiEscapeSequenceRequest", "Ansi Escape Sequence Request")]
[ScenarioCategory ("Ansi Escape Sequence")]
public sealed class AnsiEscapeSequenceRequests : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };
        appWindow.Padding.Thickness = new (1);

        var scrRequests = new List<string>
        {
            "CSI_SendDeviceAttributes",
            "CSI_ReportTerminalSizeInChars",
            "CSI_RequestCursorPositionReport",
            "CSI_SendDeviceAttributes2"
        };

        var cbRequests = new ComboBox () { Width = 40, Height = 5, ReadOnly = true, Source = new ListWrapper<string> (new (scrRequests)) };
        appWindow.Add (cbRequests);

        var label = new Label { Y = Pos.Bottom (cbRequests) + 1, Text = "Request:" };
        var tfRequest = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 20 };
        appWindow.Add (label, tfRequest);

        label = new Label { X = Pos.Right (tfRequest) + 1, Y = Pos.Top (tfRequest) - 1, Text = "Value:" };
        var tfValue = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 6 };
        appWindow.Add (label, tfValue);

        label = new Label { X = Pos.Right (tfValue) + 1, Y = Pos.Top (tfValue) - 1, Text = "Terminator:" };
        var tfTerminator = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 4 };
        appWindow.Add (label, tfTerminator);

        cbRequests.SelectedItemChanged += (s, e) =>
                                          {
                                              var selAnsiEscapeSequenceRequestName = scrRequests [cbRequests.SelectedItem];
                                              AnsiEscapeSequenceRequest selAnsiEscapeSequenceRequest = null;
                                              switch (selAnsiEscapeSequenceRequestName)
                                              {
                                                  case "CSI_SendDeviceAttributes":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_SendDeviceAttributes;
                                                      break;
                                                  case "CSI_ReportTerminalSizeInChars":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_ReportTerminalSizeInChars;
                                                      break;
                                                  case "CSI_RequestCursorPositionReport":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_RequestCursorPositionReport;
                                                      break;
                                                  case "CSI_SendDeviceAttributes2":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_SendDeviceAttributes2;
                                                      break;
                                              }

                                              tfRequest.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Request : "";
                                              tfValue.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Value ?? "" : "";
                                              tfTerminator.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Terminator : "";
                                          };
        // Forces raise cbRequests.SelectedItemChanged to update TextFields
        cbRequests.SelectedItem = 0;

        label = new Label { Y = Pos.Bottom (tfRequest) + 2, Text = "Response:" };
        var tvResponse = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 40, Height = 4, ReadOnly = true };
        appWindow.Add (label, tvResponse);

        label = new Label { X = Pos.Right (tvResponse) + 1, Y = Pos.Top (tvResponse) - 1, Text = "Error:" };
        var tvError = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 40, Height = 4, ReadOnly = true };
        appWindow.Add (label, tvError);

        label = new Label { X = Pos.Right (tvError) + 1, Y = Pos.Top (tvError) - 1, Text = "Value:" };
        var tvValue = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 6, Height = 4, ReadOnly = true };
        appWindow.Add (label, tvValue);

        label = new Label { X = Pos.Right (tvValue) + 1, Y = Pos.Top (tvValue) - 1, Text = "Terminator:" };
        var tvTerminator = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 4, Height = 4, ReadOnly = true };
        appWindow.Add (label, tvTerminator);

        var btnResponse = new Button { X = Pos.Center (), Y = Pos.Bottom (tvResponse) + 2, Text = "Send Request" };

        var lblSuccess = new Label { X = Pos.Center (), Y = Pos.Bottom (btnResponse) + 1 };
        appWindow.Add (lblSuccess);

        btnResponse.Accept += (s, e) =>
                              {
                                  var ansiEscapeSequenceRequest = new AnsiEscapeSequenceRequest
                                  {
                                      Request = tfRequest.Text,
                                      Terminator = tfTerminator.Text,
                                      Value = string.IsNullOrEmpty (tfValue.Text) ? null : tfValue.Text
                                  };

                                  var success = AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (
                                       ansiEscapeSequenceRequest,
                                       out AnsiEscapeSequenceResponse ansiEscapeSequenceResponse
                                      );

                                  tvResponse.Text = ansiEscapeSequenceResponse.Response;
                                  tvError.Text = ansiEscapeSequenceResponse.Error;
                                  tvValue.Text = ansiEscapeSequenceResponse.Value ?? "";
                                  tvTerminator.Text = ansiEscapeSequenceResponse.Terminator;

                                  if (success)
                                  {
                                      lblSuccess.ColorScheme = Colors.ColorSchemes ["Base"];
                                      lblSuccess.Text = "Successful";
                                  }
                                  else
                                  {
                                      lblSuccess.ColorScheme = Colors.ColorSchemes ["Error"];
                                      lblSuccess.Text = "Error";
                                  }
                              };
        appWindow.Add (btnResponse);

        appWindow.Add (new Label { Y = Pos.Bottom (lblSuccess) + 2, Text = "You can send other requests by editing the TextFields." });

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
