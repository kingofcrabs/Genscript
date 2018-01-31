using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;

namespace genscript384
{
    public class EVOScriptReader
    {
        static List<string> labwares = null;
        public string sScriptFile = ConfigurationManager.AppSettings["scriptFile"];
        public List<string> Labwares
        {
            get
            {
                if (labwares == null)
                    Read();
                return labwares;
            }
        }
        
        public void Read()
        {
            if (!File.Exists(sScriptFile))
                return;
            List<string> sGridDescriptions = new List<string>();
            List<string> sContents = File.ReadAllLines(sScriptFile).ToList();
            sGridDescriptions = sContents.Where(s => s.Contains("998")).ToList();
            labwares = ParseAll(sGridDescriptions).Select(x => x.Value.label).ToList();
        }

        private Dictionary<string, LabwareLayoutInfo> ParseAll(List<string> sGridDescriptions)
        {
            Dictionary<string, LabwareLayoutInfo> label_basicDef_dict
                = new Dictionary<string, LabwareLayoutInfo>();

            int grid = 0;
            int maxGrid = Math.Min(sGridDescriptions.Count - 1, 69);
            for (int i = 0; i < maxGrid; i++)
            {
                string s = sGridDescriptions[i];
                if (s == "998;0;")
                    grid++;
                string sLabels = sGridDescriptions[i + 1];

                if (s == "998;0;" || s == "998;1;" || s == "998;4;0;System;")
                    continue;
                Dictionary<string, LabwareLayoutInfo> tmpDict = Parse(s, sLabels, grid);
                label_basicDef_dict = label_basicDef_dict.Union(tmpDict).ToDictionary(p => p.Key, p => p.Value);
                grid++;
                i++;
            }
            return label_basicDef_dict;
        }

        private Dictionary<string, LabwareLayoutInfo> Parse(string sInnerNames, string sLabels, int grid)
        {
            Dictionary<string, LabwareLayoutInfo> tmpDict = new Dictionary<string, LabwareLayoutInfo>();
            string[] innerNames = sInnerNames.Split(';');
            string[] labels = sLabels.Split(';');
            int nCount = int.Parse(innerNames[1]);
            for (int i = 0; i < nCount; i++)
            {
                string innerName = innerNames[2 + i];
                string label = labels[1 + i];
                if (label == "")
                    continue;
                tmpDict.Add(label, new LabwareLayoutInfo(innerName, label, grid, i));
            }
            return tmpDict;
        }
    }
    public class LabwareLayoutInfo
    {
        public string innerName;
        public string label;
        public int grid;
        public int site;

        public LabwareLayoutInfo()
        {
            innerName = "Unknown";
            label = "Unknown 1";
            grid = 1;
            site = 1;
        }


        public LabwareLayoutInfo(string inner, string l, int g, int s)
        {
            innerName = inner;
            label = l;
            grid = g;
            site = s;
        }
    }
}
