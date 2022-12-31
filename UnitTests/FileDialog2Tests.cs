using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Terminal.Gui;
using Terminal.Gui.Views;
using Xunit;
using Xunit.Abstractions;


namespace Terminal.Gui.Core {
    public class FileDialog2Tests
    {

        [Fact, AutoInitShutdown]
        public void OnLoad_TextBoxIsFocused()
        {
            var dlg = new FileDialog2();
            dlg.BeginInit();
            dlg.EndInit();
            Application.Begin(dlg);

            // First focused is ContentView :(
            Assert.NotNull(dlg.Focused.Focused);
            Assert.IsType(typeof(FileDialog2.TextFieldWithAppendAutocomplete),dlg.Focused.Focused);
        }
    }
}