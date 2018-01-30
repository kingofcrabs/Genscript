using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace One2X
{
    class Common
    {
        public static int rowCnt = 8;
        public static int colCnt = 12;
        public static int GetWellID(int rowIndex, int colIndex)
        {
            return colIndex * 8 + rowIndex + 1;
        }

        public static void SwitchTo96()
        {
            rowCnt = 8;
            colCnt = 12;
        }
        public static  void SwitchTo384()
        {
            rowCnt = 16;
            colCnt = 24;
        }

        public static bool Mix2Plate
        {
            get
            {
                bool mix2PlateFlag = ConfigurationManager.AppSettings.AllKeys.Contains("Mix2Plate");
                if(!mix2PlateFlag)
                    return false;
                return bool.Parse(ConfigurationManager.AppSettings["Mix2Plate"]);
            }
        }

        public static int PlateCnt
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["plateCnt"]);
            }
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

        internal static int GetWellID(string sWell)
        {
            int rowIndex = sWell.First() - 'A';
            int colIndex = int.Parse(sWell.Substring(1))- 1;
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
            catch(Exception ex)
            {
                return true;
            }
            int maxWellID = rowCnt * colCnt;
            return wellID < 0 || wellID > maxWellID;
        }
    }
}
