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
            endSubID = optSheet.Items.Last().subID;
            startDstMixWell = optSheet.Items.First().sExtraDescription;
        }
    }
}
