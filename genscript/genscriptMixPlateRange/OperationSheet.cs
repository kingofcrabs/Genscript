using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace genscript
{
    class OperationSheet
    {
        int startIndex = 8;
        //int endIndex = 55;
        int cnt = 192;
        int extraDescriptionColumn = 7;
        int IDColumn = 0;
        int srcWellColumn = 6;
        private string sPlateName = "";
        public static string empty = "empty";
        List<ItemInfo> itemsInfo;
        public OperationSheet(string sCSVFile)
        {
            sPlateName = Common.GetPlateName(sCSVFile);
            List<string> strs = File.ReadAllLines(sCSVFile).ToList();
            strs = strs.GetRange(startIndex, cnt);
            var firstHalfStrLists = GetHalfStrLists(strs);
            foreach (List<string> tmpStrs in firstHalfStrLists)
            {
                string sLong = "";
                foreach (string s in tmpStrs)
                {
                    sLong += s;
                    sLong += ",";
                }
                Console.WriteLine(sLong);
            }

            var secondHalfStrLists = GetHalfStrLists(strs, false);
            foreach (List<string> tmpStrs in firstHalfStrLists)
            {
                string sLong = "";
                foreach (string s in tmpStrs)
                {
                    sLong += s;
                    sLong += ",";
                }
                Console.WriteLine(sLong);
            }
            itemsInfo = new List<ItemInfo>();
            firstHalfStrLists.AddRange(secondHalfStrLists);
            try
            {
                itemsInfo.AddRange(GetItemsInfo(firstHalfStrLists));
            }
            catch(Exception ex)
            {
                throw new Exception("Invalid file: " + sCSVFile + ex.Message);
            }
        }

      
        public List<ItemInfo> Items
        {
            get
            {
                return itemsInfo;
            }
        }

        private List<ItemInfo> GetItemsInfo(List<List<string>> strLists)
        {
            List<ItemInfo> itemsInfo = new List<ItemInfo>();
            string sExtraDescription = "";
            string sCurrentIndex = strLists.First()[0];
            sCurrentIndex = GetMainIndex(sCurrentIndex);
            foreach (List<string> strs in strLists)
            {
                string tmpCurrentIndex = GetMainIndex(strs[IDColumn]);
                if (tmpCurrentIndex != sCurrentIndex)
                {
                    sCurrentIndex = tmpCurrentIndex;
                    if( strs[extraDescriptionColumn] == "")
                        strs[extraDescriptionColumn] = empty;
                }
            }

            foreach (List<string> strs in strLists)
            {
                if (strs[extraDescriptionColumn] != string.Empty)
                    sExtraDescription = strs[extraDescriptionColumn];
                if (strs[0] == "")
                    continue;
                CheckExtraDescription(sExtraDescription);
                itemsInfo.Add(GetItemInfo(strs, sExtraDescription));
            }
            
            return itemsInfo;
        }


        private void CheckExtraDescription(string s)
        {
            string sExtraDesc = s;
            string moreInfo = "The description is: " + s;
            int pos = sExtraDesc.IndexOf("**");
            int lastPos = sExtraDesc.LastIndexOf("**");
            if (lastPos != sExtraDesc.Length - 2)
                throw new Exception("Invalid remarks! Last two chars is NOT '**'" + moreInfo);
            if (pos == -1)
                throw new Exception("Invalid remarks! first two chars is NOT '**'" + moreInfo);
            if (lastPos == pos)
                throw new Exception("Invalid remarks! Only one ** found!" + moreInfo);
            
        }

        private string GetMainIndex(string sCurrentIndex)
        {
            string[] strs = sCurrentIndex.Split('_');
            return strs.First();
        }

        private ItemInfo GetItemInfo(List<string> strs, string sExtraDescription)
        {
            ItemInfo itemInfo = new ItemInfo();
            itemInfo.sExtraDescription = sExtraDescription;
            itemInfo.sID = strs[IDColumn];
            ParseID(itemInfo.sID,ref itemInfo);
            itemInfo.srcWellID = Common.GetWellID(strs[srcWellColumn]);
            return itemInfo;
        }

        private void ParseID(string src,ref ItemInfo itemInfo)
        {
            List<string> strs = src.Split('_').ToList();
            itemInfo.plateName = sPlateName;
            itemInfo.mainID = strs[0];
            itemInfo.subID = int.Parse(strs[1]);
        }

        private List<List<string>> GetHalfStrLists(List<string> strs,bool firstHalf = true)
        {
            List<List<string>> halfStrs = new List<List<string>>();
            foreach (string s in strs)
            {
                if (firstHalf)
                    halfStrs.Add(GetHalf(s,true));
                else
                    halfStrs.Add(GetHalf(s,false));
            }
            return halfStrs;
        }


        private List<string> GetHalf(string s,bool bFirstHalf)
        {
            int startIndex = 0;
            int endIndex = 8;
            if (!bFirstHalf)
            {
                startIndex = 9;
                endIndex = 16;
            }
            return GetSubStrs(s,startIndex,endIndex);
        }

        private List<string> GetSubStrs(string s, int startIndex, int endIndex)
        {
            List<string> strs = s.Split(',').ToList();
            strs = strs.GetRange(startIndex, endIndex-startIndex+1);
            return strs;
        }

        private List<string> GetFirstHalf(string s)
        {
            return GetSubStrs(s,0, 8);
        }
    }

    class ItemInfo
    {
        public string sID;
        public int subID;
        public string mainID;
        public string plateName;
        public int srcWellID;
        public string sExtraDescription;
        public int vol;
    }

    class PipettingInfo
    {
        public string sPrimerID;
        public string srcLabware;
        public int srcWellID;
        public string dstLabware;
        public int dstWellID;
        public double vol;
        public int orgDstWellID;

        public PipettingInfo(string sPrimerID,string srcLabware,
            int srcWell, string dstLabware, int dstWell, double v)
        {
            this.sPrimerID = sPrimerID;
            this.srcLabware = srcLabware;
            this.dstLabware = dstLabware;
            this.srcWellID = srcWell;
            this.dstWellID = dstWell;
            this.vol = v;
            orgDstWellID = -1;
        }

        public PipettingInfo(PipettingInfo pipettingInfo)
        {
            sPrimerID = pipettingInfo.sPrimerID;
            srcLabware = pipettingInfo.srcLabware;
            dstLabware = pipettingInfo.dstLabware;
            srcWellID = pipettingInfo.srcWellID;
            dstWellID = pipettingInfo.dstWellID;
            vol = pipettingInfo.vol;
            orgDstWellID = pipettingInfo.orgDstWellID;
        }

       
       
    }
}
