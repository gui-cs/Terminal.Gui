using System.Threading;
using Xunit;

namespace Terminal.Gui.Core {
	public class CollectionNavigatorTests {
		static string [] simpleStrings = new string []{
		    "appricot", // 0
		    "arm",      // 1
		    "bat",      // 2
		    "batman",   // 3
		    "candle"    // 4
		  };

		[Fact]
		public void ShouldAcceptNegativeOne ()
		{
			var n = new CollectionNavigator (simpleStrings);

			// Expect that index of -1 (i.e. no selection) should work correctly
			// and select the first entry of the letter 'b'
			Assert.Equal (2, n.GetNextMatchingItem (-1, 'b'));
		}
		[Fact]
		public void OutOfBoundsShouldBeIgnored ()
		{
			var n = new CollectionNavigator (simpleStrings);

			// Expect saying that index 500 is the current selection should not cause
			// error and just be ignored (treated as no selection)
			Assert.Equal (2, n.GetNextMatchingItem (500, 'b'));
		}

		[Fact]
		public void Cycling ()
		{
			var n = new CollectionNavigator (simpleStrings);
			Assert.Equal (2, n.GetNextMatchingItem (0, 'b'));
			Assert.Equal (3, n.GetNextMatchingItem (2, 'b'));

			// if 4 (candle) is selected it should loop back to bat
			Assert.Equal (2, n.GetNextMatchingItem (4, 'b'));
		}


		[Fact]
		public void ToSearchText ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "bat",
			    "batman",
			    "bbfish",
			    "candle"
			  };

			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (2, current = n.GetNextMatchingItem (current, 'b')); // match bat
			Assert.Equal (4, current = n.GetNextMatchingItem (current, 'b')); // match bbfish

			// another 'b' means searching for "bbb" which does not exist
			// so we go back to looking for "b" as a fresh key strike
			Assert.Equal (2, current = n.GetNextMatchingItem (current, 'b')); // match bat
		}

		[Fact]
		public void FullText ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "ta",
			    "target",
			    "text",
			    "egg",
			    "candle"
			  };

			var n = new CollectionNavigator (strings);
			Assert.Equal (2, n.GetNextMatchingItem (0, 't'));

			// should match "te" in "text"
			Assert.Equal (4, n.GetNextMatchingItem (2, 'e'));

			// still matches text
			Assert.Equal (4, n.GetNextMatchingItem (4, 'x'));

			// nothing starts texa so it jumps to a for appricot
			Assert.Equal (0, n.GetNextMatchingItem (4, 'a'));
		}

		[Fact]
		public void Unicode ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "ta",
			    "丗丙业丞",
			    "丗丙丛",
			    "text",
			    "egg",
			    "candle"
			  };

			var n = new CollectionNavigator (strings);
			Assert.Equal (3, n.GetNextMatchingItem (0, '丗'));

			// 丗丙业丞 is as good a match as 丗丙丛
			// so when doing multi character searches we should
			// prefer to stay on the same index unless we invalidate
			// our typed text
			Assert.Equal (3, n.GetNextMatchingItem (3, '丙'));

			// No longer matches 丗丙业丞 and now only matches 丗丙丛
			// so we should move to the new match
			Assert.Equal (4, n.GetNextMatchingItem (3, '丛'));

			// nothing starts "丗丙丛a" so it jumps to a for appricot
			Assert.Equal (0, n.GetNextMatchingItem (4, 'a'));
		}

		[Fact]
		public void AtSymbol ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "ta",
			    "@bob",
			    "@bb",
			    "text",
			    "egg",
			    "candle"
			  };

			var n = new CollectionNavigator (strings);
			Assert.Equal (3, n.GetNextMatchingItem (0, '@'));
			Assert.Equal (3, n.GetNextMatchingItem (3, 'b'));
			Assert.Equal (4, n.GetNextMatchingItem (3, 'b'));
		}

		[Fact]
		public void Word ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "bat",
			    "batman",
			    "bates hotel",
			    "candle"
			  };
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 'b')); // match bat
			Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 'a')); // match bat
			Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 't')); // match bat
			Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, 'e')); // match bates hotel
			Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, 's')); // match bates hotel
			Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, ' ')); // match bates hotel

			// another 'b' means searching for "bates b" which does not exist
			// so we go back to looking for "b" as a fresh key strike
			Assert.Equal (strings.IndexOf<string> ("bat"), current = n.GetNextMatchingItem (current, 'b')); // match bat
		}

		[Fact]
		public void Symbols ()
		{
			var strings = new string []{
			    "$$",
			    "$100.00",
			    "$101.00",
			    "$101.10",
			    "$200.00",
			    "appricot"
			  };
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
			Assert.Equal ("a", n.SearchString);

			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);

			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, '1'));
			Assert.Equal ("$1", n.SearchString);

			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, '0'));
			Assert.Equal ("$10", n.SearchString);

			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, '1'));
			Assert.Equal ("$101", n.SearchString);

			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, '.'));
			Assert.Equal ("$101.", n.SearchString);

			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
			Assert.Equal ("a", n.SearchString);

			// another '$' means searching for "$" again
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);

			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$$", n.SearchString);

		}

		[Fact]
		public void Delay ()
		{
			var strings = new string []{
			    "$$",
			    "$100.00",
			    "$101.00",
			    "$101.10",
			    "$200.00",
			    "appricot"
			  };
			int current = 0;
			var n = new CollectionNavigator (strings);

			// No delay
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
			Assert.Equal ("a", n.SearchString);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$$", n.SearchString);

			// Delay 
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
			Assert.Equal ("a", n.SearchString);

			Thread.Sleep (n.TypingDelay + 10);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);

			Thread.Sleep (n.TypingDelay + 10);
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);

			Thread.Sleep (n.TypingDelay + 10);
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);

			Thread.Sleep (n.TypingDelay + 10);
			Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);

			Thread.Sleep (n.TypingDelay + 10);
			Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, '2')); // Shouldn't move
			Assert.Equal ("2", n.SearchString);
		}

		[Fact]
		public void MinimizeMovement_False_ShouldMoveIfMultipleMatches ()
		{
			var strings = new string [] {
				"$$",
				"$100.00",
				"$101.00",
				"$101.10",
				"$200.00",
				"appricot",
				"c",
				"car",
				"cart",
			};
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", false));
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", false)); // back to top
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$", false));

			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$", false)); // back to top
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, "a", false));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$", false)); // back to top

			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$100.00", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2", false));

			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$200.00", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2", false));

			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2", false));

			Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", false));
			Assert.Equal (strings.IndexOf ("cart"), current = n.GetNextMatchingItem (current, "car", false));

			Assert.Equal (-1, current = n.GetNextMatchingItem (current, "x", false));
		}

		[Fact]
		public void  MinimizeMovement_True_ShouldStayOnCurrentIfMultipleMatches ()
		{
			var strings = new string [] {
				"$$",
				"$100.00",
				"$101.00",
				"$101.10",
				"$200.00",
				"appricot",
				"c",
				"car",
				"cart",
			};
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", true));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$", true));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", true)); // back to top
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$1", true));
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", true));
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", true));

			Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", true));
			Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", true));

			Assert.Equal (-1, current = n.GetNextMatchingItem (current, "x", true));
		}
	}
}
