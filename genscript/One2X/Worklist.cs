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
        List<string> mix2plateKeywords = new List<string>() { "Start", "End", "Mix" };
        private void FillVols(List<ItemInfo> itemsInfoCopy)
        {
            for (int i = 0; i < itemsInfoCopy.Count; i++)
            {
                var itemInfo = itemsInfoCopy[i];
                itemInfo.vol = OdSheet.eachPlateID_Vols[itemInfo.plateName][itemInfo.srcWellID];
            }
        }
        private string GetWellStr(int wellID)
        {
            int rowIndex = wellID - 1;
            while (rowIndex >= Common.rowCnt)
                rowIndex -= Common.rowCnt;
            int colIndex = (wellID - 1) / Common.rowCnt;
            char rowID = (char)('A' + rowIndex);
            string sWell = string.Format("{0}{1:D2}", rowID, colIndex + 1);
            return sWell;
        }

        private string Format(PipettingInfo pipettingInfo, bool bReadable)
        {
            string srcWellID = bReadable ? GetWellStr(pipettingInfo.srcWellID) : pipettingInfo.srcWellID.ToString();
            string sDstWellID = pipettingInfo.dstWellID.ToString();

            if (mix2plateKeywords.Contains(pipettingInfo.dstLabware)) //mix 2 96 plate
            {
                sDstWellID = Common.GetWellDesc(pipettingInfo.dstWellID);
            }
            else //24 or 16
            {
                if (GlobalVars.LabwareWellCnt == 24)
                {
                    int rowIndex = (pipettingInfo.dstWellID - 1) / 6;
                    int colIndex = pipettingInfo.dstWellID - rowIndex * 6 - 1;
                    sDstWellID = string.Format("{0}{1}", (char)('A' + rowIndex), colIndex + 1);
                }
            }

            if (bReadable)
                return string.Format("{0},{1},{2},{3},{4},{5}",
                    pipettingInfo.sPrimerID,
                    pipettingInfo.srcLabware,
                    srcWellID,
                    pipettingInfo.dstLabware,
                    sDstWellID, pipettingInfo.vol);

            else
                return string.Format("{0},{1},{2},{3},{4},{5}",
                    pipettingInfo.srcLabware,
                    Common.GetWellDesc(pipettingInfo.srcWellID),
                    pipettingInfo.dstLabware,
                    sDstWellID, pipettingInfo.vol, pipettingInfo.sPrimerID);
        }

        private List<string> Format(List<PipettingInfo> pipettingInfos, bool bReadable = false)
        {
            List<string> strs = new List<string>();
            foreach (var pipettingInfo in pipettingInfos)
            {
                strs.Add(Format(pipettingInfo, bReadable));
            }
            return strs;
        }

        public List<PipettingInfo> Generate(List<ItemInfo> items, string sFile)
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
            allPipettings.ForEach(x => strs.AddRange(GenerateGWL(x)));
            File.WriteAllLines(sFile, strs);
            return allPipettings;
            Console.WriteLine(string.Format("csv has been generated into:{0}.",sFile));
        }

        
        private string GetAspirate(string sLabware, int srcWellID, double vol)
        {
            string sAspirate = string.Format("A;{0};;;{1};;{2};;;",
                         sLabware,
                         srcWellID,
                         vol);
            return sAspirate;
        }

        private string GetDispense(string sLabware, int dstWellID, double vol)
        {
            string sDispense = string.Format("D;{0};;;{1};;{2};;;",
              sLabware,
              dstWellID,
              vol);
            return sDispense;
        }
       
        private List<string> GenerateGWL(PipettingInfo pipettingInfo)
        {
            List<string> strs = new List<string>();
            string asp = GetAspirate(pipettingInfo.srcLabware, pipettingInfo.srcWellID, pipettingInfo.vol);
            string disp = GetDispense(pipettingInfo.dstLabware, pipettingInfo.dstWellID, pipettingInfo.vol);
            string comment = string.Format("C;{0}", Common.GetWellDesc(pipettingInfo.srcWellID));
            strs.Add(comment);
            strs.Add(asp);
            strs.Add(disp);
            strs.Add("W;");
            return strs;
        }


        //private string Format(PipettingInfo pipettingInfo)
        //{
        //    return string.Format("{0},{1},{2},{3},{4},{5}",
        //            pipettingInfo.srcLabware,
        //            Common.GetWellDesc(pipettingInfo.srcWellID),
        //            pipettingInfo.dstLabware,
        //            pipettingInfo.dstWellID,
        //            pipettingInfo.vol,
        //            pipettingInfo.sPrimerID);
        //}



        private void GetDestPlateNameAndWell(int curWellID, ref string dstLabware, ref int dstWellID)
        {
            int labwareIndex = (curWellID - 1) / GlobalVars.LabwareWellCnt;
            int wellIndexInLabware = curWellID - 1 - labwareIndex * GlobalVars.LabwareWellCnt;
            dstLabware = string.Format("dst{0}", labwareIndex + 1);
            dstWellID = wellIndexInLabware + 1;
        }

        public List<List<string>> GetWellPrimerID(List<PipettingInfo> pipettingInfos, bool mixtoPlate = false)
        {

            List<List<string>> well_PrimerIDsList = new List<List<string>>();
            var labwares = pipettingInfos.GroupBy(x => x.dstLabware).Select(x => x.Key).ToList();
            List<List<string>> all16PrimerIDs = new List<List<string>>();
            List<string> all16Labwares = new List<string>();

            List<List<string>> all96PrimerIDs = new List<List<string>>();
            List<string> all96Labwares = new List<string>();
            foreach (var labware in labwares)
            {
                var thisLabwarePipettingInfos = pipettingInfos.Where(p => p.dstLabware == labware);
                var groupedPipettingInfo = thisLabwarePipettingInfos.GroupBy(info => info.dstWellID).ToList();
                List<IGrouping<int, PipettingInfo>> sortedPipettingInfo = groupedPipettingInfo.OrderBy(x => x.First().dstWellID).ToList();
                List<string> primerIDs = new List<string>();
                foreach (var sameGroupPipettingInfo in sortedPipettingInfo)
                {
                    primerIDs.Add(GetPrimerID(sameGroupPipettingInfo));
                }

                if (mix2plateKeywords.Contains(labware))     //96
                {
                    all96Labwares.Add(labware);
                    all96PrimerIDs.Add(primerIDs);
                }
                else //16 or 24
                {
                    if (GlobalVars.LabwareWellCnt == 16)
                    {
                        all16Labwares.Add(labware);
                        all16PrimerIDs.Add(primerIDs);
                    }
                    else
                    {
                        well_PrimerIDsList.Add(Format24WellPlate(labware, primerIDs));
                    }
                }
            }
            if (all16Labwares.Count > 0)
                well_PrimerIDsList.Add(Format16Pos(all16Labwares, all16PrimerIDs));
            return mixtoPlate ? FormatMicroPlatePos(all96Labwares, all96PrimerIDs) : well_PrimerIDsList;
        }

        private List<List<string>> FormatMicroPlatePos(List<string> all96Labwares, List<List<string>> all96PrimerIDs)
        {
            List<List<string>> allStrs = new List<List<string>>();
            string header = GetPlateHeader4EachColumn(); 

            for (int i = 0; i < all96Labwares.Count; i++)
            {
                List<string> strs = new List<string>();
                strs.Add(all96Labwares[i]);
                strs.Add(header);
                List<string> rowLines = new List<string>(Common.rowCnt);
                for (int line = 0; line < Common.rowCnt; line++)
                    rowLines.Add(string.Format("{0},", (char)('A' + line)));
                var primerIDs = all96PrimerIDs[i];
                for (int j = 0; j < primerIDs.Count; j++)
                {
                    int r = j + 1;
                    while (r > Common.rowCnt)
                        r -= Common.rowCnt;
                    rowLines[r - 1] += primerIDs[j] + ",";
                }
                strs.AddRange(rowLines);
                allStrs.Add(strs);
            }
            return allStrs;
        }

        private string GetPlateHeader4EachColumn()
        {
            //",1,2,3,4,5,6,7,8,9,10,11,12";
            string s = "";
            for(int i = 0; i< Common.colCnt;i++)
            {
                s += string.Format(",{0}", i + 1);
            }
            return s;
        }

        private List<string> Format16Pos(List<string> allLabwares, List<List<string>> allPrimerIDs)
        {
            List<string> strs = new List<string>();
            string sLabwareHeader = ",";
            allLabwares.ForEach(x => sLabwareHeader = sLabwareHeader + x + ",");
            strs.Add(sLabwareHeader);
            List<string> SixteenLines = new List<string>(16);
            for (int i = 0; i < 16; i++)
            {
                SixteenLines.Add(string.Format("{0},", i + 1));
                string thisLine = "";
                foreach (List<string> eachLabwarePrimerIDs in allPrimerIDs)
                {
                    if (eachLabwarePrimerIDs.Count > i)
                        thisLine += eachLabwarePrimerIDs[i] + ",";
                }
                SixteenLines[i] += thisLine;
            }
            strs.AddRange(SixteenLines);
            return strs;
        }

        private List<string> Format24WellPlate(string dstLabware, List<string> primerIDs)
        {
            List<string> strs = new List<string>();
            strs.Add(dstLabware);
            strs.Add(",1,2,3,4,5,6,");
            List<string> fourLines = new List<string>(4);
            for (int i = 0; i < 4; i++)
                fourLines.Add(string.Format("{0},", (char)('A' + i)));

            for (int i = 0; i < primerIDs.Count; i++)
            {
                int r = i + 1;
                while (r > 4)
                    r -= 4;
                fourLines[r - 1] += primerIDs[i] + ",";
            }
            strs.AddRange(fourLines);
            return strs;
        }

        private string GetPrimerID(IGrouping<int, PipettingInfo> sameGroupPipettingInfo)
        {

            if (sameGroupPipettingInfo.Count() == 1)
                return sameGroupPipettingInfo.FirstOrDefault().sPrimerID;
            var pipettingsInfo = sameGroupPipettingInfo.OrderBy(x => x.sPrimerID).ToList();
            string first = pipettingsInfo.First().sPrimerID;
            string last = pipettingsInfo.Last().sPrimerID;
            int underlinePos = first.IndexOf("_");
            string suffixLast = last.Substring(underlinePos + 1);
            return first + "-" + suffixLast;

        }

        public void AdjustLabwareLabels(List<PipettingInfo> pipettingInfos, List<string> batchPlateNames, bool adjustSrc)
        {
            List<PipettingInfo> orgInfos = new List<PipettingInfo>(pipettingInfos);
            List<string> labwares = null;
            if (adjustSrc)
            {
                labwares = batchPlateNames;
            }
            else
            {
                labwares = pipettingInfos.GroupBy(x => x.dstLabware).Select(x => x.Key).Where(x => !mix2plateKeywords.Contains(x)).ToList();
            }
            int curID = 1;
            foreach (var labware in labwares)
            {
                string prefix = adjustSrc ? "src" : "dst";
                string map2Labware = string.Format("{0}{1}", prefix, curID);
                curID++;
                for (int i = 0; i < pipettingInfos.Count; i++)
                {
                    if (adjustSrc)
                    {
                        if (pipettingInfos[i].srcLabware == labware)
                        {
                            pipettingInfos[i].srcLabware = map2Labware;
                        }
                    }
                    else
                    {
                        if (pipettingInfos[i].dstLabware == labware)
                        {
                            pipettingInfos[i].dstLabware = map2Labware;
                        }
                    }
                }
            }

        }
    }
}
