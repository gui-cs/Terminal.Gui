using static Terminal.Gui.ConsoleDrivers.ConsoleKeyMapping;

namespace Terminal.Gui.ConsoleDrivers;

public class ConsoleKeyMappingTests
{
#if ENABLE_VK_PACKET_NON_WINDOWS
    // This test (and the GetConsoleKeyInfoFromKeyCode API) are bogus. They make no sense outside of
    // the context of Windows and knowing they keyboard layout. They should be removed.
    [Theory]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, ConsoleKey.A, KeyCode.A, 'A')]
    [InlineData ((KeyCode)'a', ConsoleKey.A, (KeyCode)'a', 'a')]
    [InlineData ((KeyCode)'À' | KeyCode.ShiftMask, ConsoleKey.A, (KeyCode)'À', 'À')]
    [InlineData ((KeyCode)'à', ConsoleKey.A, (KeyCode)'à', 'à')]
    [InlineData ((KeyCode)'Ü' | KeyCode.ShiftMask, ConsoleKey.U, (KeyCode)'Ü', 'Ü')]
    [InlineData ((KeyCode)'ü', ConsoleKey.U, (KeyCode)'ü', 'ü')]
    [InlineData ((KeyCode)'Ý' | KeyCode.ShiftMask, ConsoleKey.Y, (KeyCode)'Ý', 'Ý')]
    [InlineData ((KeyCode)'ý', ConsoleKey.Y, (KeyCode)'ý', 'ý')]
    [InlineData ((KeyCode)'!' | KeyCode.ShiftMask, ConsoleKey.D1, (KeyCode)'!', '!')]
    [InlineData (KeyCode.D1 | KeyCode.ShiftMask, ConsoleKey.D1, (KeyCode)'!', '!')]
    [InlineData (KeyCode.D1, ConsoleKey.D1, KeyCode.D1, '1')]
    [InlineData (
                    (KeyCode)'/' | KeyCode.ShiftMask,
                    ConsoleKey.D7,
                    (KeyCode)'/',
                    '/'
                )] // BUGBUG: This is incorrect for ENG keyboards. Shift-7 should be &.
    [InlineData (KeyCode.D7 | KeyCode.ShiftMask, ConsoleKey.D7, (KeyCode)'/', '/')]
    [InlineData (KeyCode.D7, ConsoleKey.D7, KeyCode.D7, '7')]
    [InlineData ((KeyCode)'{' | KeyCode.AltMask | KeyCode.CtrlMask, ConsoleKey.D7, (KeyCode)'{', '{')]
    [InlineData ((KeyCode)'?' | KeyCode.ShiftMask, ConsoleKey.Oem4, (KeyCode)'?', '?')]
    [InlineData ((KeyCode)'\'', ConsoleKey.Oem4, (KeyCode)'\'', '\'')]
    [InlineData (KeyCode.PageDown | KeyCode.ShiftMask, ConsoleKey.PageDown, KeyCode.Null, '\0')]
    [InlineData (KeyCode.PageDown, ConsoleKey.PageDown, KeyCode.Null, '\0')]
    [InlineData ((KeyCode)'q', ConsoleKey.Q, (KeyCode)'q', 'q')]
    [InlineData (KeyCode.F2, ConsoleKey.F2, KeyCode.Null, '\0')]
    [InlineData ((KeyCode)'英', ConsoleKey.None, (KeyCode)'英', '英')]
    public void GetConsoleKeyInfoFromKeyCode_Tests (
        KeyCode keyCode,
        ConsoleKey expectedConsoleKey,
        KeyCode expectedKeyCode,
        char expectedKeyChar
    )
    {
        var consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (keyCode);
        Assert.Equal (consoleKeyInfo.Key, expectedConsoleKey);
        Assert.Equal ((char)expectedKeyCode, expectedKeyChar);
        Assert.Equal (consoleKeyInfo.KeyChar, expectedKeyChar);
    }

    static object packetLock = new object ();

    /// <summary>
    /// Sometimes when using remote tools EventKeyRecord sends 'virtual keystrokes'.
    /// These are indicated with the wVirtualKeyCode of 231 (VK_PACKET). When we see this code
    /// then we need to look to the unicode character (UnicodeChar) instead of the key
    /// when telling the rest of the framework what button was pressed. For full details
    /// see: https://github.com/gui-cs/Terminal.Gui/issues/2008
    /// </summary>
    [Theory]
    [AutoInitShutdown]
    [MemberData (nameof (VKPacket))]
    public void TestVKPacket (
        uint unicodeCharacter,
        bool shift,
        bool alt,
        bool control,
        uint initialVirtualKey,
        uint initialScanCode,
        KeyCode expectedRemapping,
        uint expectedVirtualKey,
        uint expectedScanCode
    )
    {
        lock (packetLock)
        {
            Application._forceFakeConsole = true;
            Application.Init ();

            ConsoleKeyInfo originalConsoleKeyInfo =
                new ConsoleKeyInfo ((char)unicodeCharacter, (ConsoleKey)initialVirtualKey, shift, alt, control);
            var encodedChar = ConsoleKeyMapping.EncodeKeyCharForVKPacket (originalConsoleKeyInfo);

            ConsoleKeyInfo packetConsoleKeyInfo =
                new ConsoleKeyInfo (encodedChar, ConsoleKey.Packet, shift, alt, control);
            ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.DecodeVKPacketToKConsoleKeyInfo (packetConsoleKeyInfo);

            Assert.Equal (originalConsoleKeyInfo, consoleKeyInfo);

            var modifiers = ConsoleKeyMapping.GetModifiers (shift, alt, control);
            var scanCode = ConsoleKeyMapping.GetScanCodeFromConsoleKeyInfo (consoleKeyInfo);

            Assert.Equal ((uint)consoleKeyInfo.Key, initialVirtualKey);

            if (scanCode > 0 && consoleKeyInfo.KeyChar == 0)
            {
                Assert.Equal (0, (double)consoleKeyInfo.KeyChar);
            }
            else
            {
                Assert.Equal (consoleKeyInfo.KeyChar, unicodeCharacter);
            }

            Assert.Equal ((uint)consoleKeyInfo.Key, expectedVirtualKey);
            Assert.Equal (scanCode, initialScanCode);
            Assert.Equal (scanCode, expectedScanCode);

            var top = Application.Top;

            top.KeyDown += (s, e) =>
                           {
                               Assert.Equal (Key.ToString (expectedRemapping), Key.ToString (e.KeyCode));
                               e.Handled = true;
                               Application.RequestStop ();
                           };

            int iterations = -1;

            Application.Iteration += (s, a) =>
                                     {
                                         iterations++;

                                         if (iterations == 0)
                                         {
                                             var keyChar = ConsoleKeyMapping.EncodeKeyCharForVKPacket (consoleKeyInfo);
                                             Application.Driver?.SendKeys (keyChar, ConsoleKey.Packet, shift, alt, control);
                                         }
                                     };
            Application.Run ();
            Application.Shutdown ();
        }
    }

    public static IEnumerable<object []> VKPacket ()
    {
        lock (packetLock)
        {
            // unicodeCharacter, shift, alt, control, initialVirtualKey, initialScanCode, expectedRemapping, expectedVirtualKey, expectedScanCode
            yield return new object [] { 'a', false, false, false, 'A', 30, KeyCode.A, 'A', 30 };
            yield return new object [] { 'A', true, false, false, 'A', 30, KeyCode.A | KeyCode.ShiftMask, 'A', 30 };
            yield return new object [] { 'A', true, true, false, 'A', 30, KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask, 'A', 30 };
            yield return new object [] { 'A', true, true, true, 'A', 30, KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 'A', 30 };
            yield return new object [] { 'z', false, false, false, 'Z', 44, KeyCode.Z, 'Z', 44 };
            yield return new object [] { 'Z', true, false, false, 'Z', 44, KeyCode.Z | KeyCode.ShiftMask, 'Z', 44 };
            yield return new object [] { 'Z', true, true, false, 'Z', 44, KeyCode.Z | KeyCode.ShiftMask | KeyCode.AltMask, 'Z', 44 };
            yield return new object [] { 'Z', true, true, true, 'Z', 44, KeyCode.Z | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 'Z', 44 };
            yield return new object [] { '英', false, false, false, '\0', 0, (KeyCode)'英', '\0', 0 };
            yield return new object [] { '英', true, false, false, '\0', 0, (KeyCode)'英' | KeyCode.ShiftMask, '\0', 0 };
            yield return new object [] { '英', true, true, false, '\0', 0, (KeyCode)'英' | KeyCode.ShiftMask | KeyCode.AltMask, '\0', 0 };
            yield return new object [] { '英', true, true, true, '\0', 0, (KeyCode)'英' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '\0', 0 };
            yield return new object [] { '+', false, false, false, VK.OEM_PLUS, 26, (KeyCode)'+', VK.OEM_PLUS, 26 };
            yield return new object [] { '*', true, false, false, VK.OEM_PLUS, 26, (KeyCode)'*' | KeyCode.ShiftMask, VK.OEM_PLUS, 26 };
            yield return new object [] { '+', true, true, false, VK.OEM_PLUS, 26, (KeyCode)'+' | KeyCode.ShiftMask | KeyCode.AltMask, VK.OEM_PLUS, 26 };
            yield return new object [] { '+', true, true, true, VK.OEM_PLUS, 26, (KeyCode)'+' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, VK.OEM_PLUS, 26 };
            yield return new object [] { '1', false, false, false, '1', 2, KeyCode.D1, '1', 2 };
            yield return new object [] { '!', true, false, false, '1', 2, (KeyCode)'!' | KeyCode.ShiftMask, '1', 2 };
            yield return new object [] { '1', true, true, false, '1', 2, KeyCode.D1 | KeyCode.ShiftMask | KeyCode.AltMask, '1', 2 };
            yield return new object [] { '1', true, true, true, '1', 2, KeyCode.D1 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '1', 2 };
            yield return new object [] { '1', false, true, true, '1', 2, KeyCode.D1 | KeyCode.AltMask | KeyCode.CtrlMask, '1', 2 };
            yield return new object [] { '2', false, false, false, '2', 3, KeyCode.D2, '2', 3 };
            yield return new object [] { '"', true, false, false, '2', 3, (KeyCode)'"' | KeyCode.ShiftMask, '2', 3 };
            yield return new object [] { '2', true, true, false, '2', 3, KeyCode.D2 | KeyCode.ShiftMask | KeyCode.AltMask, '2', 3 };
            yield return new object [] { '2', true, true, true, '2', 3, KeyCode.D2 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '2', 3 };
            yield return new object [] { '@', false, true, true, '2', 3, (KeyCode)'@' | KeyCode.AltMask | KeyCode.CtrlMask, '2', 3 };
            yield return new object [] { '3', false, false, false, '3', 4, KeyCode.D3, '3', 4 };
            yield return new object [] { '#', true, false, false, '3', 4, (KeyCode)'#' | KeyCode.ShiftMask, '3', 4 };
            yield return new object [] { '3', true, true, false, '3', 4, KeyCode.D3 | KeyCode.ShiftMask | KeyCode.AltMask, '3', 4 };
            yield return new object [] { '3', true, true, true, '3', 4, KeyCode.D3 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '3', 4 };
            yield return new object [] { '£', false, true, true, '3', 4, (KeyCode)'£' | KeyCode.AltMask | KeyCode.CtrlMask, '3', 4 };
            yield return new object [] { '4', false, false, false, '4', 5, KeyCode.D4, '4', 5 };
            yield return new object [] { '$', true, false, false, '4', 5, (KeyCode)'$' | KeyCode.ShiftMask, '4', 5 };
            yield return new object [] { '4', true, true, false, '4', 5, KeyCode.D4 | KeyCode.ShiftMask | KeyCode.AltMask, '4', 5 };
            yield return new object [] { '4', true, true, true, '4', 5, KeyCode.D4 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '4', 5 };
            yield return new object [] { '§', false, true, true, '4', 5, (KeyCode)'§' | KeyCode.AltMask | KeyCode.CtrlMask, '4', 5 };
            yield return new object [] { '5', false, false, false, '5', 6, KeyCode.D5, '5', 6 };
            yield return new object [] { '%', true, false, false, '5', 6, (KeyCode)'%' | KeyCode.ShiftMask, '5', 6 };
            yield return new object [] { '5', true, true, false, '5', 6, KeyCode.D5 | KeyCode.ShiftMask | KeyCode.AltMask, '5', 6 };
            yield return new object [] { '5', true, true, true, '5', 6, KeyCode.D5 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '5', 6 };
            yield return new object [] { '€', false, true, true, '5', 6, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask, '5', 6 };
            yield return new object [] { '6', false, false, false, '6', 7, KeyCode.D6, '6', 7 };
            yield return new object [] { '&', true, false, false, '6', 7, (KeyCode)'&' | KeyCode.ShiftMask, '6', 7 };
            yield return new object [] { '6', true, true, false, '6', 7, KeyCode.D6 | KeyCode.ShiftMask | KeyCode.AltMask, '6', 7 };
            yield return new object [] { '6', true, true, true, '6', 7, KeyCode.D6 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '6', 7 };
            yield return new object [] { '6', false, true, true, '6', 7, KeyCode.D6 | KeyCode.AltMask | KeyCode.CtrlMask, '6', 7 };
            yield return new object [] { '7', false, false, false, '7', 8, KeyCode.D7, '7', 8 };

            yield return
                new object [] { '/', true, false, false, '7', 8, (KeyCode)'/' | KeyCode.ShiftMask, '7', 8 }; // BUGBUG: This is not true for ENG keyboards. Shift-7 is &.
            yield return new object [] { '7', true, true, false, '7', 8, KeyCode.D7 | KeyCode.ShiftMask | KeyCode.AltMask, '7', 8 };
            yield return new object [] { '7', true, true, true, '7', 8, KeyCode.D7 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '7', 8 };
            yield return new object [] { '{', false, true, true, '7', 8, (KeyCode)'{' | KeyCode.AltMask | KeyCode.CtrlMask, '7', 8 };
            yield return new object [] { '8', false, false, false, '8', 9, KeyCode.D8, '8', 9 };
            yield return new object [] { '(', true, false, false, '8', 9, (KeyCode)'(' | KeyCode.ShiftMask, '8', 9 };
            yield return new object [] { '8', true, true, false, '8', 9, KeyCode.D8 | KeyCode.ShiftMask | KeyCode.AltMask, '8', 9 };
            yield return new object [] { '8', true, true, true, '8', 9, KeyCode.D8 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '8', 9 };
            yield return new object [] { '[', false, true, true, '8', 9, (KeyCode)'[' | KeyCode.AltMask | KeyCode.CtrlMask, '8', 9 };
            yield return new object [] { '9', false, false, false, '9', 10, KeyCode.D9, '9', 10 };
            yield return new object [] { ')', true, false, false, '9', 10, (KeyCode)')' | KeyCode.ShiftMask, '9', 10 };
            yield return new object [] { '9', true, true, false, '9', 10, KeyCode.D9 | KeyCode.ShiftMask | KeyCode.AltMask, '9', 10 };
            yield return new object [] { '9', true, true, true, '9', 10, KeyCode.D9 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '9', 10 };
            yield return new object [] { ']', false, true, true, '9', 10, (KeyCode)']' | KeyCode.AltMask | KeyCode.CtrlMask, '9', 10 };
            yield return new object [] { '0', false, false, false, '0', 11, KeyCode.D0, '0', 11 };
            yield return new object [] { '=', true, false, false, '0', 11, (KeyCode)'=' | KeyCode.ShiftMask, '0', 11 };
            yield return new object [] { '0', true, true, false, '0', 11, KeyCode.D0 | KeyCode.ShiftMask | KeyCode.AltMask, '0', 11 };
            yield return new object [] { '0', true, true, true, '0', 11, KeyCode.D0 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '0', 11 };
            yield return new object [] { '}', false, true, true, '0', 11, (KeyCode)'}' | KeyCode.AltMask | KeyCode.CtrlMask, '0', 11 };
            yield return new object [] { '\'', false, false, false, VK.OEM_4, 12, (KeyCode)'\'', VK.OEM_4, 12 };
            yield return new object [] { '?', true, false, false, VK.OEM_4, 12, (KeyCode)'?' | KeyCode.ShiftMask, VK.OEM_4, 12 };
            yield return new object [] { '\'', true, true, false, VK.OEM_4, 12, (KeyCode)'\'' | KeyCode.ShiftMask | KeyCode.AltMask, VK.OEM_4, 12 };
            yield return new object [] { '\'', true, true, true, VK.OEM_4, 12, (KeyCode)'\'' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, VK.OEM_4, 12 };
            yield return new object [] { '«', false, false, false, VK.OEM_6, 13, (KeyCode)'«', VK.OEM_6, 13 };
            yield return new object [] { '»', true, false, false, VK.OEM_6, 13, (KeyCode)'»' | KeyCode.ShiftMask, VK.OEM_6, 13 };
            yield return new object [] { '«', true, true, false, VK.OEM_6, 13, (KeyCode)'«' | KeyCode.ShiftMask | KeyCode.AltMask, VK.OEM_6, 13 };
            yield return new object [] { '«', true, true, true, VK.OEM_6, 13, (KeyCode)'«' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, VK.OEM_6, 13 };
            yield return new object [] { 'á', false, false, false, 'A', 30, (KeyCode)'á', 'A', 30 };
            yield return new object [] { 'Á', true, false, false, 'A', 30, (KeyCode)'Á' | KeyCode.ShiftMask, 'A', 30 };
            yield return new object [] { 'à', false, false, false, 'A', 30, (KeyCode)'à', 'A', 30 };
            yield return new object [] { 'À', true, false, false, 'A', 30, (KeyCode)'À' | KeyCode.ShiftMask, 'A', 30 };
            yield return new object [] { 'é', false, false, false, 'E', 18, (KeyCode)'é', 'E', 18 };
            yield return new object [] { 'É', true, false, false, 'E', 18, (KeyCode)'É' | KeyCode.ShiftMask, 'E', 18 };
            yield return new object [] { 'è', false, false, false, 'E', 18, (KeyCode)'è', 'E', 18 };
            yield return new object [] { 'È', true, false, false, 'E', 18, (KeyCode)'È' | KeyCode.ShiftMask, 'E', 18 };
            yield return new object [] { 'í', false, false, false, 'I', 23, (KeyCode)'í', 'I', 23 };
            yield return new object [] { 'Í', true, false, false, 'I', 23, (KeyCode)'Í' | KeyCode.ShiftMask, 'I', 23 };
            yield return new object [] { 'ì', false, false, false, 'I', 23, (KeyCode)'ì', 'I', 23 };
            yield return new object [] { 'Ì', true, false, false, 'I', 23, (KeyCode)'Ì' | KeyCode.ShiftMask, 'I', 23 };
            yield return new object [] { 'ó', false, false, false, 'O', 24, (KeyCode)'ó', 'O', 24 };
            yield return new object [] { 'Ó', true, false, false, 'O', 24, (KeyCode)'Ó' | KeyCode.ShiftMask, 'O', 24 };
            yield return new object [] { 'ò', false, false, false, 'O', 24, (KeyCode)'ò', 'O', 24 };
            yield return new object [] { 'Ò', true, false, false, 'O', 24, (KeyCode)'Ò' | KeyCode.ShiftMask, 'O', 24 };
            yield return new object [] { 'ú', false, false, false, 'U', 22, (KeyCode)'ú', 'U', 22 };
            yield return new object [] { 'Ú', true, false, false, 'U', 22, (KeyCode)'Ú' | KeyCode.ShiftMask, 'U', 22 };
            yield return new object [] { 'ù', false, false, false, 'U', 22, (KeyCode)'ù', 'U', 22 };
            yield return new object [] { 'Ù', true, false, false, 'U', 22, (KeyCode)'Ù' | KeyCode.ShiftMask, 'U', 22 };
            yield return new object [] { 'ö', false, false, false, 'O', 24, (KeyCode)'ö', 'O', 24 };
            yield return new object [] { 'Ö', true, false, false, 'O', 24, (KeyCode)'Ö' | KeyCode.ShiftMask, 'O', 24 };
            yield return new object [] { '<', false, false, false, VK.OEM_102, 86, (KeyCode)'<', VK.OEM_102, 86 };
            yield return new object [] { '>', true, false, false, VK.OEM_102, 86, (KeyCode)'>' | KeyCode.ShiftMask, VK.OEM_102, 86 };
            yield return new object [] { '<', true, true, false, VK.OEM_102, 86, (KeyCode)'<' | KeyCode.ShiftMask | KeyCode.AltMask, VK.OEM_102, 86 };
            yield return new object [] { '<', true, true, true, VK.OEM_102, 86, (KeyCode)'<' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, VK.OEM_102, 86 };
            yield return new object [] { 'ç', false, false, false, VK.OEM_3, 39, (KeyCode)'ç', VK.OEM_3, 39 };
            yield return new object [] { 'Ç', true, false, false, VK.OEM_3, 39, (KeyCode)'Ç' | KeyCode.ShiftMask, VK.OEM_3, 39 };
            yield return new object [] { 'ç', true, true, false, VK.OEM_3, 39, (KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask, VK.OEM_3, 39 };
            yield return new object [] { 'ç', true, true, true, VK.OEM_3, 39, (KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, VK.OEM_3, 39 };
            yield return new object [] { '¨', false, true, true, VK.OEM_PLUS, 26, (KeyCode)'¨' | KeyCode.AltMask | KeyCode.CtrlMask, VK.OEM_PLUS, 26 };
            yield return new object [] { '\0', false, false, false, VK.PRIOR, 73, KeyCode.PageUp, VK.PRIOR, 73 };
            yield return new object [] { '\0', true, false, false, VK.PRIOR, 73, KeyCode.PageUp | KeyCode.ShiftMask, VK.PRIOR, 73 };
            yield return new object [] { '\0', true, true, false, VK.PRIOR, 73, KeyCode.PageUp | KeyCode.ShiftMask | KeyCode.AltMask, VK.PRIOR, 73 };
            yield return new object [] { '\0', true, true, true, VK.PRIOR, 73, KeyCode.PageUp | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, VK.PRIOR, 73 };
            yield return new object [] { '~', false, false, false, VK.SPACE, 57, (KeyCode)'~', VK.SPACE, 57 };
            yield return new object [] { '^', false, false, false, VK.SPACE, 57, (KeyCode)'^', VK.SPACE, 57 };
        }
    }

    [Theory]
    [InlineData ('a', ConsoleKey.A, 'a', ConsoleKey.A)]
    [InlineData ('A', ConsoleKey.A, 'A', ConsoleKey.A)]
    [InlineData ('á', ConsoleKey.A, 'á', ConsoleKey.A)]
    [InlineData ('Á', ConsoleKey.A, 'Á', ConsoleKey.A)]
    [InlineData ('à', ConsoleKey.A, 'à', ConsoleKey.A)]
    [InlineData ('À', ConsoleKey.A, 'À', ConsoleKey.A)]
    [InlineData ('5', ConsoleKey.D5, '5', ConsoleKey.D5)]
    [InlineData ('%', ConsoleKey.D5, '%', ConsoleKey.D5)]
    [InlineData ('€', ConsoleKey.D5, '€', ConsoleKey.D5)]
    [InlineData ('?', ConsoleKey.Oem4, '?', ConsoleKey.Oem4)]
    [InlineData ('\'', ConsoleKey.Oem4, '\'', ConsoleKey.Oem4)]
    [InlineData ('q', ConsoleKey.Q, 'q', ConsoleKey.Q)]
    [InlineData ('\0', ConsoleKey.F2, '\0', ConsoleKey.F2)]
    [InlineData ('英', ConsoleKey.None, '英', ConsoleKey.None)]
    [InlineData ('´', ConsoleKey.None, '´', ConsoleKey.Oem1)]
    [InlineData ('`', ConsoleKey.None, '`', ConsoleKey.Oem1)]

    //[InlineData ('~', ConsoleKey.None, '~', ConsoleKey.Oem2)]
    //[InlineData ('^', ConsoleKey.None, '^', ConsoleKey.Oem2)] // BUGBUG: '^' is Shift-6 on ENG keyboard,not Oem2.
    // For the US standard keyboard, Oem2 is the /? key
    public void EncodeKeyCharForVKPacket_DecodeVKPacketToKConsoleKeyInfo (
        char keyChar,
        ConsoleKey consoleKey,
        char expectedChar,
        ConsoleKey expectedConsoleKey
    )
    {
        var consoleKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, false, false, false);
        var encodedKeyChar = ConsoleKeyMapping.EncodeKeyCharForVKPacket (consoleKeyInfo);
        var encodedConsoleKeyInfo = new ConsoleKeyInfo (encodedKeyChar, ConsoleKey.Packet, false, false, false);
        var decodedConsoleKeyInfo = ConsoleKeyMapping.DecodeVKPacketToKConsoleKeyInfo (encodedConsoleKeyInfo);
        Assert.Equal (consoleKeyInfo.Key, consoleKey);
        Assert.Equal (expectedConsoleKey, decodedConsoleKeyInfo.Key);
        Assert.Equal (expectedChar, decodedConsoleKeyInfo.KeyChar);
    }

    [Theory]
    [InlineData ((KeyCode)'a', false, ConsoleKey.A, 'a')]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, false, ConsoleKey.A, 'A')]
    [InlineData ((KeyCode)'á', false, ConsoleKey.A, 'á')]
    [InlineData ((KeyCode)'Á' | KeyCode.ShiftMask, false, ConsoleKey.A, 'Á')]
    [InlineData ((KeyCode)'à', false, ConsoleKey.A, 'à')]
    [InlineData ((KeyCode)'À' | KeyCode.ShiftMask, false, ConsoleKey.A, 'À')]
    [InlineData (KeyCode.D5, false, ConsoleKey.D5, '5')]
    [InlineData ((KeyCode)'%' | KeyCode.ShiftMask, false, ConsoleKey.D5, '%')]

    //[InlineData ((KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask, false, ConsoleKey.D5, '€')] // Bogus test. This is not true on ENG keyboard layout.
    [InlineData ((KeyCode)'?' | KeyCode.ShiftMask, false, ConsoleKey.Oem4, '?')]
    [InlineData ((KeyCode)'\'', false, ConsoleKey.Oem4, '\'')]
    [InlineData ((KeyCode)'q', false, ConsoleKey.Q, 'q')]
    [InlineData (KeyCode.F2, true, ConsoleKey.F2, 'q')]
    [InlineData ((KeyCode)'英', false, ConsoleKey.None, '英')]
    [InlineData (KeyCode.Enter, true, ConsoleKey.Enter, '\r')]
    public void MapKeyCodeToConsoleKey_GetKeyCharFromUnicodeChar (
        KeyCode keyCode,
        bool expectedIsConsoleKey,
        ConsoleKey expectedConsoleKey,
        char expectedConsoleKeyChar
    )
    {
        var modifiers = ConsoleKeyMapping.MapToConsoleModifiers (keyCode);
        var consoleKey = ConsoleKeyMapping.MapKeyCodeToConsoleKey (keyCode, out bool isConsoleKey);

        if (isConsoleKey)
        {
            Assert.True (isConsoleKey == expectedIsConsoleKey);
            Assert.Equal (expectedConsoleKey, (ConsoleKey)consoleKey);
            Assert.Equal (expectedConsoleKeyChar, consoleKey);
        }
        else
        {
            var keyChar =
                ConsoleKeyMapping.GetKeyCharFromUnicodeChar (
                                                             consoleKey,
                                                             modifiers,
                                                             out consoleKey,
                                                             out _,
                                                             isConsoleKey
                                                            );
            Assert.True (isConsoleKey == expectedIsConsoleKey);
            Assert.Equal (expectedConsoleKey, (ConsoleKey)consoleKey);
            Assert.Equal (expectedConsoleKeyChar, keyChar);
        }
    }
#endif

    [Theory]
    [InlineData ('a', ConsoleKey.A, false, false, false, (KeyCode)'a')]
    [InlineData ('A', ConsoleKey.A, true, false, false, KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('á', ConsoleKey.A, false, false, false, (KeyCode)'á')]
    [InlineData ('Á', ConsoleKey.A, true, false, false, (KeyCode)'Á' | KeyCode.ShiftMask)]
    [InlineData ('à', ConsoleKey.A, false, false, false, (KeyCode)'à')]
    [InlineData ('À', ConsoleKey.A, true, false, false, (KeyCode)'À' | KeyCode.ShiftMask)]
    [InlineData ('5', ConsoleKey.D5, false, false, false, KeyCode.D5)]
    [InlineData ('%', ConsoleKey.D5, true, false, false, (KeyCode)'%' | KeyCode.ShiftMask)]
    [InlineData ('€', ConsoleKey.D5, false, true, true, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask)]
    [InlineData ('?', ConsoleKey.Oem4, true, false, false, (KeyCode)'?' | KeyCode.ShiftMask)]
    [InlineData ('\'', ConsoleKey.Oem4, false, false, false, (KeyCode)'\'')]
    [InlineData ('q', ConsoleKey.Q, false, false, false, (KeyCode)'q')]
    [InlineData ('\0', ConsoleKey.F2, false, false, false, KeyCode.F2)]
    [InlineData ('英', ConsoleKey.None, false, false, false, (KeyCode)'英')]
    [InlineData ('\r', ConsoleKey.Enter, false, false, false, KeyCode.Enter)]
    public void MapConsoleKeyInfoToKeyCode_Also_Return_Modifiers (
        char keyChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool control,
        KeyCode expectedKeyCode
    )
    {
        var consoleKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);
        KeyCode keyCode = MapConsoleKeyInfoToKeyCode (consoleKeyInfo);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    [Theory]
    [InlineData ('a', false, false, false, (KeyCode)'a')]
    [InlineData ('A', true, false, false, KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('á', false, false, false, (KeyCode)'á')]
    [InlineData ('Á', true, false, false, (KeyCode)'Á' | KeyCode.ShiftMask)]
    [InlineData ('à', false, false, false, (KeyCode)'à')]
    [InlineData ('À', true, false, false, (KeyCode)'À' | KeyCode.ShiftMask)]
    [InlineData ('5', false, false, false, KeyCode.D5)]
    [InlineData ('%', true, false, false, (KeyCode)'%' | KeyCode.ShiftMask)]
    [InlineData ('€', false, true, true, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask)]
    [InlineData ('?', true, false, false, (KeyCode)'?' | KeyCode.ShiftMask)]
    [InlineData ('\'', false, false, false, (KeyCode)'\'')]
    [InlineData ('q', false, false, false, (KeyCode)'q')]
    [InlineData ((uint)KeyCode.F2, false, false, false, KeyCode.F2)]
    [InlineData ('英', false, false, false, (KeyCode)'英')]
    [InlineData ('\r', false, false, false, KeyCode.Enter)]
    [InlineData ('\n', false, false, false, (KeyCode)'\n')]
    public void MapToKeyCodeModifiers_Tests (
        uint keyChar,
        bool shift,
        bool alt,
        bool control,
        KeyCode expectedKeyCode
    )
    {
        ConsoleModifiers modifiers = GetModifiers (shift, alt, control);
        var keyCode = (KeyCode)keyChar;
        keyCode = MapToKeyCodeModifiers (modifiers, keyCode);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    [Theory]
    [MemberData (nameof (GetScanCodeData))]
    public void GetScanCodeFromConsoleKeyInfo_Tests (
        char keyChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool control,
        uint expectedScanCode
    )
    {
        var consoleKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);
        uint scanCode = GetScanCodeFromConsoleKeyInfo (consoleKeyInfo);

        Assert.Equal (scanCode, expectedScanCode);
    }

    public static IEnumerable<object []> GetScanCodeData ()
    {
        yield return ['a', ConsoleKey.A, false, false, false, 30];
        yield return ['A', ConsoleKey.A, true, false, false, 30];
        yield return ['á', ConsoleKey.A, false, false, false, 30];
        yield return ['Á', ConsoleKey.A, true, false, false, 30];
        yield return ['à', ConsoleKey.A, false, false, false, 30];
        yield return ['À', ConsoleKey.A, true, false, false, 30];
        yield return ['0', ConsoleKey.D0, false, false, false, 11];
        yield return ['=', ConsoleKey.D0, true, false, false, 11];
        yield return ['}', ConsoleKey.D0, false, true, true, 11];
        yield return ['1', ConsoleKey.D1, false, false, false, 2];
        yield return ['!', ConsoleKey.D1, true, false, false, 2];
        yield return ['2', ConsoleKey.D2, false, false, false, 3];
        yield return ['"', ConsoleKey.D2, true, false, false, 3];
        yield return ['@', ConsoleKey.D2, false, true, true, 3];
        yield return ['3', ConsoleKey.D3, false, false, false, 4];
        yield return ['#', ConsoleKey.D3, true, false, false, 4];
        yield return ['£', ConsoleKey.D3, false, true, true, 4];
        yield return ['4', ConsoleKey.D4, false, false, false, 5];
        yield return ['$', ConsoleKey.D4, true, false, false, 5];
        yield return ['§', ConsoleKey.D4, false, true, true, 5];
        yield return ['5', ConsoleKey.D5, false, false, false, 6];
        yield return ['%', ConsoleKey.D5, true, false, false, 6];
        yield return ['€', ConsoleKey.D5, false, true, true, 6];
        yield return ['6', ConsoleKey.D6, false, false, false, 7];
        yield return ['&', ConsoleKey.D6, true, false, false, 7];
        yield return ['7', ConsoleKey.D7, false, false, false, 8];
        yield return ['/', ConsoleKey.D7, true, false, false, 8];
        yield return ['{', ConsoleKey.D7, false, true, true, 8];
        yield return ['8', ConsoleKey.D8, false, false, false, 9];
        yield return ['(', ConsoleKey.D8, true, false, false, 9];
        yield return ['[', ConsoleKey.D8, false, true, true, 9];
        yield return ['9', ConsoleKey.D9, false, false, false, 10];
        yield return [')', ConsoleKey.D9, true, false, false, 10];
        yield return [']', ConsoleKey.D9, false, true, true, 10];
        yield return ['´', ConsoleKey.Oem1, false, false, false, 27];
        yield return ['`', ConsoleKey.Oem1, true, false, false, 27];
        yield return ['~', ConsoleKey.Oem2, false, false, false, 43];
        yield return ['^', ConsoleKey.Oem2, true, false, false, 43];
        yield return ['ç', ConsoleKey.Oem3, false, false, false, 39];
        yield return ['Ç', ConsoleKey.Oem3, true, false, false, 39];
        yield return ['\'', ConsoleKey.Oem4, false, false, false, 12];
        yield return ['?', ConsoleKey.Oem4, true, false, false, 12];
        yield return ['\\', ConsoleKey.Oem5, false, true, true, 41];
        yield return ['|', ConsoleKey.Oem5, true, false, false, 41];
        yield return ['«', ConsoleKey.Oem6, false, true, true, 13];
        yield return ['»', ConsoleKey.Oem6, true, false, false, 13];
        yield return ['º', ConsoleKey.Oem7, false, true, true, 40];
        yield return ['ª', ConsoleKey.Oem7, true, false, false, 40];
        yield return ['+', ConsoleKey.OemPlus, false, true, true, 26];
        yield return ['*', ConsoleKey.OemPlus, true, false, false, 26];
        yield return ['¨', ConsoleKey.OemPlus, false, true, true, 26];
        yield return [',', ConsoleKey.OemComma, false, true, true, 51];
        yield return [';', ConsoleKey.OemComma, true, false, false, 51];
        yield return ['.', ConsoleKey.OemPeriod, false, true, true, 52];
        yield return [':', ConsoleKey.OemPeriod, true, false, false, 52];
        yield return ['-', ConsoleKey.OemMinus, false, true, true, 53];
        yield return ['_', ConsoleKey.OemMinus, true, false, false, 53];
        yield return ['q', ConsoleKey.Q, false, false, false, 16];
        yield return ['\0', ConsoleKey.F2, false, false, false, 60];
        yield return ['英', ConsoleKey.None, false, false, false, 0];
        yield return ['英', ConsoleKey.None, true, false, false, 0];
    }

    [Theory]
    [MemberData (nameof (UnShiftedChars))]
    public void GetKeyChar_Shifted_Char_From_UnShifted_Char (
        char unicodeChar,
        char expectedKeyChar,
        KeyCode expectedKeyCode
    )
    {
        ConsoleModifiers modifiers = GetModifiers (true, false, false);
        uint keyChar = GetKeyChar (unicodeChar, modifiers);
        Assert.Equal (keyChar, expectedKeyChar);

        var keyCode = (KeyCode)keyChar;
        keyCode = MapToKeyCodeModifiers (modifiers, keyCode);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    public static IEnumerable<object []> UnShiftedChars =>
        new List<object []>
        {
            new object[] { 'a', 'A', KeyCode.A | KeyCode.ShiftMask },
            new object[] { 'z', 'Z', KeyCode.Z | KeyCode.ShiftMask },
            new object[] { 'á', 'Á', (KeyCode)'Á' | KeyCode.ShiftMask },
            new object[] { 'à', 'À', (KeyCode)'À' | KeyCode.ShiftMask },
            new object[]{ 'ý', 'Ý', (KeyCode)'Ý' | KeyCode.ShiftMask },
            new object[]{ '1', '!', (KeyCode)'!' | KeyCode.ShiftMask },
            new object[]{ '2', '"', (KeyCode)'"' | KeyCode.ShiftMask },
            new object[]{ '3', '#', (KeyCode)'#' | KeyCode.ShiftMask },
            new object[]{ '4', '$', (KeyCode)'$' | KeyCode.ShiftMask },
            new object[]{ '5', '%', (KeyCode)'%' | KeyCode.ShiftMask },
            new object[]{ '6', '&', (KeyCode)'&' | KeyCode.ShiftMask },
            new object[]{ '7', '/', (KeyCode)'/' | KeyCode.ShiftMask },
            new object[]{ '8', '(', (KeyCode)'(' | KeyCode.ShiftMask },
            new object[]{ '9', ')', (KeyCode)')' | KeyCode.ShiftMask },
            new object[]{ '0', '=', (KeyCode)'=' | KeyCode.ShiftMask },
            new object[]{ '\\', '|', (KeyCode)'|' | KeyCode.ShiftMask },
            new object[]{ '\'', '?', (KeyCode)'?' | KeyCode.ShiftMask },
            new object[]{ '«', '»', (KeyCode)'»' | KeyCode.ShiftMask },
            new object[]{ '+', '*', (KeyCode)'*' | KeyCode.ShiftMask },
            new object[]{ '´', '`', (KeyCode)'`' | KeyCode.ShiftMask },
            new object[]{ 'º', 'ª', (KeyCode)'ª' | KeyCode.ShiftMask },
            new object[]{ '~', '^', (KeyCode)'^' | KeyCode.ShiftMask },
            new object[]{ '<', '>', (KeyCode)'>' | KeyCode.ShiftMask },
            new object[]{ ',', ';', (KeyCode)';' | KeyCode.ShiftMask },
            new object[]{ '.', ':', (KeyCode)':' | KeyCode.ShiftMask },
            new object[]{ '-', '_', (KeyCode)'_' | KeyCode.ShiftMask },
        };

    [Theory]
    [MemberData (nameof (ShiftedChars))]
    public void GetKeyChar_UnShifted_Char_From_Shifted_Char (
        char unicodeChar,
        char expectedKeyChar,
        KeyCode expectedKeyCode
    )
    {
        ConsoleModifiers modifiers = GetModifiers (false, false, false);
        uint keyChar = GetKeyChar (unicodeChar, modifiers);
        Assert.Equal (keyChar, expectedKeyChar);

        var keyCode = (KeyCode)keyChar;
        keyCode = MapToKeyCodeModifiers (modifiers, keyCode);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    public static IEnumerable<object []> ShiftedChars =>
        new List<object []>
        {
            new object[] { 'A', 'a', (KeyCode)'a' },
            new object[] { 'Z', 'z', (KeyCode)'z' },
            new object[] { 'Á', 'á', (KeyCode)'á' },
            new object[] { 'À', 'à', (KeyCode)'à' },
            new object[] { 'Ý', 'ý', (KeyCode)'ý' },
            new object[] { '!', '1', KeyCode.D1 },
            new object[] { '"', '2', KeyCode.D2 },
            new object[] { '#', '3', KeyCode.D3 },
            new object[] { '$', '4', KeyCode.D4 },
            new object[] { '%', '5', KeyCode.D5 },
            new object[] { '&', '6', KeyCode.D6 },
            new object[] { '/', '7', KeyCode.D7 },
            new object[] { '(', '8', KeyCode.D8 },
            new object[] { ')', '9', KeyCode.D9 },
            new object[] { '=', '0', KeyCode.D0 },
            new object[] { '|', '\\', (KeyCode)'\\' },
            new object[] { '?', '\'', (KeyCode)'\'' },
            new object[] { '»', '«', (KeyCode)'«' },
            new object[] { '*', '+', (KeyCode)'+' },
            new object[] { '`', '´', (KeyCode)'´' },
            new object[] { 'ª', 'º', (KeyCode)'º' },
            new object[] { '^', '~', (KeyCode)'~' },
            new object[] { '>', '<', (KeyCode)'<' },
            new object[] { ';', ',', (KeyCode)',' },
            new object[] { ':', '.', (KeyCode)'.' },
            new object[] { '_', '-', (KeyCode)'-' }
        };
}
