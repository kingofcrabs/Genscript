using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;

namespace BGIConverter
{
    class TableReader
    {
        public TableReader(string sCSVPath)
        {
            int startColumnIndex = (int)(ConfigurationManager.AppSettings["startColumn"].ToCharArray().First() - 'A');
            string[] strs = File.ReadAllLines(sCSVPath,Encoding.Default);
            List<List<string>> contentsLists = ParseAllStrings(strs,startColumnIndex);
            List<int> interestingIndexs = new List<int>();
            for (int i = 0; i < contentsLists.Count; i++)
            {
                if (contentsLists[i].Contains("样品位置"))
                {
                    interestingIndexs.Add(i);
                }
            }

            
            foreach (int index in interestingIndexs)
            {
                Console.WriteLine(string.Format("table{0}", index + 1));
                var tmpContentsLists = contentsLists.Skip(index);
                tmpContentsLists = tmpContentsLists.Take(8);
                List<string> debugStrs = new List<string>();
                foreach (var tmpStrs in tmpContentsLists)
                {

                    string tmpStr = "";
                    foreach (string s in tmpStrs)
                    {
                        tmpStr += s + ",";
                    }
                    Console.WriteLine(tmpStr);
                    debugStrs.Add(tmpStr);
#if DEBUG
                    File.WriteAllLines(string.Format("d:\\test{0}.txt", index + 1), debugStrs);
#endif
                }
            }

        }

        private List<List<string>> ParseAllStrings(string[] strs, int startColumnIndex)
        {
            List<List<string>> contentsList = new List<List<string>>();
            foreach (string s in strs)
            {
                string[] tmpStrs = s.Split(',');
                tmpStrs = tmpStrs.Skip(startColumnIndex).ToArray();
                contentsList.Add(tmpStrs.ToList());
            }
            return contentsList;
        }
    }
}
