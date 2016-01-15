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
        public string startDstMixWell;
    
        public OperationSheetQueueInfo(OperationSheet optSheet, string filePath)
        {
            // TODO: Complete member initialization
            this.filePath = filePath;
            startSubID = optSheet.Items.First().subID;
            var validItems = optSheet.Items.Where(x=>!Common.IsInvalidWellID(x.sExtraDescription)).ToList();
            if (validItems.Count > 0)
                endSubID = validItems.Last().subID;
            else
                endSubID = -1;
            startDstMixWell = optSheet.Items.First().sExtraDescription;
        }
    }
}
