using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mix2Plate
{
    class OdSheet
    {
        static int headIndex = 37;
        //static public List<int> vals = new List<int>();
        //static public List<List<int>> eachPlateVals = new List<List<int>>();
        static public Dictionary<string, Dictionary<int, int>> eachPlateID_Vols = new Dictionary<string, Dictionary<int, int>>(); 

        public OdSheet(string sCSVFile, int plateIndex)
        {
            var plateName = Common.GetPlateName(sCSVFile);
            if(eachPlateID_Vols.ContainsKey(plateName))
                return;

            List<string> strs = File.ReadAllLines(sCSVFile).ToList();
            string headContent = strs[headIndex];
            strs = strs.GetRange(headIndex+1, Common.rows);
            Dictionary<int, int> pos_vals = new Dictionary<int, int>();
            Console.WriteLine("OD values are as following:");
            Console.WriteLine(headContent);
            int curRowIndex = 0;
            foreach (string s in strs)
            {
                Console.WriteLine(s);
                List<string> thisLineStrs = s.Split(',').ToList();
                thisLineStrs = thisLineStrs.GetRange(1, Common.cols);
                for (int i = 0; i < Common.cols; i++)
                {
                    int curWellID = Common.GetWellID(curRowIndex,i);
                    int vol = 0;
                    if( thisLineStrs[i] != "")
                        vol = int.Parse(thisLineStrs[i]);
                    pos_vals.Add(curWellID, vol);
                }
                curRowIndex++;
            }
            //eachPlateVals.Add( pos_vals.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value).Values.ToList());
            eachPlateID_Vols.Add(plateName, pos_vals);
            Console.WriteLine("OD End");
        }

        
    }
}
