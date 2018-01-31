using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Mix2Plate
{
    class GlobalVars
    {
        public static int LabwareWellCnt = 0;
        public static string WorkingFolder = "";
        public static bool pipettingMixFirst = bool.Parse(ConfigurationManager.AppSettings["pipettingMixFirst"]);
    }
}
