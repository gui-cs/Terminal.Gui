using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.Analyzers.Internal.Tests.Generators.EnumExtensions.EnumDefinitions;
internal class SignedEnumMemberValues
{
    internal const int Bit31 = ~0b_01111111_11111111_11111111_11111111;
    internal const int Bit30 =  0b_01000000_00000000_00000000_00000000;
    internal const int Bit29 =  0b_00100000_00000000_00000000_00000000;
    internal const int Bit28 =  0b_00010000_00000000_00000000_00000000;
    internal const int Bit27 =  0b_00001000_00000000_00000000_00000000;
    internal const int Bit26 =  0b_00000100_00000000_00000000_00000000;
    internal const int Bit25 =  0b_00000010_00000000_00000000_00000000;
    internal const int Bit24 =  0b_00000001_00000000_00000000_00000000;
    internal const int Bit23 =  0b_00000000_10000000_00000000_00000000;
    internal const int Bit22 =  0b_00000000_01000000_00000000_00000000;
    internal const int Bit21 =  0b_00000000_00100000_00000000_00000000;
    internal const int Bit20 =  0b_00000000_00010000_00000000_00000000;
    internal const int Bit19 =  0b_00000000_00001000_00000000_00000000;
    internal const int Bit18 =  0b_00000000_00000100_00000000_00000000;
    internal const int Bit17 =  0b_00000000_00000010_00000000_00000000;
    internal const int Bit16 =  0b_00000000_00000001_00000000_00000000;
    internal const int Bit15 =  0b_00000000_00000000_10000000_00000000;
    internal const int Bit14 =  0b_00000000_00000000_01000000_00000000;
    internal const int Bit13 =  0b_00000000_00000000_00100000_00000000;
    internal const int Bit12 =  0b_00000000_00000000_00010000_00000000;
    internal const int Bit11 =  0b_00000000_00000000_00001000_00000000;
    internal const int Bit10 =  0b_00000000_00000000_00000100_00000000;
    internal const int Bit09 =  0b_00000000_00000000_00000010_00000000;
    internal const int Bit08 =  0b_00000000_00000000_00000001_00000000;
    internal const int Bit07 =  0b_00000000_00000000_00000000_10000000;
    internal const int Bit06 =  0b_00000000_00000000_00000000_01000000;
    internal const int Bit05 =  0b_00000000_00000000_00000000_00100000;
    internal const int Bit04 =  0b_00000000_00000000_00000000_00010000;
    internal const int Bit03 =  0b_00000000_00000000_00000000_00001000;
    internal const int Bit02 =  0b_00000000_00000000_00000000_00000100;
    internal const int Bit01 =  0b_00000000_00000000_00000000_00000010;
    internal const int Bit00 =  0b_00000000_00000000_00000000_00000001;
    internal const int All_0 =  0;
    internal const int All_1 =  ~All_0;
    internal const int Alternating_01 = 0b_01010101_01010101_01010101_01010101;
    internal const int Alternating_10 = ~Alternating_01;
    internal const int EvenBytesHigh = 0b_00000000_11111111_00000000_11111111;
    internal const int OddBytesHigh = ~EvenBytesHigh;
}
