using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace genscript
{
	internal class Worklist
	{
		private int maxVol = 200;

		private static int usedEPTubes;

		private static int usedPlateWells;

		private HashSet<int> processedSrcWells = new HashSet<int>();

		private List<string> mix2plateKeywords = new List<string>
		{
			"Start",
			"End",
			"Mix"
		};

		public List<string> GenerateWorklist(List<ItemInfo> itemsInfo, List<string> readableOutput, ref List<PipettingInfo> allPipettingInfos, ref List<string> multiDispenseOptGWL)
		{
			List<PipettingInfo> list = this.GetPipettingInfos(itemsInfo);
			list = (from x in list
			orderby x.srcLabware + Common.FormatWellID(x.srcWellID)
			select x).ToList<PipettingInfo>();
			this.CheckLabwareExists(list);
			list = this.SplitPipettingInfos(list);
			allPipettingInfos.AddRange(this.CloneInfos(list));
			List<PipettingInfo> bigVols = new List<PipettingInfo>();
			List<PipettingInfo> normalVols = new List<PipettingInfo>();
			this.SplitByVolume(list, bigVols, ref normalVols);
			multiDispenseOptGWL = this.GenerateWorklist(bigVols, normalVols);
			List<string> result = this.OptimizeCommandsSinglePlate(list);
			readableOutput.AddRange(this.Format(list, true));
			return result;
		}

		private List<PipettingInfo> SortByDstWell(List<PipettingInfo> pipettingInfos)
		{
			List<PipettingInfo> list = new List<PipettingInfo>();
			while (pipettingInfos.Count > 0)
			{
				int orgWellID = pipettingInfos.First<PipettingInfo>().orgDstWellID;
				IEnumerable<PipettingInfo> enumerable = from x in pipettingInfos
				where this.IsNeededDstWellID(x, orgWellID)
				select x;
				pipettingInfos = pipettingInfos.Except(enumerable).ToList<PipettingInfo>();
				list.AddRange(this.SortByDstLabware(enumerable));
			}
			return list;
		}

		private bool IsNeededDstWellID(PipettingInfo x, int orgWellID)
		{
			if (x.dstLabware.Contains("dst"))
			{
				return x.orgDstWellID == orgWellID;
			}
			return x.dstWellID == orgWellID;
		}

		private IEnumerable<PipettingInfo> SortByDstLabware(IEnumerable<PipettingInfo> toSort)
		{
			List<PipettingInfo> list = new List<PipettingInfo>();
			IEnumerable<PipettingInfo> enumerable = from x in toSort
			where x.dstLabware.Contains("Start")
			select x;
			IEnumerable<PipettingInfo> enumerable2 = from x in toSort
			where x.dstLabware.Contains("End")
			select x;
			List<PipettingInfo> list2 = (from x in toSort
			where x.dstLabware.Contains("dst")
			select x).ToList<PipettingInfo>();
			IEnumerable<PipettingInfo> collection = toSort.Except(enumerable).Except(enumerable2).Except(list2);
			list.AddRange(enumerable);
			list.Add(list2.First<PipettingInfo>());
			list.AddRange(collection);
			list.Add(list2[1]);
			list.AddRange(enumerable2);
			return list;
		}

		public List<List<string>> OptimizeThenFormat(List<PipettingInfo> pipettingInfos, bool generateGWL)
		{
			List<List<PipettingInfo>> list = this.OptimizeCommands(pipettingInfos);
			List<List<string>> eachPlatePipettingInfos = new List<List<string>>();
			if (generateGWL)
			{
				list.ForEach(delegate(List<PipettingInfo> x)
				{
					eachPlatePipettingInfos.Add(this.GenerateGWL(x));
				});
			}
			else
			{
				list.ForEach(delegate(List<PipettingInfo> x)
				{
					eachPlatePipettingInfos.Add(this.Format(x, false));
				});
			}
			return eachPlatePipettingInfos;
		}

		public List<string> GenerateGWL(List<PipettingInfo> pipettingInfos)
		{
			List<string> strs = new List<string>();
			pipettingInfos.ForEach(delegate(PipettingInfo x)
			{
				strs.AddRange(this.GenerateGWL(x));
			});
			return strs;
		}

		private List<string> GenerateGWL(PipettingInfo pipettingInfo)
		{
			List<string> list = new List<string>();
			string aspirate = this.GetAspirate(pipettingInfo.srcLabware, pipettingInfo.srcWellID, pipettingInfo.vol);
			string dispense = this.GetDispense(pipettingInfo.dstLabware, pipettingInfo.dstWellID, pipettingInfo.vol);
			list.Add(aspirate);
			list.Add(dispense);
			list.Add("W;");
			return list;
		}

		private int GetOrderString(PipettingInfo x)
		{
			int num = 0;
			if (x.dstLabware.Contains("Start"))
			{
				num = 1;
			}
			else if (x.dstLabware.Contains("Mix"))
			{
				num = 2;
			}
			else if (x.dstLabware.Contains("End"))
			{
				num = 3;
			}
			string[] array = x.sPrimerID.Split(new char[]
			{
				'_'
			});
			return int.Parse(array[1]) + num;
		}

		private IEnumerable<PipettingInfo> CloneInfos(List<PipettingInfo> pipettingInfos)
		{
			List<PipettingInfo> clonedInfos = new List<PipettingInfo>();
			pipettingInfos.ForEach(delegate(PipettingInfo x)
			{
				clonedInfos.Add(new PipettingInfo(x));
			});
			return clonedInfos;
		}

		public List<List<string>> GetWellPrimerID(List<PipettingInfo> pipettingInfos, bool mixto96 = false)
		{
			List<List<string>> list = new List<List<string>>();
			List<string> list2 = (from x in pipettingInfos
			group x by x.dstLabware into x
			select x.Key).ToList<string>();
			List<List<string>> list3 = new List<List<string>>();
			List<string> list4 = new List<string>();
			List<List<string>> list5 = new List<List<string>>();
			List<string> list6 = new List<string>();
			using (List<string>.Enumerator enumerator = list2.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string labware = enumerator.Current;
					IEnumerable<PipettingInfo> source = from p in pipettingInfos
					where p.dstLabware == labware
					select p;
					List<IGrouping<int, PipettingInfo>> source2 = (from info in source
					group info by info.dstWellID).ToList<IGrouping<int, PipettingInfo>>();
					List<IGrouping<int, PipettingInfo>> list7 = (from x in source2
					orderby x.First<PipettingInfo>().dstWellID
					select x).ToList<IGrouping<int, PipettingInfo>>();
					List<string> list8 = new List<string>();
					foreach (IGrouping<int, PipettingInfo> current in list7)
					{
						list8.Add(this.GetPrimerID(current));
					}
					if (this.mix2plateKeywords.Contains(labware))
					{
						list6.Add(labware);
						list5.Add(list8);
					}
					else if (GlobalVars.LabwareWellCnt == 16)
					{
						list4.Add(labware);
						list3.Add(list8);
					}
					else
					{
						list.Add(this.Format24WellPlate(labware, list8));
					}
				}
			}
			if (list6.Count > 0)
			{
				list.AddRange(this.Format96Pos(list6, list5));
			}
			if (list4.Count > 0)
			{
				list.Add(this.Format16Pos(list4, list3));
			}
			return list;
		}

		private List<List<string>> Format96Pos(List<string> all96Labwares, List<List<string>> all96PrimerIDs)
		{
			List<List<string>> list = new List<List<string>>();
			string item = ",1,2,3,4,5,6,7,8,9,10,11,12";
			for (int i = 0; i < all96Labwares.Count; i++)
			{
				List<string> list2 = new List<string>();
				list2.Add(all96Labwares[i]);
				list2.Add(item);
				List<string> list3 = new List<string>(8);
				for (int j = 0; j < 8; j++)
				{
					list3.Add(string.Format("{0},", (char)(65 + j)));
				}
				List<string> list4 = all96PrimerIDs[i];
				for (int k = 0; k < list4.Count; k++)
				{
					int l;
					for (l = k + 1; l > 8; l -= 8)
					{
					}
					List<string> list5;
					int index;
					(list5 = list3)[index = l - 1] = list5[index] + list4[k] + ",";
				}
				list2.AddRange(list3);
				list.Add(list2);
			}
			return list;
		}

		private List<string> Format16Pos(List<string> allLabwares, List<List<string>> allPrimerIDs)
		{
			List<string> list = new List<string>();
			string sLabwareHeader = ",";
			allLabwares.ForEach(delegate(string x)
			{
				sLabwareHeader = sLabwareHeader + x + ",";
			});
			list.Add(sLabwareHeader);
			List<string> list2 = new List<string>(16);
			for (int i = 0; i < 16; i++)
			{
				list2.Add(string.Format("{0},", i + 1));
				string text = "";
				foreach (List<string> current in allPrimerIDs)
				{
					if (current.Count > i)
					{
						text = text + current[i] + ",";
					}
				}
				List<string> list3;
				int index;
				(list3 = list2)[index = i] = list3[index] + text;
			}
			list.AddRange(list2);
			return list;
		}

		private List<string> Format24WellPlate(string dstLabware, List<string> primerIDs)
		{
			List<string> list = new List<string>();
			list.Add(dstLabware);
			list.Add(",1,2,3,4,5,6,");
			List<string> list2 = new List<string>(4);
			for (int i = 0; i < 4; i++)
			{
				list2.Add(string.Format("{0},", (char)(65 + i)));
			}
			for (int j = 0; j < primerIDs.Count; j++)
			{
				int k;
				for (k = j + 1; k > 4; k -= 4)
				{
				}
				List<string> list3;
				int index;
				(list3 = list2)[index = k - 1] = list3[index] + primerIDs[j] + ",";
			}
			list.AddRange(list2);
			return list;
		}

		private string GetPrimerID(IGrouping<int, PipettingInfo> sameGroupPipettingInfo)
		{
			if (sameGroupPipettingInfo.Count<PipettingInfo>() == 1)
			{
				return sameGroupPipettingInfo.FirstOrDefault<PipettingInfo>().sPrimerID;
			}
			List<PipettingInfo> source = (from x in sameGroupPipettingInfo
			orderby this.GetSubID(x.sPrimerID)
			select x).ToList<PipettingInfo>();
			string sPrimerID = source.First<PipettingInfo>().sPrimerID;
			string sPrimerID2 = source.Last<PipettingInfo>().sPrimerID;
			if (source.First<PipettingInfo>().dstLabware != "Mix")
			{
				return sPrimerID;
			}
			int num = sPrimerID.IndexOf("_");
			string str = sPrimerID2.Substring(num + 1);
			return sPrimerID + "-" + str;
		}

		private int GetSubID(string s)
		{
			string[] array = s.Split(new char[]
			{
				'_'
			});
			return int.Parse(array[1]);
		}

		private List<string> GenerateWorklist(List<PipettingInfo> bigVols, List<PipettingInfo> normalVols)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < 12; i++)
			{
				int num = i * Common.rows384 + 1;
				int num2 = num + Common.rows384 - 1;
				int ID;
				for (ID = num; ID <= num2; ID++)
				{
					if (normalVols.Exists((PipettingInfo x) => x.srcWellID == ID))
					{
						List<PipettingInfo> list2 = (from x in normalVols
						where x.srcWellID == ID
						select x).ToList<PipettingInfo>();
						list.AddRange(this.GenerateWorklistSameBatch(list2));
						normalVols = normalVols.Except(list2).ToList<PipettingInfo>();
					}
				}
			}
			foreach (PipettingInfo current in bigVols)
			{
				list.AddRange(this.GenerateWorklistSameBatch(new List<PipettingInfo>
				{
					current
				}));
			}
			return list;
		}

		private void SplitByVolume(List<PipettingInfo> pipettingInfos, List<PipettingInfo> bigVols, ref List<PipettingInfo> normalVols)
		{
			normalVols = new List<PipettingInfo>(pipettingInfos);
			bigVols = (from x in normalVols
			where x.vol == (double)this.maxVol
			select x).ToList<PipettingInfo>();
			normalVols = normalVols.Except(bigVols).ToList<PipettingInfo>();
		}

		private void CheckLabwareExists(List<PipettingInfo> pipettingInfos)
		{
			EVOScriptReader scriptReader = new EVOScriptReader();
			if (scriptReader.sScriptFile == "")
			{
				return;
			}
			List<string> first = (from x in pipettingInfos
			select x.srcLabware).Distinct<string>().ToList<string>();
			List<string> second = (from x in pipettingInfos
			select x.dstLabware).Distinct<string>().ToList<string>();
			List<string> list = first.Union(second).ToList<string>();
			if (!list.All((string x) => scriptReader.Labwares.Contains(x)))
			{
				foreach (string current in list)
				{
					if (!scriptReader.Labwares.Contains(current))
					{
						throw new Exception(string.Format("Labware {0} doesnot exist in the scrpit!", current));
					}
				}
			}
		}

		public List<string> OptimizeCommandsSinglePlate(List<PipettingInfo> pipettingInfos)
		{
            //if (pipettingInfos.First<PipettingInfo>().dstLabware != "Mix")
            //{
            //	List<string> list = new List<string>();
            //	foreach (PipettingInfo current in pipettingInfos)
            //	{
            //		list.AddRange(this.GenerateGWL(current));
            //	}
            //	list.Add("B;");
            //	return list;
            //}
            //List<PipettingInfo> list2 = new List<PipettingInfo>(pipettingInfos);
            //List<string> list3 = new List<string>();
            //List<string> list4 = (from x in pipettingInfos
            //select x.srcLabware).Distinct<string>().ToList<string>();
            //List<PipettingInfo> list5 = new List<PipettingInfo>();
            //for (int i = 0; i < list4.Count; i++)
            //{
            //	string curPlateName = list4[i];
            //	for (int j = 0; j < Common.cols384; j++)
            //	{
            //		int num = j * Common.rows384 + 1;
            //		int num2 = num + Common.rows384 - 1;
            //		for (int k = 0; k < 2; k++)
            //		{
            //			list5.Clear();
            //			int ID;
            //			for (ID = num + k; ID <= num2; ID += 2)
            //			{
            //				if (list2.Exists((PipettingInfo x) => x.srcWellID == ID && x.srcLabware == curPlateName))
            //				{
            //					PipettingInfo item = list2.First((PipettingInfo x) => x.srcWellID == ID && x.srcLabware == curPlateName);
            //					list5.Add(item);
            //					list2 = list2.Except(new List<PipettingInfo>
            //					{
            //						item
            //					}).ToList<PipettingInfo>();
            //				}
            //			}
            //			list3.AddRange(this.FormatBatch(list5));
            //		}
            //	}
            //}
            //foreach (PipettingInfo current2 in list2)
            //{
            //	list3.AddRange(this.GenerateGWL(current2));
            //}
            //list3.Add("B;");
            //return list3;

            if (pipettingInfos.First().dstLabware != "Mix")
            {
                List<string> tmpCommands = new List<string>();
                foreach (var pipettingInfo in pipettingInfos)
                {
                    tmpCommands.AddRange(GenerateGWL(pipettingInfo));
                }
                tmpCommands.Add("B;");
                return tmpCommands;
            }

            List<PipettingInfo> tmpPipettingInfos = new List<PipettingInfo>(pipettingInfos);
            //List<PipettingInfo> allOptimizedPipettingInfos = new List<PipettingInfo>();
            List<string> commands = new List<string>();
            string firstPlateName = pipettingInfos.First().srcLabware;
            string secondPlateName = pipettingInfos.Last().srcLabware;
            List<PipettingInfo> thisBatchPipettingInfos = new List<PipettingInfo>();
            List<string> plateNames = new List<string>();
            plateNames.Add(firstPlateName);
            plateNames.Add(secondPlateName);
            for (int times = 0; times < 2; times++)
            {
                string curPlateName = plateNames[times];
                for(int i = 0; i< 12; i++) //12 batch, each batch 8 wells
                {
                    int startDstWellID = i * 8 + 1;
                    int endDstWellID = startDstWellID + 8 - 1;
                    thisBatchPipettingInfos.Clear();
                    thisBatchPipettingInfos = tmpPipettingInfos.Where(x => SameBatch(x.dstWellID, startDstWellID, endDstWellID)).ToList();
                    commands.AddRange(FormatBatch(thisBatchPipettingInfos));
                    tmpPipettingInfos = tmpPipettingInfos.Except(thisBatchPipettingInfos).ToList();
                }

                //for (int col = 0; col < Common.cols384; col++)
                //{
                //    int startID = col * Common.rows384 + 1;
                //    int endID = startID + Common.rows384 - 1;
                //    for (int oddEven = 0; oddEven < 2; oddEven++)
                //    // first we pipet 1,3,5,7,9,11,13,15
                //    //then we pipet  2,4,6,8,10,12,14,16
                //    {
                //        thisBatchPipettingInfos.Clear();
                //        for (int ID = startID + oddEven; ID <= endID; ID += 2)
                //        {
                //            if (!tmpPipettingInfos.Exists(x => x.srcWellID == ID && x.srcLabware == curPlateName))
                //                continue;
                //            var pipettingInfo = tmpPipettingInfos.First(x => x.srcWellID == ID && x.srcLabware == curPlateName);
                //            thisBatchPipettingInfos.Add(pipettingInfo);

                //            //allOptimizedPipettingInfos.Add(pipettingInfo);
                //            tmpPipettingInfos = tmpPipettingInfos.Except(new List<PipettingInfo>() { pipettingInfo }).ToList();
                //        }
                //        commands.AddRange(FormatBatch(thisBatchPipettingInfos));

                //    }

                //}
            }
            //foreach (var pipettingInfo in tmpPipettingInfos)
            //{
            //    commands.AddRange(GenerateGWL(pipettingInfo));
            //}
            commands.Add("B;");
            return commands;
        }

        private bool SameBatch(int dstWellID, int startDstWellID, int endDstWellID)
        {
            return dstWellID <= endDstWellID && dstWellID >= startDstWellID;
        }

        private List<string> FormatBatch(List<PipettingInfo> thisBatchPipettingInfos)
		{
			List<string> list = new List<string>();
			foreach (PipettingInfo current in thisBatchPipettingInfos)
			{
				list.AddRange(this.GenerateGWL(current));
			}
			list.Add("B;");
			return list;
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
                        List<PipettingInfo> expectedItems = sameSrcPlatePipettingInfo.Where(x => x.srcWellID == i + 1 && x.dstLabware == "Mix").ToList();
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

        public List<string> Format(List<PipettingInfo> pipettingInfos, bool bReadable = false)
		{
			List<string> list = new List<string>();
			foreach (PipettingInfo current in pipettingInfos)
			{
				list.Add(this.Format(current, bReadable));
			}
			return list;
		}

		private string Format(PipettingInfo pipettingInfo, bool bReadable)
		{
			string text = bReadable ? this.GetWellStr384(pipettingInfo.srcWellID) : pipettingInfo.srcWellID.ToString();
			string text2 = pipettingInfo.dstWellID.ToString();
			if (this.mix2plateKeywords.Contains(pipettingInfo.dstLabware))
			{
				text2 = Common.GetWellDesc(pipettingInfo.dstWellID);
			}
			else if (GlobalVars.LabwareWellCnt == 24)
			{
				int num = (pipettingInfo.dstWellID - 1) / 6;
				int num2 = pipettingInfo.dstWellID - num * 6 - 1;
				text2 = string.Format("{0}{1}", (char)(65 + num), num2 + 1);
			}
			if (bReadable)
			{
				return string.Format("{0},{1},{2},{3},{4},{5}", new object[]
				{
					pipettingInfo.sPrimerID,
					pipettingInfo.srcLabware,
					text,
					pipettingInfo.dstLabware,
					text2,
					pipettingInfo.vol
				});
			}
			return string.Format("{0},{1},{2},{3},{4},{5}", new object[]
			{
				pipettingInfo.srcLabware,
				Common.GetWellDesc384(pipettingInfo.srcWellID),
				pipettingInfo.dstLabware,
				text2,
				pipettingInfo.vol,
				pipettingInfo.sPrimerID
			});
		}

		private List<PipettingInfo> SplitPipettingInfos(List<PipettingInfo> pipettingInfos)
		{
			List<PipettingInfo> list = new List<PipettingInfo>();
			foreach (PipettingInfo current in pipettingInfos)
			{
				PipettingInfo pipettingInfo = new PipettingInfo(current);
				while (pipettingInfo.vol > (double)this.maxVol)
				{
					list.Add(new PipettingInfo(pipettingInfo)
					{
						vol = (double)this.maxVol
					});
					pipettingInfo.vol -= (double)this.maxVol;
				}
				if (pipettingInfo.vol != 0.0)
				{
					list.Add(pipettingInfo);
				}
			}
			return list;
		}

		private List<string> GenerateWorklist(List<PipettingInfo> pipettingInfos)
		{
			List<string> list = new List<string>();
			while (pipettingInfos.Count > 0)
			{
				PipettingInfo first = pipettingInfos.First<PipettingInfo>();
				List<PipettingInfo> list2 = (from x in pipettingInfos
				where x.srcWellID == first.srcWellID
				select x).ToList<PipettingInfo>();
				List<PipettingInfo> list3 = (from x in list2
				where x.vol == (double)this.maxVol
				select x).ToList<PipettingInfo>();
				foreach (PipettingInfo current in list3)
				{
					list.AddRange(this.GenerateWorklistSameBatch(new List<PipettingInfo>
					{
						current
					}));
				}
				pipettingInfos = pipettingInfos.Except(list2).ToList<PipettingInfo>();
				list2 = list2.Except(list3).ToList<PipettingInfo>();
				list.AddRange(this.GenerateWorklistSameBatch(list2));
			}
			return list;
		}

		private string GetWellStr(int wellID)
		{
			int i;
			for (i = wellID - 1; i >= 8; i -= 8)
			{
			}
			int num = (wellID - 1) / 8;
			char c = (char)(65 + i);
			return string.Format("{0}{1:D2}", c, num + 1);
		}

		private string GetWellStr384(int wellID)
		{
			int i;
			for (i = wellID - 1; i >= Common.rows384; i -= Common.rows384)
			{
			}
			int num = (wellID - 1) / Common.rows384;
			char c = (char)(65 + i);
			return string.Format("{0}{1:D2}", c, num + 1);
		}

		private List<string> GenerateWorklistSameBatch(List<PipettingInfo> batchPipettingInfos)
		{
			List<string> list = new List<string>();
			PipettingInfo pipettingInfo = batchPipettingInfos.First<PipettingInfo>();
			int i;
			for (i = pipettingInfo.srcWellID - 1; i >= Common.rows384; i -= Common.rows384)
			{
			}
			int num = (pipettingInfo.srcWellID - 1) / Common.rows384;
			char c = (char)(65 + i);
			string item = string.Format("C;{0}{1:D2}", c, num + 1);
			list.Add(item);
			this.processedSrcWells.Add(pipettingInfo.srcWellID);
			if (batchPipettingInfos.Count == 1)
			{
				string aspirate = this.GetAspirate(pipettingInfo.srcLabware, pipettingInfo.srcWellID, pipettingInfo.vol);
				string dispense = this.GetDispense(pipettingInfo.dstLabware, pipettingInfo.dstWellID, pipettingInfo.vol);
				list.Add(aspirate);
				list.Add(dispense);
			}
			else
			{
				double vol = batchPipettingInfos.Sum((PipettingInfo x) => x.vol);
				double arg_101_0 = (double)this.maxVol;
				string aspirate2 = this.GetAspirate(pipettingInfo.srcLabware, pipettingInfo.srcWellID, vol);
				list.Add(aspirate2);
				foreach (PipettingInfo current in batchPipettingInfos)
				{
					string dispense2 = this.GetDispense(current.dstLabware, current.dstWellID, current.vol);
					list.Add(dispense2);
				}
			}
			list.Add("W;");
			return list;
		}

		private List<PipettingInfo> GetPipettingInfos(List<ItemInfo> itemsInfo)
		{
			List<PipettingInfo> list = new List<PipettingInfo>();
			List<ItemInfo> list2 = new List<ItemInfo>(itemsInfo);
			this.FillVols(list2);
			while (list2.Count > 0)
			{
				ItemInfo first = list2.First<ItemInfo>();
				int subID = first.subID;
				List<ItemInfo> list3 = (from x in list2
				where x.mainID == first.mainID && x.sExtraDescription == first.sExtraDescription
				select x).ToList<ItemInfo>();
				list3 = this.CheckSequential(first, list3);
				list2 = list2.Except(list3).ToList<ItemInfo>();
				List<StartEnd> list4 = this.ParseRanges(first.sExtraDescription, subID);
				List<ItemInfo> list5 = new List<ItemInfo>();
				using (List<StartEnd>.Enumerator enumerator = list4.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						StartEnd range = enumerator.Current;
						List<ItemInfo> list6 = (from x in list3
						where this.InRange(x, range)
						select x).ToList<ItemInfo>();
						if (list6.Count > 0)
						{
							this.AddPipettingInfo4ThisRange(list, list6, range);
						}
						list5.AddRange(list6);
					}
				}
				list5 = list5.Distinct<ItemInfo>().ToList<ItemInfo>();
				list3 = list3.Except(list5).ToList<ItemInfo>();
				if (list3.Count > 0)
				{
					ItemInfo itemInfo = list3.First<ItemInfo>();
					throw new Exception(string.Format("there are samples not in any range! \r\nFirst sample plateName:{0}, id:{1} ", itemInfo.plateName, itemInfo.sID));
				}
			}
			return list;
		}

		private PipettingInfo GetStartEndPipetting(PipettingInfo item, string dstLabware, int dstWellID, int vol)
		{
			return new PipettingInfo(item.sPrimerID, item.dstLabware, item.dstWellID, dstLabware, dstWellID, (double)vol);
		}

		public List<PipettingInfo> AddStartEnd2EPTube(List<PipettingInfo> pipettingInfos)
		{
			List<PipettingInfo> list = new List<PipettingInfo>();
			List<PipettingInfo> list2 = (from x in pipettingInfos
			where x.dstLabware == "Start"
			orderby x.dstWellID
			select x).ToList<PipettingInfo>();
			List<PipettingInfo> list3 = (from x in pipettingInfos
			where x.dstLabware == "End"
			orderby x.dstWellID
			select x).ToList<PipettingInfo>();
			if (list2.Count != list3.Count)
			{
				throw new Exception(string.Format("Start Plate total count:{0} != End Plate total count:{1}", list2.Count, list3.Count));
			}
			int vol = int.Parse(ConfigurationManager.AppSettings["EPVolume"]);
			string dstLabware = "";
			int dstWellID = 0;
			for (int i = 0; i < list2.Count; i++)
			{
				this.CalculateEPPos(ref dstLabware, ref dstWellID);
				Worklist.usedEPTubes++;
				PipettingInfo item = list2[i];
				list.Add(this.GetStartEndPipetting(item, dstLabware, dstWellID, vol));
				this.CalculateEPPos(ref dstLabware, ref dstWellID);
				Worklist.usedEPTubes++;
				PipettingInfo item2 = list3[i];
				list.Add(this.GetStartEndPipetting(item2, dstLabware, dstWellID, vol));
			}
			pipettingInfos.AddRange(list);
			return list;
		}

		private void AddPipettingInfo4ThisRange(List<PipettingInfo> pipettingInfos, List<ItemInfo> sameRangeItems, StartEnd range)
		{
			if (this.IsOnBoundary(sameRangeItems.First<ItemInfo>(), range))
			{
				this.Add2EPTube(pipettingInfos, sameRangeItems.First<ItemInfo>());
				this.Add2PlateStart(pipettingInfos, sameRangeItems.First<ItemInfo>());
			}
			foreach (ItemInfo current in sameRangeItems)
			{
				this.Add2PlateMix(pipettingInfos, current);
			}
			if (sameRangeItems.Count > 1 && this.IsOnBoundary(sameRangeItems.Last<ItemInfo>(), range))
			{
				this.Add2EPTube(pipettingInfos, sameRangeItems.Last<ItemInfo>());
				this.Add2PlateEnd(pipettingInfos, sameRangeItems.Last<ItemInfo>());
			}
			Worklist.usedPlateWells++;
		}

		private void Add2PlateEnd(List<PipettingInfo> pipettingInfos, ItemInfo itemInfo)
		{
			this.Add2Plate(pipettingInfos, itemInfo, "End", true);
		}

		private void Add2PlateMix(List<PipettingInfo> pipettingInfos, ItemInfo itemInfo)
		{
			this.Add2Plate(pipettingInfos, itemInfo, "Mix", false);
		}

		private void Add2PlateStart(List<PipettingInfo> pipettingInfos, ItemInfo itemInfo)
		{
			this.Add2Plate(pipettingInfos, itemInfo, "Start", true);
		}

		private void Add2Plate(List<PipettingInfo> pipettingInfos, ItemInfo itemInfo, string plateName, bool bOnBoundary = true)
		{
			int num = itemInfo.vol;
			if (bOnBoundary)
			{
				num = (ConfigurationManager.AppSettings.AllKeys.Contains("BoundaryFixedVolume") ? int.Parse(ConfigurationManager.AppSettings["BoundaryFixedVolume"]) : (itemInfo.vol * 10));
				num += int.Parse(ConfigurationManager.AppSettings["EPVolume"]);
			}
			PipettingInfo item = new PipettingInfo(itemInfo.sID, itemInfo.plateName, itemInfo.srcWellID, plateName, Worklist.usedPlateWells + 1, (double)num);
			pipettingInfos.Add(item);
		}

		private void Add2EPTube(List<PipettingInfo> pipettingInfos, ItemInfo itemInfo)
		{
		}

		private void CalculateEPPos(ref string slabwareID, ref int wellID)
		{
			int num = Worklist.usedEPTubes + 1;
			int num2 = (num + 16 - 1) / 16;
			slabwareID = string.Format("dst{0}", num2);
			wellID = num - 16 * (num2 - 1);
		}

		public void AdjustLabwareLabels(List<PipettingInfo> pipettingInfos, List<string> batchPlateNames, bool adjustSrc)
		{
			new List<PipettingInfo>(pipettingInfos);
			List<string> list;
			if (adjustSrc)
			{
				list = batchPlateNames;
			}
			else
			{
				list = (from x in pipettingInfos
				group x by x.dstLabware into x
				select x.Key into x
				where !this.mix2plateKeywords.Contains(x)
				select x).ToList<string>();
			}
			int num = 1;
			foreach (string current in list)
			{
				string arg = adjustSrc ? "src" : "dst";
				string text = string.Format("{0}{1}", arg, num);
				num++;
				for (int i = 0; i < pipettingInfos.Count; i++)
				{
					if (adjustSrc)
					{
						if (pipettingInfos[i].srcLabware == current)
						{
							pipettingInfos[i].srcLabware = text;
						}
					}
					else if (pipettingInfos[i].dstLabware == current)
					{
						pipettingInfos[i].dstLabware = text;
					}
				}
			}
		}

		private List<ItemInfo> CheckSequential(ItemInfo first, List<ItemInfo> sameMainIDItems)
		{
			List<ItemInfo> list = new List<ItemInfo>();
			int expectedID = first.subID;
			bool flag = true;
			list.Add(first);
			while (flag)
			{
				expectedID++;
				flag = sameMainIDItems.Exists((ItemInfo x) => x.subID == expectedID);
				if (flag)
				{
					list.Add(sameMainIDItems.Find((ItemInfo x) => x.subID == expectedID));
				}
			}
			return list;
		}

		private bool IsFixedPosRange(string sExtraDescription)
		{
			if (sExtraDescription.Length > 3)
			{
				return false;
			}
			char c = sExtraDescription.First<char>();
			if (c < 'A' || c > 'H')
			{
				return false;
			}
			sExtraDescription = sExtraDescription.Substring(1);
			int num = 0;
			return int.TryParse(sExtraDescription, out num);
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
				ItemInfo itemInfo = itemsInfoCopy[i];
				itemInfo.vol = OdSheet.eachPlateID_Vols[itemInfo.plateName][itemInfo.srcWellID];
			}
		}

		private List<StartEnd> ParseRanges(string s, int firstSubID)
		{
			if (s == "" || s == OperationSheet.empty)
			{
				return new List<StartEnd>();
			}
			s = this.GetMeaningfulRange(s);
			return this.ParseMeaningfulRanges(s, firstSubID);
		}

		private string GetMeaningfulRange(string s)
		{
			int num = s.IndexOf("**");
			int num2 = s.LastIndexOf("**");
			if (num2 != s.Length - 2)
			{
				throw new Exception("Invalid remarks! Last two chars is NOT '**'");
			}
			if (num == -1)
			{
				throw new Exception("Invalid remarks! first two chars is NOT '**'");
			}
			if (num2 == num)
			{
				throw new Exception("Invalid remarks! Only one ** found!");
			}
			string text = s.Substring(num + 2);
			return text.Substring(0, text.Length - 2);
		}

		private string GetAspirate(string sLabware, int srcWellID, double vol)
		{
			return string.Format("A;{0};;;{1};;{2};;;", sLabware, srcWellID, vol);
		}

		private string GetDispense(string sLabware, int dstWellID, double vol)
		{
			return string.Format("D;{0};;;{1};;{2};;;", sLabware, dstWellID, vol);
		}

		private List<StartEnd> ParseMeaningfulRanges(string sExtraDesc, int firstSubID)
		{
			string[] array = sExtraDesc.Split(new char[]
			{
				'*'
			});
			List<StartEnd> list = new List<StartEnd>();
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string s = array2[i];
				list.Add(this.ParseRange(s, firstSubID));
			}
			return list;
		}

		private StartEnd ParseRange(string s, int firstSubID)
		{
			string[] array = s.Split(new char[]
			{
				'-'
			});
			int num = int.Parse(array[0]);
			int num2 = int.Parse(array[1]);
			StartEnd result = new StartEnd(num + firstSubID - 1, num2 + firstSubID - 1);
			return result;
		}

		internal List<string> GetDestLabwares(List<PipettingInfo> allPipettingInfos)
		{
			HashSet<string> itemNames = new HashSet<string>();
			allPipettingInfos.ForEach(delegate(PipettingInfo x)
			{
				itemNames.Add(x.dstLabware);
			});
			return new List<string>(itemNames);
		}
	}
}
