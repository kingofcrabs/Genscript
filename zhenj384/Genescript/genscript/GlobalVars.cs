using System;
using System.Configuration;

namespace genscript
{
	internal class GlobalVars
	{
		public static int LabwareWellCnt = 0;

		public static string WorkingFolder = "";

		public static bool pipettingMixFirst = bool.Parse(ConfigurationManager.AppSettings["pipettingMixFirst"]);
	}
}
