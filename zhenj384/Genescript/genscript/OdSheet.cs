using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace genscript
{
	internal class OdSheet
	{
		private static int headIndex = 53;

		public static Dictionary<string, Dictionary<int, int>> eachPlateID_Vols = new Dictionary<string, Dictionary<int, int>>();

		public void ReadInfo(string sCSVFile, int plateIndex)
		{
			string plateName = Common.GetPlateName(sCSVFile);
			if (OdSheet.eachPlateID_Vols.ContainsKey(plateName))
			{
				return;
			}
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			List<string> list = File.ReadAllLines(sCSVFile).ToList<string>();
			string value = list[OdSheet.headIndex];
			list = list.GetRange(OdSheet.headIndex + 1, Common.rows384);
			Console.WriteLine("OD values are as following:");
			Console.WriteLine(value);
			int num = 0;
			foreach (string current in list)
			{
				Console.WriteLine(current);
				List<string> list2 = current.Split(new char[]
				{
					','
				}).ToList<string>();
				list2 = list2.GetRange(1, Common.cols384);
				for (int i = 0; i < Common.cols384; i++)
				{
					int wellID = Common.GetWellID384(num, i);
					int value2 = 0;
					if (list2[i] != "")
					{
						value2 = int.Parse(list2[i]);
					}
					dictionary.Add(wellID, value2);
				}
				num++;
			}
			OdSheet.eachPlateID_Vols.Add(plateName, dictionary);
			Console.WriteLine("OD End");
		}
	}
}
