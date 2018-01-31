using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using Microsoft.Office.Interop.Excel;
using System.Reflection;

namespace genscript384
{
    class Program
    {
        
        static void Main(string[] args)
        {
            GlobalVars.LabwareWellCnt = int.Parse(ConfigurationManager.AppSettings["labwareWellCnt"]);
            GlobalVars.WorkingFolder = ConfigurationManager.AppSettings["workingFolder"] + "\\";
            Convert2CSV();
#if DEBUG
            DoJob();
#else
            try
            {
                DoJob();
            }
            catch (System.Exception ex)
            {
                Console.Write(ex.Message + ex.StackTrace);
            }
#endif       
            Console.ReadKey();
        }

        public static void DoJob()
        {
            string sHeader = "srcLabel,srcWell,dstLabel,dstWell,volume";
            string sReadableHeader = "primerLabel,srcLabel,srcWell,dstLabel,dstWell,volume";
            List<string> files = Directory.EnumerateFiles(GlobalVars.WorkingFolder, "*csv").ToList();
            List<string> optFiles = files.Where(x => x.Contains("_384")).ToList();
            List<string> odFiles = files.Where(x => x.Contains("_OD")).ToList();
            optFiles = optFiles.OrderBy(x => GetSubString(x)).ToList();
            odFiles = odFiles.OrderBy(x => GetSubString(x)).ToList();
            string outputFolder = GlobalVars.WorkingFolder + "Outputs\\";
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string sResultFile = outputFolder + "result.txt";
            File.WriteAllText(sResultFile, "false");

            if (optFiles.Count != odFiles.Count)
            {
                Console.WriteLine("operation sheets' count does not equal to OD sheets' count.");
                Console.WriteLine("Press any key to exit!");
                Console.ReadKey();
                return;
            }
            if (optFiles.Count == 0)
            {
                Console.WriteLine("No valid file found in the directory.");
                Console.WriteLine("Press any key to exit!");
                Console.ReadKey();
                return;
            }
            List<string> optCSVFiles = new List<string>();
            List<string> odCSVFiles = new List<string>();

            string sOutputGWLFile = outputFolder + "output.gwl";
            string sReadableOutputFile = outputFolder + "readableOutput.csv";
            string s24WellPlatePrimerIDsFile = outputFolder + "readableOutput24WellPrimerIDs.csv";
            Worklist worklist = new Worklist();
#if DEBUG

#else
            try
#endif
            {
                // first, save all the excel files as csv files.
                for (int i = 0; i < optFiles.Count; i++)
                {
                    string operationSheetPath = optFiles[i];
                    string odSheetPath = odFiles[i];
                    optCSVFiles.Add(operationSheetPath);
                    odCSVFiles.Add(odSheetPath);
                }
                optCSVFiles.Sort();
                odCSVFiles.Sort();
                List<string> csvFormatstrs = new List<string>();
                List<string> readablecsvFormatStrs = new List<string>();
                List<string> optGwlFormatStrs = new List<string>();
                
                csvFormatstrs.Add(sHeader);
                readablecsvFormatStrs.Add(sReadableHeader);
             
                List<PipettingInfo> allPipettingInfos = new List<PipettingInfo>();
                List<PipettingInfo> copyPipettingInfos = new List<PipettingInfo>();
                List<ItemInfo> itemsInfo = new List<ItemInfo>();
                for (int i = 0; i < optCSVFiles.Count; i++)
                {
                    OperationSheet optSheet = new OperationSheet(optCSVFiles[i]);
                    itemsInfo.AddRange(optSheet.Items);

                    OdSheet odSheet = new OdSheet();
                    odSheet.ReadInfo(odCSVFiles[i], i);
                }


                var tmpStrs = worklist.GenerateWorklist(itemsInfo, readablecsvFormatStrs, ref allPipettingInfos, ref copyPipettingInfos,
                          ref optGwlFormatStrs);
                

                List<string> batchPlateNames = new List<string>();
                worklist.AdjustLabwareLabels(allPipettingInfos, batchPlateNames, true);
                
                Dictionary<string, List<PipettingInfo>> pipettingInfoList = new Dictionary<string, List<PipettingInfo>>();
                if (GlobalVars.pipettingMixFirst)
                {
                    var mixPipettingInfos = allPipettingInfos.Where(x => x.dstLabware == "Mix").ToList();
                    var otherPipettingInfos = allPipettingInfos.Except(mixPipettingInfos).ToList();
                    pipettingInfoList.Add("Mix", mixPipettingInfos);
                    pipettingInfoList.Add("StartEnd", otherPipettingInfos);
                }
                else
                {
                    pipettingInfoList.Add("All", allPipettingInfos);
                }

                foreach (var pair in pipettingInfoList)
                {
                    var tmpPipettingInfo = pair.Value;
                    var prefix = pair.Key;
                    string copyFileName = outputFolder + "copy.gwl";
                    string sOutputFolder = outputFolder + string.Format("{0}\\", prefix);
                    if (!Directory.Exists(sOutputFolder))
                    {
                        Directory.CreateDirectory(sOutputFolder);
                    }
                    File.WriteAllText(sOutputFolder + "count.txt", "1");
                    string fileName = sOutputFolder + "1.gwl";
                    var strs = worklist.OptimizeCommandsSinglePlate(tmpPipettingInfo);
                    File.WriteAllLines(fileName, strs);
                    var copyStrs = worklist.Format(copyPipettingInfos); 
                    File.WriteAllLines(copyFileName, strs);
                }
                
                //add start end to tubes
                var startEndPipettings = worklist.AddStartEnd2EPTube(allPipettingInfos);
                var startPipettings = startEndPipettings.Where(x=>x.srcLabware == "Start").ToList();
                var startPipettingStrs = worklist.GenerateGWL(startPipettings);
                var endPipettings = startEndPipettings.Where(x => x.srcLabware == "End").ToList();
                var endPipettingStrs = worklist.GenerateGWL(endPipettings);
                File.WriteAllLines(outputFolder + "start.gwl", startPipettingStrs);
                File.WriteAllLines(outputFolder + "end.gwl", endPipettingStrs);

                List<List<string>> primerIDsOfLabwareList = new List<List<string>>();
                primerIDsOfLabwareList = worklist.GetWellPrimerID(allPipettingInfos, Common.Mix2Plate);
                MergeReadable(readablecsvFormatStrs, primerIDsOfLabwareList);
                File.WriteAllLines(sReadableOutputFile, readablecsvFormatStrs);
            }
#if DEBUG

#else
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                Console.WriteLine("Press any key to exit!");
                throw ex;
            }
#endif
            string sBackupFolder = outputFolder + "backup\\";
            if (!Directory.Exists(sBackupFolder))
                Directory.CreateDirectory(sBackupFolder);
            string sBackupFile = sBackupFolder + DateTime.Now.ToString("yyMMdd_hhmmss") + "_output.csv";
            string sBackupReadableFile = sBackupFolder + DateTime.Now.ToString("yyMMdd_hhmmss") + "_readableoutput.csv";
            File.Copy(sReadableOutputFile, sBackupReadableFile);
            string sVersion = strings.version;
            File.WriteAllText(sResultFile, "true");
            Console.WriteLine(string.Format("Out put file has been written to folder : {0}", outputFolder));
            Console.WriteLine("version: " + sVersion);
            Console.WriteLine("Press any key to exit!");
        }

        private static List<PipettingInfo> GetPipettingInfosThisBatch(List<PipettingInfo> allPipettingInfos, List<OperationSheetQueueInfo> batchPlateInfos)
        {
            List<PipettingInfo>  batchPipettigInfos = allPipettingInfos.Where(x => PlateInBatch(x.srcLabware, batchPlateInfos)).ToList();
            List<PipettingInfo> tmpPipettingInfos = new List<PipettingInfo>();
            foreach (var pipettingInfo in batchPipettigInfos)
            {
                tmpPipettingInfos.Add(new PipettingInfo(pipettingInfo));
            }
            return tmpPipettingInfos;
        }

        private static bool PlateInBatch(string plateName, List<OperationSheetQueueInfo> batchPlateInfos)
        {

            bool contains = batchPlateInfos.Exists(x => Common.GetPlateName(x.filePath) == plateName);
            return contains;
        }

        private static bool InOneOfTheRanges(string sPrimerID, List<OperationSheetQueueInfo> batchPlateInfos)
        {
            List<string> strs = sPrimerID.Split('_').ToList();
            int subID = int.Parse(strs[1]);
            foreach(var queueInfo in batchPlateInfos)
            {
                if(InRange(subID,queueInfo))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool InRange(int subID, OperationSheetQueueInfo queueInfo)
        {
            return  subID >= queueInfo.startSubID && subID <= queueInfo.endSubID;
        }

     

        private static string GetSrcPlateName(string sFilePath)
        {
            FileInfo fileInfo = new FileInfo(sFilePath);
            string name = fileInfo.Name;
            return name.Substring(0, name.Length-8);
        }

        internal static void Convert2CSV()
        {
            Console.WriteLine("try to convert the excel to csv format.");
            List<string> files = Directory.EnumerateFiles(GlobalVars.WorkingFolder, "*.xls").ToList();
            SaveAsCSV(files);
        }
        static public string GetExeFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s + "\\";
        }

        static public string GetExeParentFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int index = s.LastIndexOf("\\");
            return s.Substring(0, index) + "\\";
        }

        static private void MergeReadable(List<string> readableOutput, List<List<string>> well_PrimerIDsList)
        {
            int startLine = 0;
            foreach (List<string> well_PrimerIDs in well_PrimerIDsList)
            {
                for (int i = 0; i < well_PrimerIDs.Count; i++)
                {
                    readableOutput[i + startLine] += ",," + well_PrimerIDs[i];
                }
                startLine += 11;
            }
            return;
       
        }

        private static string GetSubString(string x)
        {
            int pos = 0;
            x = x.ToLower();
            pos = x.LastIndexOf("\\");
            x = x.Substring(pos+1);
            pos = x.IndexOf(".csv");
            x = x.Substring(0, pos);
            for( int i =0; i< x.Length; i++)
            {
                char ch = x[i];
                if (Char.IsLetter(ch))
                {
                    pos = i;
                    break;
                }

            }
            int endPos = x.IndexOf('_');
            string sub = x.Substring(0, endPos);
            sub = sub.Substring(pos);
            return sub;
        }

        private static void SaveAsCSV(List<string> sheetPaths)
        {
            Application app = new Application();
            app.Visible = false;
            app.DisplayAlerts = false;
            foreach (string sheetPath in sheetPaths)
            {
                
                string sWithoutSuffix = "";
                int pos = sheetPath.IndexOf(".xls");
                if (pos == -1)
                    throw new Exception("Cannot find xls in file name!");
                sWithoutSuffix = sheetPath.Substring(0, pos);
                string sCSVFile = sWithoutSuffix + ".csv";
                if (File.Exists(sCSVFile))
                    continue;
                sCSVFile = sCSVFile.Replace("\\\\", "\\");
                Workbook wbWorkbook = app.Workbooks.Open(sheetPath);
                wbWorkbook.SaveAs(sCSVFile, XlFileFormat.xlCSV);
                wbWorkbook.Close();
                Console.WriteLine(sCSVFile);
            }
            app.Quit();
        }
    }
}
