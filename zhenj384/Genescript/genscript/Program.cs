using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace genscript
{
	internal class Program
	{
		private static void Main(string[] args)
		{
            var nameValueCollection = ConfigurationManager.AppSettings;
            var labwareCnt = ConfigurationManager.AppSettings["labwareWellCnt"];

            GlobalVars.LabwareWellCnt = int.Parse(labwareCnt);
			GlobalVars.WorkingFolder = ConfigurationManager.AppSettings["workingFolder"] + "\\";
			Program.Convert2CSV();
			try
			{
				Program.DoJob();
			}
			catch (Exception ex)
			{
				Console.Write(ex.Message + ex.StackTrace);
			}
			Console.ReadKey();
		}

		public static void DoJob()
		{
			string item = "srcLabel,srcWell,dstLabel,dstWell,volume";
			string item2 = "primerLabel,srcLabel,srcWell,dstLabel,dstWell,volume";
			List<string> source = Directory.EnumerateFiles(GlobalVars.WorkingFolder, "*csv").ToList<string>();
			List<string> list = (from x in source
			where x.Contains("_768")
			select x).ToList<string>();
			List<string> list2 = (from x in source
			where x.Contains("_OD")
			select x).ToList<string>();
			list = (from x in list
			orderby Program.GetSubString(x)
			select x).ToList<string>();
			list2 = (from x in list2
			orderby Program.GetSubString(x)
			select x).ToList<string>();
			string outputFolder = GlobalVars.WorkingFolder + "Outputs\\";
			if (!Directory.Exists(outputFolder))
			{
				Directory.CreateDirectory(outputFolder);
			}
			string path = outputFolder + "result.txt";
			File.WriteAllText(path, "false");
			if (list.Count != list2.Count)
			{
				Console.WriteLine("operation sheets' count does not equal to OD sheets' count.");
				Console.WriteLine("Press any key to exit!");
				Console.ReadKey();
				return;
			}
			if (list.Count == 0)
			{
				Console.WriteLine("No valid file found in the directory.");
				Console.WriteLine("Press any key to exit!");
				Console.ReadKey();
				return;
			}
			List<string> optCSVFiles = new List<string>();
			List<string> odCSVFiles = new List<string>();
			//text + "output.gwl";
			string text2 = outputFolder + "readableOutput.csv";
			//text + "readableOutput24WellPrimerIDs.csv";
			Worklist worklist = new Worklist();
			try
			{
				for (int i = 0; i < list.Count; i++)
				{
					string item3 = list[i];
					string item4 = list2[i];
					optCSVFiles.Add(item3);
					odCSVFiles.Add(item4);
				}
				optCSVFiles.Sort();
				odCSVFiles.Sort();
				List<string> csvFormatstrs = new List<string>();
				List<string> readablecsvFormatStrs = new List<string>();
				List<string> optGwlFormatStrs = new List<string>();
				csvFormatstrs.Add(item);
				readablecsvFormatStrs.Add(item2);
				List<PipettingInfo> allPipettingInfos = new List<PipettingInfo>();
				List<ItemInfo> itemsInfo = new List<ItemInfo>();
				for (int j = 0; j < optCSVFiles.Count; j++)
				{
					OperationSheet operationSheet = new OperationSheet(optCSVFiles[j]);
					itemsInfo.AddRange(operationSheet.Items);
					OdSheet odSheet = new OdSheet();
					odSheet.ReadInfo(odCSVFiles[j], j);
				}
				worklist.GenerateWorklist(itemsInfo, readablecsvFormatStrs, ref allPipettingInfos, ref optGwlFormatStrs);
				List<string> batchPlateNames = new List<string>();
				worklist.AdjustLabwareLabels(allPipettingInfos, batchPlateNames, true);
				Dictionary<string, List<PipettingInfo>> dictionary = new Dictionary<string, List<PipettingInfo>>();
				if (GlobalVars.pipettingMixFirst)
				{
					List<PipettingInfo> list10 = (from x in allPipettingInfos
					where x.dstLabware == "Mix"
					select x).ToList<PipettingInfo>();
					List<PipettingInfo> value = allPipettingInfos.Except(list10).ToList<PipettingInfo>();
					dictionary.Add("Mix", list10);
					dictionary.Add("StartEnd", value);
				}
				else
				{
					dictionary.Add("All", allPipettingInfos);
				}
				foreach (KeyValuePair<string, List<PipettingInfo>> current in dictionary)
				{
					List<PipettingInfo> value2 = current.Value;
					string key = current.Key;
					string subFolder = outputFolder + string.Format("{0}\\", key);
					if (!Directory.Exists(subFolder))
					{
						Directory.CreateDirectory(subFolder);
					}
					File.WriteAllText(subFolder + "count.txt", "1");
					string gwlFile = subFolder + "1.gwl";
					List<string> contents = worklist.OptimizeCommandsSinglePlate(value2);
					File.WriteAllLines(gwlFile, contents);
				}
				List<PipettingInfo> source2 = worklist.AddStartEnd2EPTube(allPipettingInfos);
				List<PipettingInfo> pipettingInfos = (from x in source2
				where x.srcLabware == "Start"
				select x).ToList<PipettingInfo>();
				List<string> contents2 = worklist.GenerateGWL(pipettingInfos);
				List<PipettingInfo> pipettingInfos2 = (from x in source2
				where x.srcLabware == "End"
				select x).ToList<PipettingInfo>();
				List<string> contents3 = worklist.GenerateGWL(pipettingInfos2);
				File.WriteAllLines(outputFolder + "start.gwl", contents2);
				File.WriteAllLines(outputFolder + "end.gwl", contents3);
				List<List<string>> well_PrimerIDsList = new List<List<string>>();
				well_PrimerIDsList = worklist.GetWellPrimerID(allPipettingInfos, Common.Mix2Plate);
				Program.MergeReadable(readablecsvFormatStrs, well_PrimerIDsList);
				File.WriteAllLines(text2, readablecsvFormatStrs);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + ex.StackTrace);
				Console.WriteLine("Press any key to exit!");
				throw ex;
			}
			string text4 = outputFolder + "backup\\";
			if (!Directory.Exists(text4))
			{
				Directory.CreateDirectory(text4);
			}
			//text4 + DateTime.Now.ToString("yyMMdd_hhmmss") + "_output.csv";
			string destFileName = text4 + DateTime.Now.ToString("yyMMdd_hhmmss") + "_readableoutput.csv";
			File.Copy(text2, destFileName);
			string version = strings.version;
			File.WriteAllText(path, "true");
			Console.WriteLine(string.Format("Out put file has been written to folder : {0}", outputFolder));
			Console.WriteLine("version: " + version);
			Console.WriteLine("Press any key to exit!");
		}

		private static List<PipettingInfo> GetPipettingInfosThisBatch(List<PipettingInfo> allPipettingInfos, List<OperationSheetQueueInfo> batchPlateInfos)
		{
			List<PipettingInfo> list = (from x in allPipettingInfos
			where Program.PlateInBatch(x.srcLabware, batchPlateInfos)
			select x).ToList<PipettingInfo>();
			List<PipettingInfo> list2 = new List<PipettingInfo>();
			foreach (PipettingInfo current in list)
			{
				list2.Add(new PipettingInfo(current));
			}
			return list2;
		}

		private static bool PlateInBatch(string plateName, List<OperationSheetQueueInfo> batchPlateInfos)
		{
			return batchPlateInfos.Exists((OperationSheetQueueInfo x) => Common.GetPlateName(x.filePath) == plateName);
		}

		private static bool InOneOfTheRanges(string sPrimerID, List<OperationSheetQueueInfo> batchPlateInfos)
		{
			List<string> list = sPrimerID.Split(new char[]
			{
				'_'
			}).ToList<string>();
			int subID = int.Parse(list[1]);
			foreach (OperationSheetQueueInfo current in batchPlateInfos)
			{
				if (Program.InRange(subID, current))
				{
					return true;
				}
			}
			return false;
		}

		private static bool InRange(int subID, OperationSheetQueueInfo queueInfo)
		{
			return subID >= queueInfo.startSubID && subID <= queueInfo.endSubID;
		}

		private static string GetSrcPlateName(string sFilePath)
		{
			FileInfo fileInfo = new FileInfo(sFilePath);
			string name = fileInfo.Name;
			return name.Substring(0, name.Length - 8);
		}

		internal static void Convert2CSV()
		{
			Console.WriteLine("try to convert the excel to csv format.");
			List<string> sheetPaths = Directory.EnumerateFiles(GlobalVars.WorkingFolder, "*.xls").ToList<string>();
			Program.SaveAsCSV(sheetPaths);
		}

		public static string GetExeFolder()
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			return directoryName + "\\";
		}

		public static string GetExeParentFolder()
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			int length = directoryName.LastIndexOf("\\");
			return directoryName.Substring(0, length) + "\\";
		}

		private static void MergeReadable(List<string> readableOutput, List<List<string>> well_PrimerIDsList)
		{
			int num = 0;
			foreach (List<string> current in well_PrimerIDsList)
			{
				for (int i = 0; i < current.Count; i++)
				{
					int index;
					readableOutput[index = i + num] = readableOutput[index] + ",," + current[i];
				}
				num += 11;
			}
		}

		private static string GetSubString(string x)
		{
			x = x.ToLower();
			int num = x.LastIndexOf("\\");
			x = x.Substring(num + 1);
			num = x.IndexOf(".csv");
			x = x.Substring(0, num);
			for (int i = 0; i < x.Length; i++)
			{
				char c = x[i];
				if (char.IsLetter(c))
				{
					num = i;
					break;
				}
			}
			int length = x.IndexOf('_');
			string text = x.Substring(0, length);
			return text.Substring(num);
		}

		private static void SaveAsCSV(List<string> sheetPaths)
		{
			Application application = (Application)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00024500-0000-0000-C000-000000000046")));
			application.Visible = false;
			application.DisplayAlerts = false;
			foreach (string current in sheetPaths)
			{
				int num = current.IndexOf(".xls");
				if (num == -1)
				{
					throw new Exception("Cannot find xls in file name!");
				}
				string str = current.Substring(0, num);
				string text = str + ".csv";
				if (!File.Exists(text))
				{
					text = text.Replace("\\\\", "\\");
					Workbook workbook = application.Workbooks.Open(current, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
					workbook.SaveAs(text, XlFileFormat.xlCSV, Missing.Value, Missing.Value, Missing.Value, Missing.Value, XlSaveAsAccessMode.xlNoChange, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
					workbook.Close(Missing.Value, Missing.Value, Missing.Value);
					Console.WriteLine(text);
				}
			}
			application.Quit();
		}
	}
}
