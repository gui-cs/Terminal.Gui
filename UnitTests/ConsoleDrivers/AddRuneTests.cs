using System.Buffers;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using static Terminal.Gui.SpinnerStyle;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.DriverTests;
public class AddRuneTests {
	readonly ITestOutputHelper _output;

	public AddRuneTests (ITestOutputHelper output)
	{
		this._output = output;
	}

	[Fact]
	public void AddRune ()
	{

		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		driver.AddRune (new Rune ('a'));
		Assert.Equal ((Rune)'a', driver.Contents[0,0].Runes[0]);
		
		driver.End ();
		Application.Shutdown ();
	}

	[Fact]
	public void AddRune_InvalidLocation_DoesNothing ()
	{
		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		driver.Move (driver.Cols, driver.Rows);
		driver.AddRune ('a');
		
		for (var col = 0; col < driver.Cols; col++) {
			for (var row = 0; row < driver.Rows; row++) {
				Assert.Equal ((Rune)' ', driver.Contents [row, col].Runes [0]);
			}
		}

		driver.End ();
		Application.Shutdown ();
	}

	[Fact]
	public void AddRune_MovesToNextColumn ()
	{
		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		driver.AddRune ('a');
		Assert.Equal ((Rune)'a', driver.Contents [0, 0].Runes [0]);
		Assert.Equal (0, driver.Row);
		Assert.Equal (1, driver.Col);

		driver.AddRune ('b');
		Assert.Equal ((Rune)'b', driver.Contents [0, 1].Runes [0]);
		Assert.Equal (0, driver.Row);
		Assert.Equal (2, driver.Col);

		// Move to the last column of the first row
		var lastCol = driver.Cols - 1;
		driver.Move (lastCol, 0);
		Assert.Equal (0, driver.Row);
		Assert.Equal (lastCol, driver.Col);

		// Add a rune to the last column of the first row; should increment the row or col even though it's now invalid
		driver.AddRune ('c');
		Assert.Equal ((Rune)'c', driver.Contents [0, lastCol].Runes [0]);
		Assert.Equal (lastCol + 1, driver.Col);

		// Add a rune; should succeed but do nothing as it's outside of Contents
		driver.AddRune ('d');
		Assert.Equal (lastCol + 2, driver.Col);
		for (var col = 0; col < driver.Cols; col++) {
			for (var row = 0; row < driver.Rows; row++) {
				Assert.NotEqual ((Rune)'d', driver.Contents [row, col].Runes [0]);
			}
		}

		driver.End ();
		Application.Shutdown ();
	}

	[Fact]
	public void AddRune_MovesToNextColumn_Wide ()
	{
		var driver = new FakeDriver ();
		Application.Init (driver);
		driver.Init (() => { });

		// 🍕 Slice of Pizza "\U0001F355"
		var operationStatus = Rune.DecodeFromUtf16 ("\U0001F355", out Rune rune, out int charsConsumed);
		Assert.Equal (OperationStatus.Done, operationStatus);
		Assert.Equal (charsConsumed, rune.Utf16SequenceLength);
		Assert.Equal (2, rune.GetColumns ());
		
		driver.AddRune (rune);
		Assert.Equal (rune, driver.Contents [0, 0].Runes [0]);
		Assert.Equal (0, driver.Row);
		Assert.Equal (2, driver.Col);

		//driver.AddRune ('b');
		//Assert.Equal ((Rune)'b', driver.Contents [0, 1].Runes [0]);
		//Assert.Equal (0, driver.Row);
		//Assert.Equal (2, driver.Col);

		//// Move to the last column of the first row
		//var lastCol = driver.Cols - 1;
		//driver.Move (lastCol, 0);
		//Assert.Equal (0, driver.Row);
		//Assert.Equal (lastCol, driver.Col);

		//// Add a rune to the last column of the first row; should increment the row or col even though it's now invalid
		//driver.AddRune ('c');
		//Assert.Equal ((Rune)'c', driver.Contents [0, lastCol].Runes [0]);
		//Assert.Equal (lastCol + 1, driver.Col);

		//// Add a rune; should succeed but do nothing as it's outside of Contents
		//driver.AddRune ('d');
		//Assert.Equal (lastCol + 2, driver.Col);
		//for (var col = 0; col < driver.Cols; col++) {
		//	for (var row = 0; row < driver.Rows; row++) {
		//		Assert.NotEqual ((Rune)'d', driver.Contents [row, col].Runes [0]);
		//	}
		//}

		driver.End ();
		Application.Shutdown ();
	}
}
