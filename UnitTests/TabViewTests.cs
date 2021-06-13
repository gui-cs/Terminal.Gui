using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;

namespace Terminal.Gui.Views {
  
	public class TabViewTests {
		private TabView GetTabView ()
		{
			return GetTabView (out _, out _);
		}

		private TabView GetTabView (out TabView.Tab tab1, out TabView.Tab tab2)
		{
			InitFakeDriver ();

			var tv = new TabView ();
			tv.AddTab (tab1 = new TabView.Tab ("Tab1", new TextField ("hi")), false);
			tv.AddTab (tab2 = new TabView.Tab ("Tab2", new Label ("hi2")), false);
			return tv;
		}

		[Fact]
		public void AddTwoTabs_SecondIsSelected ()
		{
			InitFakeDriver ();

			var tv = new TabView ();
			TabView.Tab tab1;
			TabView.Tab tab2;
			tv.AddTab (tab1 = new TabView.Tab ("Tab1", new TextField ("hi")), false);
			tv.AddTab (tab2 = new TabView.Tab ("Tab1", new Label ("hi2")), true);

			Assert.Equal (2, tv.Tabs.Count);
			Assert.Equal (tab2, tv.SelectedTab);

			Application.Shutdown ();
		}


		[Fact]
		public void EnsureSelectedTabVisible_NullSelect ()
		{
			var tv = GetTabView ();

			tv.SelectedTab = null;

			Assert.Null (tv.SelectedTab);
			Assert.Equal (0, tv.TabScrollOffset);

			tv.EnsureSelectedTabIsVisible ();

			Assert.Null (tv.SelectedTab);
			Assert.Equal (0, tv.TabScrollOffset);

			Application.Shutdown ();
		}

		[Fact]
		public void EnsureSelectedTabVisible_MustScroll ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			// Make tab width small to force only one tab visible at once
			tv.Width = 4;

			tv.SelectedTab = tab1;
			Assert.Equal (0, tv.TabScrollOffset);
			tv.EnsureSelectedTabIsVisible ();
			Assert.Equal (0, tv.TabScrollOffset);

			// Asking to show tab2 should automatically move scroll offset accordingly
			tv.SelectedTab = tab2;
			Assert.Equal (1, tv.TabScrollOffset);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void SelectedTabChanged_Called ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;

			TabView.Tab oldTab = null;
			TabView.Tab newTab = null;
			int called = 0;

			tv.SelectedTabChanged += (s, e) => {
				oldTab = e.OldTab;
				newTab = e.NewTab;
				called++;
			};

			tv.SelectedTab = tab2;

			Assert.Equal (1, called);
			Assert.Equal (tab1, oldTab);
			Assert.Equal (tab2, newTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}
		[Fact]
		public void RemoveTab_ChangesSelection ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;
			tv.RemoveTab (tab1);

			Assert.Equal (tab2, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void RemoveTab_MultipleCalls_NotAnError ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;

			// Repeated calls to remove a tab that is not part of
			// the collection should be ignored
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab1);

			Assert.Equal (tab2, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void RemoveAllTabs_ClearsSelection ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab2);

			Assert.Null (tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void SwitchTabBy_NormalUsage ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			TabView.Tab tab3;
			TabView.Tab tab4;
			TabView.Tab tab5;

			tv.AddTab (tab3 = new TabView.Tab (), false);
			tv.AddTab (tab4 = new TabView.Tab (), false);
			tv.AddTab (tab5 = new TabView.Tab (), false);

			tv.SelectedTab = tab1;

			int called = 0;
			tv.SelectedTabChanged += (s, e) => { called++; };

			tv.SwitchTabBy (1);

			Assert.Equal (1, called);
			Assert.Equal (tab2, tv.SelectedTab);

			//reset called counter
			called = 0;

			// go right 2
			tv.SwitchTabBy (2);

			// even though we go right 2 indexes the event should only be called once
			Assert.Equal (1, called);
			Assert.Equal (tab4, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void AddTab_SameTabMoreThanOnce ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			Assert.Equal (2, tv.Tabs.Count);

			// Tab is already part of the control so shouldn't result in duplication
			tv.AddTab (tab1, false);
			tv.AddTab (tab1, false);
			tv.AddTab (tab1, false);
			tv.AddTab (tab1, false);

			Assert.Equal (2, tv.Tabs.Count);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}



		[Fact]
		public void SwitchTabBy_OutOfTabsRange ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;
			tv.SwitchTabBy (500);

			Assert.Equal (tab2, tv.SelectedTab);

			tv.SwitchTabBy (-500);

			Assert.Equal (tab1, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();

		}

		private void InitFakeDriver ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });
		}
	}
}