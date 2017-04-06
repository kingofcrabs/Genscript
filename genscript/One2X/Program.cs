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

            for (int i = 0; i < optFiles.Count; i++)
            {
                string operationSheetPath = optFiles[i];
                string odSheetPath = odFiles[i];
                optCSVFiles.Add(operationSheetPath);
                odCSVFiles.Add(odSheetPath);
            }
            optCSVFiles.Sort();
            odCSVFiles.Sort();
            bool mix2EPTubes = !Common.Mix2Plate;
            List<PipettingInfo> allPipettingInfos = new List<PipettingInfo>();
            int filesPerBatch = mix2EPTubes ? 2 : Common.PlateCnt;
            int batchCnt = (optCSVFiles.Count + filesPerBatch - 1) / filesPerBatch;
            File.WriteAllText(outputFolder + "fileCnt.txt", batchCnt.ToString());

            List<ItemInfo> itemsInfo = new List<ItemInfo>();
            List<OperationSheetQueueInfo> queueInfos = new List<OperationSheetQueueInfo>();
            if (mix2EPTubes) //write dummy files
            {
                //for (int i = 0; i < 4; i++)
                //{
                //    string sOutputFile = outputFolder + string.Format("{0}.csv", i + 1);
                //    File.WriteAllLines(sOutputFile, new List<string>());
                //}
            }
            else //pass all itemsInfo to get whole pipettingInfo
            {
                for (int i = 0; i < optCSVFiles.Count; i++)
                {
                    OperationSheet optSheet = new OperationSheet(optCSVFiles[i]);
                    OperationSheetQueueInfo queueInfo = new OperationSheetQueueInfo(optSheet, optCSVFiles[i]);
                    queueInfos.Add(queueInfo);
                    OdSheet odSheet = new OdSheet(odCSVFiles[i], i);
                    itemsInfo.AddRange(optSheet.Items);
                }
            }

            Worklist wklist = new Worklist();
            for (int batchIndex = 0; batchIndex < batchCnt; batchIndex++)
            {
                int startFileIndex = batchIndex * filesPerBatch;
                string sOutputGwlFile = outputFolder + string.Format("{0}.gwl", batchIndex + 1);
                List<string> batchPlateNames = new List<string>();
                List<OperationSheetQueueInfo> batchPlateInfos = new List<OperationSheetQueueInfo>();
                
                for (int i = 0; i < filesPerBatch; i++)
                {
                    int curFileIndex = startFileIndex + i;
                    if (!mix2EPTubes && curFileIndex >= queueInfos.Count)
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
                    wklist.Generate(itemsInfo, sOutputGwlFile);
                    itemsInfo.Clear();
                }
                else
                {
                    throw new Exception("Not support mix to plate!");
                }
                Console.WriteLine(string.Format("Version is: {0}", strings.version));
            }
        }

        private static string GetSrcPlateName(string sFilePath)
        {
            FileInfo fileInfo = new FileInfo(sFilePath);
            string name = fileInfo.Name;
            return name.Substring(0, name.Length - 8);
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
