using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace One2X
{
    class Worklist
    {
        private void FillVols(List<ItemInfo> itemsInfoCopy)
        {
            for (int i = 0; i < itemsInfoCopy.Count; i++)
            {
                var itemInfo = itemsInfoCopy[i];
                itemInfo.vol = OdSheet.eachPlateID_Vols[itemInfo.plateName][itemInfo.srcWellID];
            }
        }

        public void Generate(List<ItemInfo> items, string sFile)
        {
            FillVols(items);
            int totalWellNeeded = items.Sum(x => x.sliceCnt);
            int curWellID = 1;
            string dstLabware = "";
            int dstWellID = 0;
            List<PipettingInfo> allPipettings = new List<PipettingInfo>();
            foreach(var item in items)
            {
                GetDestPlateNameAndWell(curWellID, ref dstLabware, ref dstWellID);
                PipettingInfo pipettingInfo = new PipettingInfo(item.sID, item.plateName, item.srcWellID, dstLabware, dstWellID, item.vol);
                allPipettings.Add(pipettingInfo);
                curWellID++;
            }
            foreach(var item in items)
            {
                if(item.sliceCnt > 1)
                {
                    for(int i = 0; i< item.sliceCnt -1 ;i++)
                    {
                        GetDestPlateNameAndWell(curWellID, ref dstLabware, ref dstWellID);
                        curWellID++;
                        PipettingInfo pipettingInfo = new PipettingInfo(item.sID, item.plateName, item.srcWellID, dstLabware, dstWellID, item.vol);
                        allPipettings.Add(pipettingInfo);
                        
                    }
                }
            }
            List<string> strs = new List<string>();
            allPipettings.ForEach(x => strs.Add(Format(x)));
            File.WriteAllLines(sFile, strs);
            Console.WriteLine(string.Format("csv has been generated into:{0}.",sFile));
        }

        private string Format(PipettingInfo pipettingInfo)
        {
            return string.Format("{0},{1},{2},{3},{4},{5}",
                    pipettingInfo.srcLabware,
                    Common.GetWellDesc(pipettingInfo.srcWellID),
                    pipettingInfo.dstLabware,
                    pipettingInfo.dstWellID,
                    pipettingInfo.vol,
                    pipettingInfo.sPrimerID);
        }



        private void GetDestPlateNameAndWell(int curWellID, ref string dstLabware, ref int dstWellID)
        {
            int labwareIndex = (curWellID - 1) / GlobalVars.LabwareWellCnt;
            int wellIndexInLabware = curWellID - 1 - labwareIndex * GlobalVars.LabwareWellCnt;
            dstLabware = string.Format("dst{0}", labwareIndex + 1);
            dstWellID = wellIndexInLabware + 1;
        }
    }
}
