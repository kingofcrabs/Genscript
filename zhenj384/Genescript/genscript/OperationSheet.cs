using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace genscript
{
	internal class OperationSheet
	{
		private int startIndex = 8;

		private int cnt = 192;

		private int extraDescriptionColumn = 7;

		private int IDColumn;

		private int srcWellColumn = 6;

		private string sPlateName = "";

		public static string empty = "empty";

		private List<ItemInfo> itemsInfo;

		public List<ItemInfo> Items
		{
			get
			{
				return this.itemsInfo;
			}
		}

		public OperationSheet(string sCSVFile)
		{
			this.sPlateName = Common.GetPlateName(sCSVFile);
			List<string> list = File.ReadAllLines(sCSVFile).ToList<string>();
			list = list.GetRange(this.startIndex, this.cnt);
			List<List<string>> halfStrLists = this.GetHalfStrLists(list, true);
			foreach (List<string> current in halfStrLists)
			{
				string text = "";
				foreach (string current2 in current)
				{
					text += current2;
					text += ",";
				}
				Console.WriteLine(text);
			}
			List<List<string>> halfStrLists2 = this.GetHalfStrLists(list, false);
			foreach (List<string> current3 in halfStrLists)
			{
				string text2 = "";
				foreach (string current4 in current3)
				{
					text2 += current4;
					text2 += ",";
				}
				Console.WriteLine(text2);
			}
			this.itemsInfo = new List<ItemInfo>();
			halfStrLists.AddRange(halfStrLists2);
			try
			{
				this.itemsInfo.AddRange(this.GetItemsInfo(halfStrLists));
			}
			catch (Exception ex)
			{
				throw new Exception("Invalid file: " + sCSVFile + ex.Message);
			}
		}

		private List<ItemInfo> GetItemsInfo(List<List<string>> strLists)
		{
			List<ItemInfo> list = new List<ItemInfo>();
			string text = "";
			string text2 = strLists.First<List<string>>()[0];
			text2 = this.GetMainIndex(text2);
			foreach (List<string> current in strLists)
			{
				string mainIndex = this.GetMainIndex(current[this.IDColumn]);
				if (mainIndex != text2)
				{
					text2 = mainIndex;
					if (current[this.extraDescriptionColumn] == "")
					{
						current[this.extraDescriptionColumn] = OperationSheet.empty;
					}
				}
			}
			foreach (List<string> current2 in strLists)
			{
				if (current2[this.extraDescriptionColumn] != string.Empty)
				{
					text = current2[this.extraDescriptionColumn];
				}
				if (!(current2[0] == ""))
				{
					this.CheckExtraDescription(text);
					list.Add(this.GetItemInfo(current2, text));
				}
			}
			return list;
		}

		private void CheckExtraDescription(string s)
		{
			string str = "The description is: " + s;
			int num = s.IndexOf("**");
			int num2 = s.LastIndexOf("**");
			if (num2 != s.Length - 2)
			{
				throw new Exception("Invalid remarks! Last two chars is NOT '**'" + str);
			}
			if (num == -1)
			{
				throw new Exception("Invalid remarks! first two chars is NOT '**'" + str);
			}
			if (num2 == num)
			{
				throw new Exception("Invalid remarks! Only one ** found!" + str);
			}
		}

		private string GetMainIndex(string sCurrentIndex)
		{
			string[] source = sCurrentIndex.Split(new char[]
			{
				'_'
			});
			return source.First<string>();
		}

		private ItemInfo GetItemInfo(List<string> strs, string sExtraDescription)
		{
			ItemInfo itemInfo = new ItemInfo();
			itemInfo.sExtraDescription = sExtraDescription;
			itemInfo.sID = strs[this.IDColumn];
			this.ParseID(itemInfo.sID, ref itemInfo);
			itemInfo.srcWellID = Common.GetWellID(strs[this.srcWellColumn]);
			return itemInfo;
		}

		private void ParseID(string src, ref ItemInfo itemInfo)
		{
			List<string> list = src.Split(new char[]
			{
				'_'
			}).ToList<string>();
			itemInfo.plateName = this.sPlateName;
			itemInfo.mainID = list[0];
			itemInfo.subID = int.Parse(list[1]);
		}

		private List<List<string>> GetHalfStrLists(List<string> strs, bool firstHalf = true)
		{
			List<List<string>> list = new List<List<string>>();
			foreach (string current in strs)
			{
				if (firstHalf)
				{
					list.Add(this.GetHalf(current, true));
				}
				else
				{
					list.Add(this.GetHalf(current, false));
				}
			}
			return list;
		}

		private List<string> GetHalf(string s, bool bFirstHalf)
		{
			int num = 0;
			int endIndex = 8;
			if (!bFirstHalf)
			{
				num = 9;
				endIndex = 16;
			}
			return this.GetSubStrs(s, num, endIndex);
		}

		private List<string> GetSubStrs(string s, int startIndex, int endIndex)
		{
			List<string> list = s.Split(new char[]
			{
				','
			}).ToList<string>();
			return list.GetRange(startIndex, endIndex - startIndex + 1);
		}

		private List<string> GetFirstHalf(string s)
		{
			return this.GetSubStrs(s, 0, 8);
		}
	}
}
