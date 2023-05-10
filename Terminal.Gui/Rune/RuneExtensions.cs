using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	public static class RuneExtensions {
		public static Rune MaxRune = new Rune (0x10FFFF);

		public static int ColumnWidth (this Rune rune)
		{
			return RuneUtilities.ColumnWidth (rune);
		}

		public static bool IsNonSpacingChar (this Rune rune)
		{
			return RuneUtilities.IsNonSpacingChar (rune.Value);
		}

		public static bool IsWideChar (this Rune rune)
		{
			return RuneUtilities.IsWideChar (rune.Value);
		}

		public static int RuneUnicodeLength (this Rune rune, Encoding encoding = null)
		{
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			var bytes = encoding.GetBytes (rune.ToString ().ToCharArray ());
			var offset = 0;
			if (bytes [bytes.Length - 1] == 0) {
				offset++;
			}
			return bytes.Length - offset;
		}

		public static int EncodeRune (this Rune rune, byte [] dest, int start = 0, int nbytes = -1)
		{
			var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
			int length = 0;
			for (int i = 0; i < (nbytes == -1 ? bytes.Length : nbytes); i++) {
				if (bytes [i] == 0) {
					break;
				}
				dest [start + i] = bytes [i];
				length++;
			}
			return length;
		}

		public static (Rune Rune, int Size) DecodeRune (byte [] buffer, int start = 0, int nbytes = -1)
		{
			var operationStatus = Rune.DecodeFromUtf8 (buffer, out Rune rune, out int bytesConsumed);
			return (rune, bytesConsumed);
		}

		public static (Rune Rune, int Size) DecodeLastRune (byte [] buffer, int end = -1)
		{
			var operationStatus = Rune.DecodeLastFromUtf8 (buffer, out Rune rune, out int bytesConsumed);
			if (operationStatus == System.Buffers.OperationStatus.Done) {
				return (rune, bytesConsumed);
			} else {
				return (default, 0);
			}
		}

		public static bool DecodeSurrogatePair (this Rune rune, out char [] spair)
		{
			if (rune.IsSurrogatePair ()) {
				spair = rune.ToString ().ToCharArray ();
				return true;
			}
			spair = null;
			return false;
		}

		public static bool EncodeSurrogatePair (char highsurrogate, char lowSurrogate, out Rune result)
		{
			result = default;
			if (char.IsSurrogatePair (highsurrogate, lowSurrogate)) {
				result = (Rune)char.ConvertToUtf32 (highsurrogate, lowSurrogate);
				return true;
			}
			return false;
		}

		public static bool IsSurrogatePair (this Rune rune)
		{
			return char.IsSurrogatePair (rune.ToString (), 0);
		}

		public static bool IsValid (byte [] buffer)
		{
			var str = Encoding.Unicode.GetString (buffer);
			foreach (var rune in str.EnumerateRunes ()) {
				if (rune == Rune.ReplacementChar) {
					return false;
				}
			}
			return true;
		}

		public static bool IsValid (this Rune rune)
		{
			return Rune.IsValid (rune.Value);
		}
	}
}
