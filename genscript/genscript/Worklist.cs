using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Diagnostics;

namespace genscript
{
    class Worklist
    {
        int maxVol = 200;
        static int usedWells = 0;
        HashSet<int> processedSrcWells = new HashSet<int>();
        List<string> mix2plateKeywords = new List<string>() { "Start", "End", "Mix" };



        public List<string> GenerateWorklist(List<ItemInfo> itemsInfo,
            List<string> readableOutput, ref List<PipettingInfo> allPipettingInfos,
            ref List<string> multiDispenseOptGWL)
        {
            List<PipettingInfo> pipettingInfos = GetPipettingInfos(itemsInfo);
            pipettingInfos = pipettingInfos.OrderBy(x => GetOrderString(x)).ToList();
            //pipettingInfos = SortByDstWell(pipettingInfos);
#if DEBUG

#else
            CheckLabwareExists(pipettingInfos);
#endif
            pipettingInfos = SplitPipettingInfos(pipettingInfos);
            allPipettingInfos.AddRange(CloneInfos(pipettingInfos));

            //if (Common.Mix2Plate)
            //{
            //    AdjustLabwareLabels(pipettingInfos, true);
            //    AdjustLabwareLabels(pipettingInfos, false);
            //}

            #region generate opt multi dispense gwl
            List<PipettingInfo> bigVols =new List<PipettingInfo>();
            List<PipettingInfo> normalVols =new List<PipettingInfo>();
            SplitByVolume(pipettingInfos,bigVols,ref normalVols);
            multiDispenseOptGWL = GenerateWorklist(bigVols, normalVols);
            #endregion
            //pipettingInfos = pipettingInfos.OrderBy(x => x.srcLabware + x.sPrimerID).ToList();
            List<PipettingInfo> optimizedPipettingInfos = OptimizeCommandsSinglePlate(pipettingInfos);
            List<string> strs = Format(optimizedPipettingInfos);
            //well_PrimerIDsList.AddRange(GetWellPrimerID(pipettingInfos));
            readableOutput.AddRange(Format(pipettingInfos, true));
            return strs;
        }

        private List<PipettingInfo> SortByDstWell(List<PipettingInfo> pipettingInfos)
        {
            List<PipettingInfo> sortedInfos = new List<PipettingInfo>();
            while(pipettingInfos.Count > 0)
            {
                int orgWellID = pipettingInfos.First().orgDstWellID;
                var sameDstWellInfos = pipettingInfos.Where(x => IsNeededDstWellID(x,orgWellID));
                pipettingInfos = pipettingInfos.Except(sameDstWellInfos).ToList();
                sortedInfos.AddRange(SortByDstLabware(sameDstWellInfos));
            }
            return sortedInfos;
        }

        private bool IsNeededDstWellID(PipettingInfo x, int orgWellID)
        {
            if (x.dstLabware.Contains("dst"))
                return x.orgDstWellID == orgWellID;
            else
                return x.dstWellID == orgWellID;
        }

        private IEnumerable<PipettingInfo> SortByDstLabware(IEnumerable<PipettingInfo> toSort)
        {
            List<PipettingInfo> sorted = new List<PipettingInfo>();
            var start = toSort.Where(x => x.dstLabware.Contains("Start"));
            var end = toSort.Where(x => x.dstLabware.Contains("End"));
            var dst = toSort.Where(x => x.dstLabware.Contains("dst")).ToList(); ;
            var others = toSort.Except(start).Except(end).Except(dst);
            sorted.AddRange(start);
            sorted.Add(dst.First());
            sorted.AddRange(others);
            sorted.Add(dst[1]);
            sorted.AddRange(end);
            return sorted;
           
        }

     

        public List<List<string>> OptimizeThenFormat(List<PipettingInfo> pipettingInfos,bool generateGWL)
        {
            List<List<PipettingInfo>> optimizedPipettingInfos = OptimizeCommands(pipettingInfos);
            List<List<string>> eachPlatePipettingInfos = new List<List<string>>();
            if(generateGWL)
                optimizedPipettingInfos.ForEach(x => eachPlatePipettingInfos.Add(GenerateGWL(x)));
            else
                optimizedPipettingInfos.ForEach(x => eachPlatePipettingInfos.Add(Format(x)));

            return eachPlatePipettingInfos;
        }

        private List<string> GenerateGWL(List<PipettingInfo> pipettingInfos)
        {
            List<string> strs = new List<string>();
            pipettingInfos.ForEach(x=>strs.AddRange(GenerateGWL(x)));
            return strs;
        }

        private List<string> GenerateGWL(PipettingInfo pipettingInfo)
        {
            List<string> strs = new List<string>();
            string asp = GetAspirate(pipettingInfo.srcLabware, pipettingInfo.srcWellID, pipettingInfo.vol);
            string disp = GetDispense(pipettingInfo.dstLabware, pipettingInfo.dstWellID, pipettingInfo.vol);
            strs.Add(asp);
            strs.Add(disp);
            strs.Add("W;");
            return strs;
        }

        private int GetOrderString(PipettingInfo x)
        {
            //Dictionary<string, string> sortMap = new Dictionary<string, string>();
            //sortMap.Add("Mix", "01");
            //sortMap.Add("Start", "02");
            //sortMap.Add("End", "03");
            //string mappedLabware = x.dstLabware;
            //if (sortMap.ContainsKey(mappedLabware))
            //    mappedLabware = sortMap[mappedLabware];
            //else //dst01 dst02...
            //{
                
            //    if(mappedLabware.Contains("dst"))
            //    {
            //        mappedLabware = mappedLabware.Replace("dst", "");
            //        int val = int.Parse(mappedLabware);
            //        if(val < 10)
            //        {
            //            mappedLabware += "x";
            //        }
            //    }
            //}
            int labwareMappedInt = 0;
            if (x.dstLabware.Contains("Start"))
                labwareMappedInt = 1;
            else if (x.dstLabware.Contains("Mix"))
                labwareMappedInt = 2;
            else if (x.dstLabware.Contains("End"))
                labwareMappedInt = 3;
            string[] strs = x.sPrimerID.Split('-');
            string sDigit = "";
            foreach(var ch in strs[0])
            {
                if( char.IsDigit(ch))
                {
                    sDigit += ch;
                }
            }
            return int.Parse(sDigit) + labwareMappedInt;
        }

        private IEnumerable<PipettingInfo> CloneInfos(List<PipettingInfo> pipettingInfos)
        {
            List<PipettingInfo> clonedInfos = new List<PipettingInfo>();
            pipettingInfos.ForEach(x=>clonedInfos.Add(new PipettingInfo(x)));
            return clonedInfos;
        }

        public List<List<string>> GetWellPrimerID(List<PipettingInfo> pipettingInfos,bool mixto96 = false)
        {
            
            List<List<string>> well_PrimerIDsList = new List<List<string>>();
            var labwares = pipettingInfos.GroupBy(x => x.dstLabware).Select(x => x.Key).ToList();
            List<List<string>> all16PrimerIDs = new List<List<string>>();
            List<string> all16Labwares = new List<string>();

            List<List<string>> all96PrimerIDs = new List<List<string>>();
            List<string> all96Labwares = new List<string>();
            foreach(var labware in labwares)
            {
                var thisLabwarePipettingInfos = pipettingInfos.Where(p => p.dstLabware == labware);
                var groupedPipettingInfo = thisLabwarePipettingInfos.GroupBy(info => info.dstWellID).ToList();
                List<IGrouping<int,PipettingInfo>> sortedPipettingInfo = groupedPipettingInfo.OrderBy(x => x.First().dstWellID).ToList();
                List<string> primerIDs = new List<string>();
                foreach (var sameGroupPipettingInfo in sortedPipettingInfo)
                {
                    primerIDs.Add(GetPrimerID(sameGroupPipettingInfo));
                }
         
                if(mix2plateKeywords.Contains(labware))     //96
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
            if(all16Labwares.Count > 0)
                well_PrimerIDsList.Add(Format16Pos(all16Labwares, all16PrimerIDs));
            return mixto96 ? Format96Pos(all96Labwares, all96PrimerIDs) : well_PrimerIDsList;
        }

        private List<List<string>> Format96Pos(List<string> all96Labwares, List<List<string>> all96PrimerIDs)
        {
            List<List<string>> allStrs = new List<List<string>>();
            string header = ",1,2,3,4,5,6,7,8,9,10,11,12";
            for (int i = 0; i < all96Labwares.Count; i++)
            {
                List<string> strs = new List<string>();
                strs.Add(all96Labwares[i]);
                strs.Add(header);
                List<string> eightLines = new List<string>(8);
                for (int line = 0; line < 8; line++)
                    eightLines.Add(string.Format("{0},", (char)('A' + line)));
                var primerIDs = all96PrimerIDs[i];
                for (int j = 0; j < primerIDs.Count; j++)
                {
                    int r = j + 1;
                    while (r > 8)
                        r -= 8;
                    eightLines[r - 1] += primerIDs[j] + ",";
                }
                strs.AddRange(eightLines);
                allStrs.Add(strs);
            }
            return allStrs;
        }

        private List<string> Format16Pos(List<string> allLabwares, List<List<string>> allPrimerIDs)
        {
            List<string> strs = new List<string>();
            string sLabwareHeader = ",";
            allLabwares.ForEach(x =>sLabwareHeader= sLabwareHeader + x + ",");
            strs.Add(sLabwareHeader);
            List<string> SixteenLines = new List<string>(16);
            for (int i = 0; i < 16; i++)
            {
                SixteenLines.Add(string.Format("{0},", i+1));
                string thisLine = "";
                foreach (List<string> eachLabwarePrimerIDs in allPrimerIDs)
                {
                    if(eachLabwarePrimerIDs.Count > i)
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
                int r = i+1;
                while (r > 4)
                    r -= 4;
                fourLines[r - 1] += primerIDs[i] + ",";
            }
            strs.AddRange(fourLines);
            return strs;
        }

        private string GetPrimerID(IGrouping<int,PipettingInfo> sameGroupPipettingInfo)
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


        private List<string> GenerateWorklist(List<PipettingInfo> bigVols, List<PipettingInfo> normalVols)
        {
            List<string> strs = new List<string>();
            for (int col = 0; col < 12; col++)
            {
                int startID = col * 8 + 1;
                int endID = startID + 7;
                for (int ID = startID; ID <= endID; ID++)
                {
                    if (!normalVols.Exists(x => x.srcWellID == ID))
                        continue;
                    List<PipettingInfo> batchPipettingInfos = normalVols.Where(x => x.srcWellID == ID).ToList();
                    strs.AddRange(GenerateWorklistSameBatch(batchPipettingInfos));
                    normalVols = normalVols.Except(batchPipettingInfos).ToList();
                }
            }
            foreach (var pipettingInfo in bigVols)
            {
                strs.AddRange(GenerateWorklistSameBatch(new List<PipettingInfo>() { pipettingInfo }));
            }
            return strs;
        }

        private void SplitByVolume(List<PipettingInfo> pipettingInfos,
            List<PipettingInfo> bigVols, 
            ref List<PipettingInfo> normalVols)
        {
            normalVols = new List<PipettingInfo>(pipettingInfos);
            bigVols = normalVols.Where(x => x.vol == maxVol).ToList();
            normalVols = normalVols.Except(bigVols).ToList();
        }

        private void CheckLabwareExists(List<PipettingInfo> pipettingInfos)
        {
            EVOScriptReader scriptReader = new EVOScriptReader();
            if (scriptReader.sScriptFile == "")
                return;
            List<string> srcLabwares = pipettingInfos.Select(x => x.srcLabware).Distinct().ToList();
            List<string> dstLabwares = pipettingInfos.Select(x => x.dstLabware).Distinct().ToList();
            List<string> needesLabwares = srcLabwares.Union(dstLabwares).ToList();
            
            bool exists = needesLabwares.All(x => scriptReader.Labwares.Contains(x));
            if (!exists)
            {
                foreach (string needLabel in needesLabwares)
                {
                    if (!scriptReader.Labwares.Contains(needLabel))
                        throw new Exception(string.Format("Labware {0} doesnot exist in the scrpit!", needLabel));
                }
            }
        }

        private List<PipettingInfo> OptimizeCommandsSinglePlate(List<PipettingInfo> pipettingInfos)
        {
            List<PipettingInfo> tmpPipettingInfos = new List<PipettingInfo>(pipettingInfos);
            List<PipettingInfo> allOptimizedPipettingInfos = new List<PipettingInfo>();
           
            string firstPlateName = pipettingInfos.First().srcLabware;
            string secondPlateName = pipettingInfos.Last().srcLabware;
            List<string> plateNames = new List<string>();
            plateNames.Add(firstPlateName);
            plateNames.Add(secondPlateName);
            for (int times = 0; times < 2; times++)
            {
                string curPlateName = plateNames[times];
                for (int col = 0; col < 12; col++)
                {
                    int startID = col * 8 + 1;
                    int endID = startID + 7;
                    for (int ID = startID; ID <= endID; ID++)
                    {
                        if (!tmpPipettingInfos.Exists(x => x.srcWellID == ID && x.srcLabware == curPlateName))
                            continue;
                        var pipettingInfo = tmpPipettingInfos.First(x => x.srcWellID == ID && x.srcLabware == curPlateName);

                        allOptimizedPipettingInfos.Add(pipettingInfo);
                        tmpPipettingInfos = tmpPipettingInfos.Except(new List<PipettingInfo>() { pipettingInfo }).ToList();
                    }
                }
            }
            allOptimizedPipettingInfos.AddRange(tmpPipettingInfos.OrderBy(x => x.srcLabware + x.srcWellID.ToString()));
            return allOptimizedPipettingInfos;
        }

        private List<List<PipettingInfo>> OptimizeCommands(List<PipettingInfo> pipettingInfos)
        {
            List<PipettingInfo> tmpPipettingInfos = new List<PipettingInfo>(pipettingInfos);
            List<List<PipettingInfo>> allOptimizedPipettingInfos = new List<List<PipettingInfo>>();
            if (Common.Mix2Plate)
            {
                var srcLabwares = tmpPipettingInfos.GroupBy(x => x.srcLabware).Select(x => x.Key).ToList();
                foreach (var srcLabware in srcLabwares)
                {
                    List<PipettingInfo> sameSrcPlatePipettingInfo = tmpPipettingInfos.Where(x => x.srcLabware == srcLabware).ToList();
                    List<PipettingInfo> thisPlateOptPipettingInfos = new List<PipettingInfo>();
                    for (int i = 0; i < 96; i++)
                    {
                        List<PipettingInfo> expectedItems = sameSrcPlatePipettingInfo.Where(x => x.srcWellID == i + 1).ToList();
                        if (expectedItems.Count == 0)
                            continue;
                        var theOne = expectedItems.OrderBy(x => GetOrderString(x)).First();
                        if (theOne != null)
                        {
                            //var theOne = expectedItems.First();
                            sameSrcPlatePipettingInfo.Remove(theOne);
                            thisPlateOptPipettingInfos.Add(theOne);
                        }
                    }
                    thisPlateOptPipettingInfos.AddRange(sameSrcPlatePipettingInfo);
                    allOptimizedPipettingInfos.Add(thisPlateOptPipettingInfos);
                }
                return allOptimizedPipettingInfos;
            }
            else
                throw new Exception("you must be kidding me, I am only for mix2Plate");
        }

        private List<string> Format(List<PipettingInfo> pipettingInfos,bool bReadable = false)
        {
            List<string> strs = new List<string>();
            foreach (var pipettingInfo in pipettingInfos)
            {
                strs.Add(Format(pipettingInfo, bReadable));
            }
            return strs;
        }

        private string Format(PipettingInfo pipettingInfo, bool bReadable)
        {
            string srcWellID = bReadable ? GetWellStr(pipettingInfo.srcWellID) : pipettingInfo.srcWellID.ToString();
            string sDstWellID = pipettingInfo.dstWellID.ToString();

            if(mix2plateKeywords.Contains(pipettingInfo.dstLabware)) //mix 2 96 plate
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
                    sDstWellID, pipettingInfo.vol,pipettingInfo.sPrimerID);
        }

        private List<PipettingInfo> SplitPipettingInfos(List<PipettingInfo> pipettingInfos)
        {
            List<PipettingInfo> newInfos = new List<PipettingInfo>();
            foreach (var pipettingInfo in pipettingInfos)
            {
                PipettingInfo tmp = new PipettingInfo(pipettingInfo);
                while (tmp.vol > maxVol)
                {
                    PipettingInfo sliceInfo = new PipettingInfo(tmp);
                    sliceInfo.vol = maxVol;
                    newInfos.Add(sliceInfo);
                    tmp.vol -= maxVol;
                }
                if (tmp.vol != 0)
                    newInfos.Add(tmp);
            }
            return newInfos;
        }

        private List<string> GenerateWorklist(List<PipettingInfo> pipettingInfos)
        {
            List<string> strs = new List<string>();
            while (pipettingInfos.Count > 0)
            {
                PipettingInfo first = pipettingInfos.First();
                List<PipettingInfo> batchPipettingInfos = pipettingInfos.Where(x => x.srcWellID == first.srcWellID).ToList();
                List<PipettingInfo> maxVolThisBatch = batchPipettingInfos.Where(x => x.vol == maxVol).ToList();
                foreach (var pipettingInfo in maxVolThisBatch)
                {
                    strs.AddRange(GenerateWorklistSameBatch(new List<PipettingInfo>() { pipettingInfo }));
                }
                pipettingInfos = pipettingInfos.Except(batchPipettingInfos).ToList();
                batchPipettingInfos = batchPipettingInfos.Except(maxVolThisBatch).ToList();
                strs.AddRange(GenerateWorklistSameBatch(batchPipettingInfos));
            }
            return strs;
        }

        private string GetWellStr(int wellID)
        {
            int rowIndex = wellID - 1;
            while (rowIndex >= 8)
                rowIndex -= 8;
            int colIndex = (wellID - 1) / 8;
            char rowID = (char)('A' + rowIndex);
            string sWell = string.Format("{0}{1:D2}", rowID, colIndex + 1);
            return sWell;
        }

        private List<string> GenerateWorklistSameBatch(List<PipettingInfo> batchPipettingInfos)
        {
            List<string> strs = new List<string>();
            var pipettingInfo = batchPipettingInfos.First();

            int rowIndex = pipettingInfo.srcWellID - 1;
            while (rowIndex >= 8)
                rowIndex -= 8;
            int colIndex = (pipettingInfo.srcWellID - 1) / 8;
            char rowID = (char)('A' + rowIndex);
            string comment = string.Format("C;{0}{1:D2}", rowID, colIndex + 1);
            //if( !processedSrcWells.Contains(pipettingInfo.srcWellID))
            strs.Add(comment);
            processedSrcWells.Add(pipettingInfo.srcWellID);

            if (batchPipettingInfos.Count == 1)
            {
                string sAsp = "";
                string sDisp = "";
                sAsp = GetAspirate(pipettingInfo.srcLabware, pipettingInfo.srcWellID, pipettingInfo.vol);
                sDisp = GetDispense(pipettingInfo.dstLabware, pipettingInfo.dstWellID, pipettingInfo.vol);
                strs.Add(sAsp);
                strs.Add(sDisp);
            }
            else
            {
               
                double v = batchPipettingInfos.Sum(x => x.vol);
                //Debug.Assert(v <= maxVol);
                if (v > maxVol)
                    Debug.WriteLine("vol > 200!");
                string sAsp = "";
                string sDisp = "";
                sAsp = GetAspirate(pipettingInfo.srcLabware, pipettingInfo.srcWellID, v);
                strs.Add(sAsp);
                foreach (var tempInfo in batchPipettingInfos)
                {
                    sDisp = GetDispense(tempInfo.dstLabware, tempInfo.dstWellID, tempInfo.vol);
                    strs.Add(sDisp);
                }
            }
            strs.Add("W;");
            return strs;
        }
        
        private List<PipettingInfo> GetPipettingInfos(List<ItemInfo> itemsInfo)
        {
            List<PipettingInfo> pipettingInfos = new List<PipettingInfo>();
            List<ItemInfo> itemsInfoCopy = new List<ItemInfo>(itemsInfo);
            FillVols(itemsInfoCopy);
            //if(Common.Mix2Plate)
            //{
            //    int nRemovedCnt = itemsInfo.RemoveAll(x => !IsFixedPosRange(x.sExtraDescription));
            //    itemsInfoCopy = itemsInfo.OrderBy(x => 100 * Common.GetWellID(x.sExtraDescription) + x.subID).ToList();
            //}
            while (itemsInfoCopy.Count > 0)
            {
                var first = itemsInfoCopy.First();
                int realStartSubIDOfThisBatch = first.subID;
                
                //List<ItemInfo> sameMainIDItems = itemsInfoCopy.Where(x => x.mainID == first.mainID && x.plateName == first.plateName && x.sExtraDescription == first.sExtraDescription).ToList();
                List<ItemInfo> sameMainIDItems = itemsInfoCopy.Where(x => x.mainID == first.mainID  &&
                    x.sExtraDescription == first.sExtraDescription).ToList();
                sameMainIDItems = CheckSequential(first,sameMainIDItems);
                itemsInfoCopy = itemsInfoCopy.Except(sameMainIDItems).ToList();
                List<StartEnd> ranges = ParseRanges(first.sExtraDescription, realStartSubIDOfThisBatch);
                List<ItemInfo> allRangeItems = new List<ItemInfo>();
                foreach (StartEnd range in ranges)
                {
                    List<ItemInfo> sameRangeItems = sameMainIDItems.Where(x => InRange(x, range)).ToList();
                    if (sameRangeItems.Count > 0)
                        AddPipettingInfo4ThisRange(pipettingInfos, sameRangeItems, range);
                    allRangeItems.AddRange(sameRangeItems);
                }
                allRangeItems = allRangeItems.Distinct().ToList();
                sameMainIDItems = sameMainIDItems.Except(allRangeItems).ToList();
                foreach (var item in sameMainIDItems)
                {
                    AddPipettingInfo(pipettingInfos, item, false);
                    usedWells++;
                }
            }
          
            return pipettingInfos;
        }

        public void AdjustLabwareLabels(List<PipettingInfo> pipettingInfos,List<string> batchPlateNames, bool adjustSrc)
        {
            List<PipettingInfo> orgInfos = new List<PipettingInfo>(pipettingInfos);
            List<string> labwares = null;
            if (adjustSrc)
            {
                labwares = batchPlateNames;                
            }
            else
            {
                labwares = pipettingInfos.GroupBy(x => x.dstLabware).Select(x => x.Key).Where(x=>!mix2plateKeywords.Contains(x)).ToList();
            }
            int curID = 1;
            foreach (var labware in labwares)
            {
                string prefix = adjustSrc ? "src" : "dst";
                string map2Labware = string.Format("{0}{1}",prefix, curID);
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


        private List<ItemInfo> CheckSequential(ItemInfo first, List<ItemInfo> sameMainIDItems)
        {
            List<ItemInfo> seqItems = new List<ItemInfo>();
            int expectedID = first.subID;
            bool bSeq = true;
            seqItems.Add(first);
            while (bSeq)
            {
                expectedID++;
                bSeq = sameMainIDItems.Exists(x => x.subID == expectedID);
                if (bSeq)
                    seqItems.Add(sameMainIDItems.Find(x => x.subID == expectedID));
            }
            return seqItems;
        }

     
        private bool IsFixedPosRange(string sExtraDescription)
        {
            if (sExtraDescription.Length > 3)
                return false;

            char firstChar = sExtraDescription.First();
            if (firstChar < 'A' || firstChar > 'H')
                return false;
            sExtraDescription = sExtraDescription.Substring(1);
            int val = 0;
            return int.TryParse(sExtraDescription, out val);
      
        }

        private void AddPipettingInfo4FixedRange(List<PipettingInfo> pipettingInfos, List<ItemInfo> sameMainIDItems)
        {
            var first = sameMainIDItems.First();
            AddPipettingInfoFixedPlate(pipettingInfos, first, true, true);
            AddPipettingInfo(pipettingInfos, first,true,true);
            usedWells++;
            
            var last = sameMainIDItems.Last();
            AddPipettingInfoFixedPlate(pipettingInfos, last, true, false);
            AddPipettingInfo(pipettingInfos, last,true,true);
            usedWells++;
            foreach (var item in sameMainIDItems)
            {
                AddPipettingInfoFixedPlate(pipettingInfos, item, false, false);
            }

        }


        private void AddPipettingInfo4ThisRange(List<PipettingInfo> pipettingInfos, 
            List<ItemInfo> sameRangeItems,
            StartEnd range)
        {
            if (IsOnBoundary(sameRangeItems.First(), range)) 
            {
                AddPipettingInfo(pipettingInfos, sameRangeItems.First(), true);
                usedWells++;
            }
            foreach (var item in sameRangeItems)
                AddPipettingInfo(pipettingInfos, item);

            if (sameRangeItems.Count == 1)
                return;
            if (IsOnBoundary(sameRangeItems.Last(), range)) 
            {
                usedWells++;
                AddPipettingInfo(pipettingInfos, sameRangeItems.Last(), true);
                usedWells++;
            }
        }

        private bool IsOnBoundary(ItemInfo itemInfo, StartEnd range)
        {
            return itemInfo.subID == range.start || itemInfo.subID == range.end;
        }

        private bool InRange(ItemInfo x, StartEnd range)
        {
            return x.subID <= range.end && x.subID >= range.start;
        }

        private void FillVols(List<ItemInfo> itemsInfoCopy)
        {
            for (int i = 0; i < itemsInfoCopy.Count; i++)
            {
                var itemInfo = itemsInfoCopy[i];
                itemInfo.vol = OdSheet.eachPlateID_Vols[itemInfo.plateName][itemInfo.srcWellID];
            }
        }

        //use extra description as wellID
         private void AddPipettingInfoFixedPlate(List<PipettingInfo> pipettingInfos,
            ItemInfo itemInfo,bool isOnBoundary, bool isStart)
        {
            int vol = isOnBoundary ? int.Parse(ConfigurationManager.AppSettings["BoundaryFixedVolume"]) : itemInfo.vol;

            string dstLabware = "Mix";
            if (isOnBoundary)
            {
                dstLabware = isStart ? "Start" : "End";
            }

            PipettingInfo pipettingInfo = new PipettingInfo(itemInfo.sID,
                           itemInfo.plateName,
                           itemInfo.srcWellID,
                           dstLabware, Common.GetWellID(itemInfo.sExtraDescription), vol);
            pipettingInfos.Add(pipettingInfo);

        }


        private void AddPipettingInfo(List<PipettingInfo> pipettingInfos,
            ItemInfo itemInfo, bool bOnBoundary = false, bool isEPTube = false)
        {
            string dstLabware = "";
            int dstWellID = 0;
            int vol = bOnBoundary ? itemInfo.vol * 10 : itemInfo.vol;
            if (bOnBoundary)
            {
                if (isEPTube)
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("EPVolume"))
                    {
                        vol = int.Parse(ConfigurationManager.AppSettings["EPVolume"]);
                    }
                }
                else
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("BoundaryFixedVolume"))
                    {
                        vol = int.Parse(ConfigurationManager.AppSettings["BoundaryFixedVolume"]);
                    }
                }
            }
            CalculateDestPos(usedWells, ref dstLabware, ref dstWellID);
            PipettingInfo pipettingInfo = new PipettingInfo(itemInfo.sID,
                itemInfo.plateName, 
                itemInfo.srcWellID,
                dstLabware, dstWellID, vol);
            //pipettingInfo.orgDstWellID = Common.GetWellID(itemInfo.sExtraDescription);
            pipettingInfos.Add(pipettingInfo);

            Debug.WriteLine(string.Format("plateName:{0},srcWell{1},dstLabware {2},dstWellID {3}", itemInfo.plateName, itemInfo.srcWellID, dstLabware,dstWellID));
        }


        private List<StartEnd> ParseRanges(string s, int firstSubID)
        {
            if (s == "" || s == OperationSheet.empty)
                return new List<StartEnd>();
            s = GetMeaningfulRange(s);
            List<StartEnd> ranges = ParseMeaningfulRanges(s, firstSubID);
            return ranges;
        }

        private string GetMeaningfulRange(string s)
        {
            string sExtraDesc = s.Trim();
            int pos = sExtraDesc.IndexOf("**");
            int lastPos = sExtraDesc.LastIndexOf("**");
            if (lastPos != sExtraDesc.Length - 2)
                throw new Exception("Invalid remarks! Last two chars is NOT '*'");
            //if (pos == -1)
            //    throw new Exception("Invalid remarks! first two chars is NOT '*'");
            if (lastPos == pos)
                throw new Exception("Invalid remarks! Only one ** found!");
            sExtraDesc = sExtraDesc.Substring(pos + 2);
            sExtraDesc = sExtraDesc.Substring(0, sExtraDesc.Length - 2);
            return sExtraDesc;
        }

     
        private string GetAspirate(string sLabware,int srcWellID, double vol)
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
       

        private void CalculateDestPos(int usedWells,ref string slabwareID, ref int wellID)
        {
            
            int curWellID = usedWells + 1;
            int labwareID = (curWellID + GlobalVars.LabwareWellCnt - 1) / GlobalVars.LabwareWellCnt;
            slabwareID = string.Format("dst{0}", labwareID);
            wellID = curWellID;
            while (wellID > GlobalVars.LabwareWellCnt)
                wellID -= GlobalVars.LabwareWellCnt;

            if (GlobalVars.LabwareWellCnt == 24)
            {
                wellID = ConvertWellID(wellID);
            }
        }

        private static int ConvertWellID(int wellID)
        {
            int colIndex = (wellID - 1) / 4;
            int rowIndex = wellID - colIndex * 4 - 1;
            return rowIndex * 6 + colIndex + 1;
        }

     

        private List<StartEnd> ParseMeaningfulRanges(string sExtraDesc, int firstSubID)
        {
            string[] strs = sExtraDesc.Split('*');
            List<StartEnd> ranges = new List<StartEnd>();
            foreach (string s in strs)
            {
                ranges.Add(ParseRange(s, firstSubID));
            }
            return ranges;
        }

        private StartEnd ParseRange(string s, int firstSubID)
        {
            string[] strs = s.Split('-');
            int start = int.Parse(strs[0]);
            int end = int.Parse(strs[1]);
            StartEnd startEnd = new StartEnd(start + firstSubID -1 , end + firstSubID -1);
            return startEnd;
        }



        internal List<string> GetDestLabwares(List<PipettingInfo> allPipettingInfos)
        {
            var notMix2PlateItems = allPipettingInfos.Where(x => !mix2plateKeywords.Contains((x.dstLabware))).ToList();
            HashSet<string> itemNames = new HashSet<string>();
            notMix2PlateItems.ForEach(x => itemNames.Add(x.dstLabware));
            return new List<string>(itemNames);
        }
    }

    struct StartEnd
    {
        public int start;
        public int end;
        public StartEnd(int s, int e)
        {
            start = s;
            end = e;
        }
    }
}
