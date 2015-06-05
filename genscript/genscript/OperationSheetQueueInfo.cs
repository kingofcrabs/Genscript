using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace genscript
{
    class OperationSheetQueueInfo
    {
        public string filePath;
        public int startSubID;
        public int endSubID;
        public int startDstMixWell;
        public OperationSheetQueueInfo(OperationSheet optSheet, string filePath)
        {
            // TODO: Complete member initialization
            startDstMixWell = Common.GetWellID(optSheet.Items[0].sExtraDescription);
            this.filePath = filePath;
            startSubID = optSheet.Items.First().subID;
            endSubID = optSheet.Items.Last().subID;
        }
       
    }
}
