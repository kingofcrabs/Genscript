using System;
using System.Collections.Generic;
using System.Linq;

namespace genscript
{
	internal class OperationSheetQueueInfo
	{
		public string filePath;

		public int startSubID;

		public int endSubID;

		public string startDstMixWell;

		public OperationSheetQueueInfo(OperationSheet optSheet, string filePath)
		{
			this.filePath = filePath;
			this.startSubID = optSheet.Items.First<ItemInfo>().subID;
			List<ItemInfo> list = (from x in optSheet.Items
			where !Common.IsInvalidWellID(x.sExtraDescription)
			select x).ToList<ItemInfo>();
			if (list.Count > 0)
			{
				this.endSubID = list.Last<ItemInfo>().subID;
			}
			else
			{
				this.endSubID = -1;
			}
			this.startDstMixWell = optSheet.Items.First<ItemInfo>().sExtraDescription;
		}
	}
}
