using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace HintLabwares
{
    class GlobalVars
    {
        public static int batchID = 0;
        public static string workingFolder = ConfigurationManager.AppSettings["workingFolder"];
        public static string outputFolder = workingFolder + "\\Outputs\\";
        public static string resultFile = outputFolder + "\\hintResult.txt";
    }
}
