using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace One2X
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

        private static void DoJob()
        {
            List<string> files = Directory.EnumerateFiles(GlobalVars.WorkingFolder, "*csv").ToList();
            List<string> optFiles = files.Where(x => x.Contains("_192") ).ToList();
            List<string> odFiles = files.Where(x => x.Contains("_OD")).ToList();
            optFiles = optFiles.OrderBy(x => GetSubString(x)).ToList();
            odFiles = odFiles.OrderBy(x => GetSubString(x)).ToList();
            string outputFolder = GlobalVars.WorkingFolder + "Outputs\\";
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string sResultFile = outputFolder + "result.txt";
            File.WriteAllText(sResultFile, "False");

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

            for (int i = 0; i < optFiles.Count; i++)
            {
                string operationSheetPath = optFiles[i];
                string odSheetPath = odFiles[i];
                optCSVFiles.Add(operationSheetPath);
                odCSVFiles.Add(odSheetPath);
            }
            optCSVFiles.Sort();
            odCSVFiles.Sort();

            List<PipettingInfo> allPipettingInfos = new List<PipettingInfo>();
            List<ItemInfo> itemsInfo = new List<ItemInfo>();

            if (optCSVFiles.Count != 4)
                throw new Exception("csv files count must be 4!");

            for (int i = 0; i < optCSVFiles.Count; i++)
            {
                OperationSheet optSheet = new OperationSheet(optCSVFiles[i]);
                OdSheet odSheet = new OdSheet(odCSVFiles[i], i);
                itemsInfo.AddRange(optSheet.Items);
            }

            Worklist wklist = new Worklist();
            string sOutputGwlFile = outputFolder + "allInOne.gwl";
            var pipettingInfos = wklist.Generate(itemsInfo, sOutputGwlFile);
            allPipettingInfos.AddRange(pipettingInfos);
            itemsInfo.Clear();
            WriteReadable(wklist,allPipettingInfos,outputFolder);

            Console.WriteLine(string.Format("Version is: {0}", strings.version));
            File.WriteAllText(sResultFile, "True");
           
        }

        private static void WriteReadable(Worklist wklist, List<PipettingInfo> allPipettingInfos,string outputFolder)
        {
            List<List<string>> primerIDsOfLabwareList = new List<List<string>>();
            primerIDsOfLabwareList = wklist.GetWellPrimerID(allPipettingInfos);
            List<string> readablecsvFormatStrs = new List<string>();
            string sReadableHeader = "primerLabel,srcLabel,srcWell,dstLabel,dstWell,volume";
            readablecsvFormatStrs.Add(sReadableHeader);
            MergeReadable(readablecsvFormatStrs, primerIDsOfLabwareList);
            string sReadableOutputFile = outputFolder + "readableOutput.csv";
            File.WriteAllLines(sReadableOutputFile, readablecsvFormatStrs);
        }


        //private static List<PipettingInfo> GetPipettingInfosThisBatch(List<PipettingInfo> allPipettingInfos, List<OperationSheetQueueInfo> batchPlateInfos)
        //{
        //    List<PipettingInfo> batchPipettigInfos = allPipettingInfos.Where(x => InOneOfTheRanges(x.sPrimerID, batchPlateInfos)).ToList();
        //    List<PipettingInfo> tmpPipettingInfos = new List<PipettingInfo>();
        //    foreach (var pipettingInfo in batchPipettigInfos)
        //    {
        //        tmpPipettingInfos.Add(new PipettingInfo(pipettingInfo));
        //    }
        //    return tmpPipettingInfos;
        //}
        private static string GetSrcPlateName(string sFilePath)
        {
            FileInfo fileInfo = new FileInfo(sFilePath);
            string name = fileInfo.Name;
            return name.Substring(0, name.Length - 8);
        }

        static private void MergeReadable(List<string> readableOutput, List<List<string>> well_PrimerIDsList)
        {
            int startLine = 0;
            bool is96Plate = GlobalVars.LabwareWellCnt != 16;
            if (is96Plate)
            {
                startLine = 18;
                foreach (List<string> well_PrimerIDs in well_PrimerIDsList)
                {
                    for (int i = 0; i < well_PrimerIDs.Count; i++)
                    {
                        readableOutput[i + startLine] += ",," + well_PrimerIDs[i];
                    }
                    startLine += (Common.rowCnt + 3);
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

          
        }
       
        private static string GetSubString(string x)
        {
            int pos = 0;
            x = x.ToLower();
            pos = x.LastIndexOf("\\");
            x = x.Substring(pos + 1);
            pos = x.IndexOf(".csv");
            x = x.Substring(0, pos);
            for (int i = 0; i < x.Length; i++)
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


        internal static void Convert2CSV()
        {
            Console.WriteLine("try to convert the excel to csv format.");
            List<string> files = Directory.EnumerateFiles(GlobalVars.WorkingFolder, "*.xls").ToList();
            SaveAsCSV(files);
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
