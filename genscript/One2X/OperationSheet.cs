using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace One2X
{
    class OperationSheet
    {
        int startIndex = 8;
        //int endIndex = 55;
        int cnt = 48;
        int extraDescriptionColumn = 7; //index
        int IDColumn = 0;
        int NameColumn = 6;
        int srcWellColumn = 4;  //index
        int SliceCntColumn = 3; 
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
            itemsInfo.AddRange(GetItemsInfo(firstHalfStrLists));
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
            Dictionary<string, string> primerName_ExtraDescription = new Dictionary<string, string>();


            foreach (List<string> strs in strLists)
            {
                if (IsEmptySample(strs))
                    continue;

                string primerName = GetMainPrimerName(strs[NameColumn]);
                if (strs[extraDescriptionColumn] != string.Empty)
                {
                    if (!primerName_ExtraDescription.ContainsKey(primerName))
                        primerName_ExtraDescription.Add(primerName, strs[extraDescriptionColumn]);
                }
                string extraDesc = primerName_ExtraDescription.ContainsKey(primerName) ? primerName_ExtraDescription[primerName] : empty;

                itemsInfo.Add(GetItemInfo(strs,extraDesc));

            }
            return itemsInfo;
        }

        private bool IsEmptySample(List<string> strs)
        {
            return strs.First() == "";
        }

        private string GetMainPrimerName(string name)
        {
            string[] strs = name.Split('_');
            return strs[0];

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
            itemInfo.sliceCnt = int.Parse(strs[SliceCntColumn]);

            string name = strs[NameColumn];
            ParseID( itemInfo.sID,name,ref itemInfo);
            itemInfo.srcWellID = Common.GetWellID(strs[srcWellColumn]);
            return itemInfo;
        }

        private void ParseID(string sPrimerID, string sPrimerName, ref ItemInfo itemInfo)
        {
            List<string> strs = sPrimerName.Split('_').ToList();
            itemInfo.plateName = sPlateName;
            itemInfo.mainID = strs[0];
            string sSubID = strs.Last();
            if(itemInfo.sExtraDescription == empty)
            {
                itemInfo.subID = 1;
            }
            else
            {
                int val = 0;
                bool bok = int.TryParse(strs.Last(), out val);
                if (!bok)
                    throw new Exception(string.Format("Primer :{0}'s name is invalid!", sPrimerID));
                itemInfo.subID = val;
            }
            
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
        public int sliceCnt;
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
