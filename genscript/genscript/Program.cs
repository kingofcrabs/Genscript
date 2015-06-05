using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using Microsoft.Office.Interop.Excel;
using System.Reflection;

namespace genscript
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
            List<string> optFiles = files.Where(x => x.Contains("_192")).ToList();
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
                bool mix2EPTubes = !Common.Mix2Plate;

                List<PipettingInfo> allPipettingInfos = new List<PipettingInfo>();
                int filesPerBatch = mix2EPTubes ?  2 : Common.PlateCnt;
                int batchCnt = (optCSVFiles.Count + filesPerBatch - 1) / filesPerBatch;
                File.WriteAllText(outputFolder + "fileCnt.txt", batchCnt.ToString());
                List<ItemInfo> itemsInfo = new List<ItemInfo>();
                List<OperationSheetQueueInfo> queueInfos = new List<OperationSheetQueueInfo>();
                if (mix2EPTubes) //write dummy files
                {
                    for (int i = 0; i < 4; i++)
                    {
                        string sOutputFile = outputFolder + string.Format("{0}.csv", i + 1);
                        File.WriteAllLines(sOutputFile, new List<string>());
                    }
               
                }
                else //pass all itemsInfo to get whole pipettingInfo
                {
                    //sort with first dest mix well & subID
                    for (int i = 0; i < optCSVFiles.Count; i++)
                    {
                        OperationSheet optSheet = new OperationSheet(optCSVFiles[i]);
                        OperationSheetQueueInfo queueInfo = new OperationSheetQueueInfo(optSheet, optCSVFiles[i]);
                        queueInfos.Add(queueInfo);
                        OdSheet odSheet = new OdSheet(odCSVFiles[i], i);
                        itemsInfo.AddRange(optSheet.Items);
                    }
                    var tmpStrs = worklist.GenerateWorklist(itemsInfo, readablecsvFormatStrs, ref allPipettingInfos,
                              ref optGwlFormatStrs);
                    queueInfos = queueInfos.OrderBy(x => x.startDstMixWell + x.startSubID.ToString()).ToList();
                }
                for (int batchIndex = 0; batchIndex < batchCnt; batchIndex++)
                {
                    int startFileIndex = batchIndex * filesPerBatch;
                    string sOutputFile = outputFolder + string.Format("{0}.csv", batchIndex + 1);
                    string sBatchSrcPlatesFile = outputFolder + string.Format("src_{0}.txt", batchIndex + 1);
                    string sBatchSrcPlatesCntFile = outputFolder + string.Format("src_{0}Cnt.txt", batchIndex + 1);
                    string sDstLabwaresFile = outputFolder + string.Format("dst_{0}.txt", batchIndex + 1);
                    string sOutputGwlFile = outputFolder + string.Format("{0}.gwl", batchIndex + 1);
                    List<string> batchPlateNames = new List<string>();
                    List<OperationSheetQueueInfo> batchPlateInfos = new List<OperationSheetQueueInfo>();
                    for (int i = 0; i < filesPerBatch; i++)
                    {
                        int curFileIndex = startFileIndex + i;
                        if (curFileIndex >= optCSVFiles.Count)
                            break;
                        var filePath = mix2EPTubes ? optCSVFiles[curFileIndex] : queueInfos[curFileIndex].filePath;
                        batchPlateNames.Add(GetSrcPlateName(filePath));
                        if (mix2EPTubes) //prepare itemInfos
                        {
                            OperationSheet optSheet = new OperationSheet(optCSVFiles[curFileIndex]);
                            OdSheet odSheet = new OdSheet(odCSVFiles[curFileIndex], curFileIndex);
                            itemsInfo.AddRange(optSheet.Items);
                        }
                        else //prepare the plates
                        {
                            batchPlateInfos.Add(queueInfos[curFileIndex]);
                        }
                    }

                    if (mix2EPTubes)
                    {
                        var pipettingStrs = worklist.GenerateWorklist(itemsInfo, readablecsvFormatStrs, ref allPipettingInfos,
                                    ref optGwlFormatStrs);
                        File.WriteAllLines(sOutputFile, pipettingStrs);
                        File.WriteAllLines(sOutputGwlFile, optGwlFormatStrs);
                        itemsInfo.Clear();
                    }
                    else
                    {
                        var thisBatchPipettingInfos = GetPipettingInfosThisBatch(allPipettingInfos, batchPlateInfos);
                        var pipettingStrs = worklist.OptimizeThenFormat(thisBatchPipettingInfos);
                        var destLabwares = worklist.GetDestLabwares(allPipettingInfos);
                        File.WriteAllLines(sDstLabwaresFile, destLabwares);
                        File.WriteAllLines(sBatchSrcPlatesFile, batchPlateNames);
                        File.WriteAllLines(sOutputFile, pipettingStrs);
                    }
                }
                
                List<List<string>> primerIDsOfLabwareList = new List<List<string>>();
                primerIDsOfLabwareList = worklist.GetWellPrimerID(allPipettingInfos);
                MergeReadable(readablecsvFormatStrs, primerIDsOfLabwareList);
                if (Common.Mix2Plate)
                {
                    primerIDsOfLabwareList = worklist.GetWellPrimerID(allPipettingInfos, true);
                    MergeReadable(readablecsvFormatStrs, primerIDsOfLabwareList,true);
                }
                
                File.WriteAllLines(sReadableOutputFile, readablecsvFormatStrs);
                //File.WriteAllLines(s24WellPlatePrimerIDsFile, primerIDsOf24WellPlate);
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
            File.WriteAllText(sResultFile, "true");
            Console.WriteLine(string.Format("Out put file has been written to folder : {0}", outputFolder));
            Console.WriteLine("version: " + strings.version);
            Console.WriteLine("Press any key to exit!");
        }

        private static List<PipettingInfo> GetPipettingInfosThisBatch(List<PipettingInfo> allPipettingInfos, List<OperationSheetQueueInfo> batchPlateInfos)
        {
            throw new NotImplementedException();
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

        static private void MergeReadable(List<string> readableOutput, List<List<string>> well_PrimerIDsList, bool is96Plate = false)
        {
            int startLine = 0;
            if (is96Plate)
            {
                startLine = 18;
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

            if (GlobalVars.LabwareWellCnt == 16)
            {
                foreach (List<string> strs in well_PrimerIDsList)
                {
                    for (int i = 0; i < strs.Count; i++)
                    {
                        readableOutput[i] += ",," + strs[i];
                    }
                }
                return;
            }

            foreach (List<string> well_PrimerIDs in well_PrimerIDsList)
            {
                for (int i = 0; i < 6; i++)
                {
                    readableOutput[i + startLine] += ",," + well_PrimerIDs[i];
                }
                startLine += 9;
            }
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
