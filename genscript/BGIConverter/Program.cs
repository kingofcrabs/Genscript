using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Configuration;

namespace BGIConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string runningDefinitionFile = GetRunningDefFile();
            Console.WriteLine(string.Format("About to process the following file: {0}",runningDefinitionFile));
            string sCSVFile = SaveAsCSV(runningDefinitionFile);
            TableReader tableReader = new TableReader(sCSVFile);
        }

        static public string GetExeParentFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int index = s.LastIndexOf("\\");
            return s.Substring(0, index) + "\\";
        }

        static public string GetRunningDefFile()
        {
            string workingFolder = ConfigurationManager.AppSettings["workingFolder"];
            
            string keyword = "*.xlsx";
            List<string> files = Directory.EnumerateFiles(workingFolder, keyword).ToList();
            if (files.Count == 0)
                throw new Exception("No run definition file found!");

            var directory = new DirectoryInfo(workingFolder);
            var myFile = directory.GetFiles(keyword)
             .OrderByDescending(f => f.LastWriteTime)
             .First();
            return myFile.FullName;
        }

        private static string SaveAsCSV(string sheetPath)
        {
            int pos = sheetPath.IndexOf(".xls");
            if (pos == -1)
                throw new Exception("invalid file, must has suffix with .xls");

            Application app = new Application();
            app.Visible = false;
            app.DisplayAlerts = false;
            Workbook wbWorkbook = app.Workbooks.Open(sheetPath, CorruptLoad: true);
            string sWithoutSuffix = "";
            sWithoutSuffix = sheetPath.Substring(0, pos);
            string sCSVFile = sWithoutSuffix + ".csv";
            wbWorkbook.SaveAs(sCSVFile, XlFileFormat.xlCSV);
            wbWorkbook.Close(false, "", true);
            app.Quit();
            return sCSVFile;
        }
    }
}
