using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UICatalog {
	public static class NumberToWords {
		private static String [] units = { "Zero", "One", "Two", "Three",
		    "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven",
		    "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
		    "Seventeen", "Eighteen", "Nineteen" };
		private static String [] tens = { "", "", "Twenty", "Thirty", "Forty",
		    "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

		public static String ConvertAmount (double amount)
		{
			try {
				Int64 amount_int = (Int64)amount;
				Int64 amount_dec = (Int64)Math.Round ((amount - (double)(amount_int)) * 100);
				if (amount_dec == 0) {
					return Convert (amount_int) + " Only.";
				} else {
					return Convert (amount_int) + " Point " + Convert (amount_dec) + " Only.";
				}
			} catch (Exception e) {
				throw new ArgumentOutOfRangeException (e.Message);
			}
		}

		public static String Convert (Int64 i)
		{
			if (i < 20) {
				return units [i];
			}
			if (i < 100) {
				return tens [i / 10] + ((i % 10 > 0) ? " " + Convert (i % 10) : "");
			}
			if (i < 1000) {
				return units [i / 100] + " Hundred"
					+ ((i % 100 > 0) ? " And " + Convert (i % 100) : "");
			}
			if (i < 100000) {
				return Convert (i / 1000) + " Thousand "
				+ ((i % 1000 > 0) ? " " + Convert (i % 1000) : "");
			}
			if (i < 10000000) {
				return Convert (i / 100000) + " Lakh "
					+ ((i % 100000 > 0) ? " " + Convert (i % 100000) : "");
			}
			if (i < 1000000000) {
				return Convert (i / 10000000) + " Crore "
					+ ((i % 10000000 > 0) ? " " + Convert (i % 10000000) : "");
			}
			return Convert (i / 1000000000) + " Arab "
				+ ((i % 1000000000 > 0) ? " " + Convert (i % 1000000000) : "");
		}
	}
}
