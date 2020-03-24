using System;
using System.Configuration;
using System.Linq;

namespace genscript
{
	internal class Common
	{
		public static int rows = 8;

		public static int rows384 = 16;

		public static int cols = 12;

		public static int cols384 = 24;

		public static bool Mix2Plate
		{
			get
			{
				return ConfigurationManager.AppSettings.AllKeys.Contains("Mix2Plate") && bool.Parse(ConfigurationManager.AppSettings["Mix2Plate"]);
			}
		}

		public static int PlateCnt
		{
			get
			{
				return int.Parse(ConfigurationManager.AppSettings["plateCnt"]);
			}
		}

		public static int GetWellID(int rowIndex, int colIndex)
		{
			return colIndex * 8 + rowIndex + 1;
		}

		public static int GetWellID384(int rowIndex, int colIndex)
		{
			return colIndex * 16 + rowIndex + 1;
		}

		public static string GetPlateName(string sCSVFile)
		{
			int num = sCSVFile.LastIndexOf("\\");
			string text = sCSVFile.Substring(num + 1);
			num = text.IndexOf("_");
			return text.Substring(0, num);
		}

		public static string GetWellDesc384(int wellID)
		{
			int num = (wellID - 1) / Common.rows384;
			int num2 = wellID - num * Common.rows384 - 1;
			return string.Format("{0}{1}", (char)(65 + num2), num + 1);
		}

		public static string GetWellDesc(int wellID)
		{
			int num = (wellID - 1) / Common.rows;
			int num2 = wellID - num * Common.rows - 1;
			return string.Format("{0}{1}", (char)(65 + num2), num + 1);
		}

		public static string FormatWellID(int wellID)
		{
			return string.Format("{0:D3}", wellID);
		}

		public static int GetWellID(string sWell)
		{
			int rowIndex = (int)(sWell.First<char>() - 'A');
			int colIndex = int.Parse(sWell.Substring(1)) - 1;
			return Common.GetWellID384(rowIndex, colIndex);
		}

		internal static bool IsInvalidWellID(string s)
		{
			if (s.Length > 3)
			{
				return true;
			}
			int num = -1;
			try
			{
				num = Common.GetWellID(s);
			}
			catch (Exception)
			{
				return true;
			}
			return num < 0 || num > 96;
		}
	}
}
