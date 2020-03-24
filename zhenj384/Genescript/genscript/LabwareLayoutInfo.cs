using System;

namespace genscript
{
	public class LabwareLayoutInfo
	{
		public string innerName;

		public string label;

		public int grid;

		public int site;

		public LabwareLayoutInfo()
		{
			this.innerName = "Unknown";
			this.label = "Unknown 1";
			this.grid = 1;
			this.site = 1;
		}

		public LabwareLayoutInfo(string inner, string l, int g, int s)
		{
			this.innerName = inner;
			this.label = l;
			this.grid = g;
			this.site = s;
		}
	}
}
