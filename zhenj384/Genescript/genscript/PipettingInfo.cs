using System;

namespace genscript
{
	internal class PipettingInfo
	{
		public string sPrimerID;

		public string srcLabware;

		public int srcWellID;

		public string dstLabware;

		public int dstWellID;

		public double vol;

		public int orgDstWellID;

		public PipettingInfo(string sPrimerID, string srcLabware, int srcWell, string dstLabware, int dstWell, double v)
		{
			this.sPrimerID = sPrimerID;
			this.srcLabware = srcLabware;
			this.dstLabware = dstLabware;
			this.srcWellID = srcWell;
			this.dstWellID = dstWell;
			this.vol = v;
			this.orgDstWellID = -1;
		}

		public PipettingInfo(PipettingInfo pipettingInfo)
		{
			this.sPrimerID = pipettingInfo.sPrimerID;
			this.srcLabware = pipettingInfo.srcLabware;
			this.dstLabware = pipettingInfo.dstLabware;
			this.srcWellID = pipettingInfo.srcWellID;
			this.dstWellID = pipettingInfo.dstWellID;
			this.vol = pipettingInfo.vol;
			this.orgDstWellID = pipettingInfo.orgDstWellID;
		}
	}
}
