using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace SrcDestViceVerse
{
    class OperationSheet
    {
        List<List<string>> allRowStrs;
     
        public OperationSheet(List<List<string>> allRowStrs)
        {
            this.allRowStrs = allRowStrs;
         
        }

     
        public List<ItemInfo> GetItemInfos()
        {

            List<ItemInfo> itemInfos = new List<ItemInfo>();
            List<string> meaningfulStrs = new List<string>();
            
            int lineIndex = 0;
            foreach (var thisRowStrs in allRowStrs)
            {
                ItemInfo itemInfo = new ItemInfo();
                try
                {
                    ParseRow(thisRowStrs, ref itemInfo);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("line {0} has error! ", lineIndex + 2) + ex.Message);
                }

                itemInfos.Add(itemInfo);
                lineIndex++;
            }
            return itemInfos;
        }

        private void ParseRow(List<string> thisRowStrs, ref ItemInfo itemInfo)
        {
            itemInfo.srcPlateBarcode = thisRowStrs[(int)ColumnIndexDefinition.srcPlateBarcode];
            itemInfo.dstPlateBarcode = thisRowStrs[(int)ColumnIndexDefinition.dstPlateBarcode];
            string sSrcWellID = thisRowStrs[(int)ColumnIndexDefinition.srcWellID];
            
            itemInfo.srcWellID =  Common96.GetWellID(sSrcWellID);
            string sDstWellID = thisRowStrs[(int)ColumnIndexDefinition.dstWellID];
       
            itemInfo.dstWellID = Common96.GetWellID(sDstWellID);
            string vol = thisRowStrs[(int)ColumnIndexDefinition.volume];
            itemInfo.volumeUL = int.Parse(vol);
        }

       
    }
}
