using System;
using System.Runtime.InteropServices;

namespace Ender
{
	public static class Utils
	{
		/* ender_utils.h */
		[DllImport("libender.dll")]
		static extern string ender_utils_name_convert(string s, Case scase, Notation snot,
				Case dcase, Notation dnot);

		public enum Case
		{
			CAMEL,
			PASCAL,
			UNDERSCORE,
		}

		public enum Notation
		{
			ENGLISH,
			LATIN,
		}

		private static string Convert(string s, Case scase, Notation snot,
				Case dcase, Notation dnot)
		{
			return ender_utils_name_convert(s, scase, snot, dcase, dnot);
		}

		private static string camelize(string str)
		{
			string[] splitByPt = str.Split('.');
			string ret = null;
 
			foreach(string wordByPt in splitByPt)
			{
				string result = "";

				string[] splitByUnder = wordByPt.Split('_');
				foreach(string word in splitByUnder)
				{
					result += word.Substring(0, 1).ToUpper() + word.Substring(1);
				}
				if (ret == null)
					ret = result;
				else
					ret += "." + result;

			}
			return ret;
		}

		private static string camelize(string[] strArray)
		{
			string result = "";
			foreach(string word in strArray)
			{
				result += word.Substring(0, 1).ToUpper() + word.Substring(1);
			}
			return result;

		}
	}
}
