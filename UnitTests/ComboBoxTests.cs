﻿using System;
using System.Linq;
using Terminal.Gui;
using Xunit;

namespace UnitTests {
    public class ComboBoxTests {
        [Fact]
        [AutoInitShutdown]
        public void EnsureKeyEventsDoNotCauseExceptions ()
        {
            var comboBox = new ComboBox ("0");

            var source = Enumerable.Range (0, 15).Select (x => x.ToString ()).ToArray ();
            comboBox.SetSource(source);

            Application.Top.Add(comboBox);

            foreach (var key in (Key [])Enum.GetValues (typeof(Key))) {
                comboBox.ProcessKey (new KeyEvent (key, new KeyModifiers ()));
            }
        }
    }
}