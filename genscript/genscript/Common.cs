using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace genscript
{
    class Common
    {
        public static int rows = 8;
        public static int cols = 12;
        public static int GetWellID(int rowIndex, int colIndex)
        {
            return colIndex * 8 + rowIndex + 1;
        }

        public static string GetPlateName(string sCSVFile)
        {
            int pos = sCSVFile.LastIndexOf("\\");
            string sName = sCSVFile.Substring(pos + 1);
            pos = sName.IndexOf("_");
            sName = sName.Substring(0, pos);
            return sName;
        }

        internal static int GetWellID(string sWell)
        {
            int rowIndex = sWell.First() - 'A';
            int colIndex = int.Parse(sWell.Substring(1))- 1;
            return GetWellID(rowIndex, colIndex);
        }
    }
}
