using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utility
{
    public class FolderHelper
    {
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

        static public string GetConfigFolder()
        {
            string sConfigFolder = GetExeParentFolder() + "Config\\";
            CreateIfNotExist(sConfigFolder);
            return sConfigFolder;
        }

        private static void CreateIfNotExist(string sFolder)
        {
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
        }

        public static string GetOutputFolder()
        {
            string sExeParent = GetExeParentFolder();
            string sOutputFolder = sExeParent + "Output\\";
            CreateIfNotExist(sOutputFolder);
            return sOutputFolder;
        }

        public static void WriteResult(bool bok)
        {
            File.WriteAllText(GetOutputFolder() + "result.txt", bok.ToString());
        }
    }

    public class ProcessHelper
    {
        public static void CloseWaiter(string windowTitle)
        {
            Thread.Sleep(1000);
            windowTitle = windowTitle.ToLower();
            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    if (process.MainWindowTitle.ToLower().Contains(windowTitle))
                    {
                        process.CloseMainWindow();
                    }
                }
            }
        }
    }
    public class ExcelHelper
    {
        public static List<List<string>> ReadExcel(string excelFile)
        {
          

            if (!File.Exists(excelFile))
                throw new Exception("cannot find the excel file");

            int pos = excelFile.IndexOf(".xls");
            if (pos == -1)
            {
                throw new Exception("Cannot find xls in file name!");
            }
                

            Application app = new Application();
            app.Visible = false;
            app.DisplayAlerts = false;
            Workbook workbook = app.Workbooks.Open(excelFile);
            var sheets = workbook.Worksheets;
            Worksheet worksheet = (Worksheet)sheets.get_Item(1);//读取第一张表
            int rowsCount = worksheet.UsedRange.Rows.Count;
            int colsCount = worksheet.UsedRange.Columns.Count;
            Range a2 = (Microsoft.Office.Interop.Excel.Range)worksheet.Cells[1, 1];
            Range endCell = (Microsoft.Office.Interop.Excel.Range)worksheet.Cells[rowsCount, colsCount];
            Range rng = (Microsoft.Office.Interop.Excel.Range)worksheet.get_Range(a2, endCell);
            object[,] exceldata = (object[,])rng.get_Value(Microsoft.Office.Interop.Excel.XlRangeValueDataType.xlRangeValueDefault);
            List<List<string>> allRowStrs = new List<List<string>>();
            for (int r = 0; r < rowsCount; r++)
            {
                List<string> thisRowStrs = new List<string>();
                if (exceldata[r + 1, 1] == null || string.IsNullOrEmpty(exceldata[r + 1, 1].ToString()))
                    break;
                for (int c = 0; c < colsCount; c++)
                {
                    string content = "";
                    if (exceldata[r + 1, c + 1] != null)
                    {
                        content = exceldata[r + 1, c + 1].ToString();
                    }
                    thisRowStrs.Add(content);
                }
                allRowStrs.Add(thisRowStrs);
            }
            app.Quit();
            Console.WriteLine("read excel successfully!");
            return allRowStrs.Skip(1).ToList();

        }
    }
}
