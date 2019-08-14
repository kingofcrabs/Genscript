using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class Common384
    {
        public static int rowCnt = 16;
        public static int colCnt = 24;

        public static int GetWellID(int rowIndex, int colIndex)
        {
            return colIndex * rowCnt + rowIndex + 1;
        }


        public static string GetPlateName(string sCSVFile)
        {
            int pos = sCSVFile.LastIndexOf("\\");
            string sName = sCSVFile.Substring(pos + 1);
            pos = sName.IndexOf("_");
            sName = sName.Substring(0, pos);
            return sName;
        }

        public static string GetWellDesc(int wellID)
        {
            int colIndex = (wellID - 1) / rowCnt;
            int rowIndex = wellID - colIndex * rowCnt - 1;
            return string.Format("{0}{1}", (char)('A' + rowIndex), colIndex + 1);
        }

        public static int GetWellID(string sWell)
        {
            int rowIndex = sWell.ToCharArray()[0] - 'A';
            int colIndex = int.Parse(sWell.Substring(1)) - 1;
            return GetWellID(rowIndex, colIndex);
        }

        internal static bool IsInvalidWellID(string s)
        {
            if (s.Length > 3)
                return true;
            int wellID = -1;
            try
            {
                wellID = GetWellID(s);
            }
            catch (Exception ex)
            {
                return true;
            }
            int maxID = 384;
            return wellID < 0 || wellID > maxID;
        }
    }
}
