using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace genscript
{
	public class EVOScriptReader
	{
		private static List<string> labwares;

		public string sScriptFile = ConfigurationManager.AppSettings["scriptFile"];

		public List<string> Labwares
		{
			get
			{
				if (EVOScriptReader.labwares == null)
				{
					this.Read();
				}
				return EVOScriptReader.labwares;
			}
		}

		public void Read()
		{
			if (!File.Exists(this.sScriptFile))
			{
				return;
			}
			List<string> sGridDescriptions = new List<string>();
			List<string> source = File.ReadAllLines(this.sScriptFile).ToList<string>();
			sGridDescriptions = (from s in source
			where s.Contains("998")
			select s).ToList<string>();
			EVOScriptReader.labwares = (from x in this.ParseAll(sGridDescriptions)
			select x.Value.label).ToList<string>();
		}

		private Dictionary<string, LabwareLayoutInfo> ParseAll(List<string> sGridDescriptions)
		{
			Dictionary<string, LabwareLayoutInfo> dictionary = new Dictionary<string, LabwareLayoutInfo>();
			int num = 0;
			int num2 = Math.Min(sGridDescriptions.Count - 1, 69);
			for (int i = 0; i < num2; i++)
			{
				string text = sGridDescriptions[i];
				if (text == "998;0;")
				{
					num++;
				}
				string sLabels = sGridDescriptions[i + 1];
				if (!(text == "998;0;") && !(text == "998;1;") && !(text == "998;4;0;System;"))
				{
					Dictionary<string, LabwareLayoutInfo> second = this.Parse(text, sLabels, num);
					dictionary = dictionary.Union(second).ToDictionary((KeyValuePair<string, LabwareLayoutInfo> p) => p.Key, (KeyValuePair<string, LabwareLayoutInfo> p) => p.Value);
					num++;
					i++;
				}
			}
			return dictionary;
		}

		private Dictionary<string, LabwareLayoutInfo> Parse(string sInnerNames, string sLabels, int grid)
		{
			Dictionary<string, LabwareLayoutInfo> dictionary = new Dictionary<string, LabwareLayoutInfo>();
			string[] array = sInnerNames.Split(new char[]
			{
				';'
			});
			string[] array2 = sLabels.Split(new char[]
			{
				';'
			});
			int num = int.Parse(array[1]);
			for (int i = 0; i < num; i++)
			{
				string inner = array[2 + i];
				string text = array2[1 + i];
				if (!(text == ""))
				{
					dictionary.Add(text, new LabwareLayoutInfo(inner, text, grid, i));
				}
			}
			return dictionary;
		}
	}
}
