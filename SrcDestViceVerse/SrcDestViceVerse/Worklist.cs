using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace SrcDestViceVerse
{
    class Worklist
    {
      
        Dictionary<string, string> barcode_Label;
        public static int finishedBarcodeCnt = 0;
        List<ItemInfo> itemInfos;
        public static bool srcPlateOnTable;
        public Worklist(List<ItemInfo> itemInfos,bool isSrcPlateOnTable)
        {
            this.itemInfos = itemInfos;
            srcPlateOnTable = isSrcPlateOnTable;
        }


        public void Generate(List<string> thisBatchPlateBarcodes, List<string> onTablePlateBarcodes)
        {
            PrepareDictionary(onTablePlateBarcodes);
            List<string> strs = new List<string>();
            List<ItemInfo> thisBatchItemInfos = new List<ItemInfo>();
            foreach(var plateBarcode in thisBatchPlateBarcodes)
            {
                var thisPlateItemInfos = srcPlateOnTable ? itemInfos.Where(x => x.dstPlateBarcode == plateBarcode).ToList() :
                itemInfos.Where(x => x.srcPlateBarcode == plateBarcode).ToList();
                thisBatchItemInfos.AddRange(thisPlateItemInfos);
            }
            
            foreach (var itm in thisBatchItemInfos)
            {
                strs.Add(Format(itm,srcPlateOnTable));
            }
            string wklistPath = FolderHelper.GetOutputFolder() + "wklist.gwl";
            File.WriteAllLines(wklistPath, strs);
        }

        public static void ClearWklist()
        {
            string wklistPath = FolderHelper.GetOutputFolder() + "wklist.gwl";
            if (File.Exists(wklistPath))
                File.Delete(wklistPath);
        }

        private void PrepareDictionary(List<string> onTableBarcodes)
        {
            barcode_Label = new Dictionary<string, string>();
            int ID = 1;
            foreach (var barcode in onTableBarcodes)
            {
                barcode_Label.Add(barcode, string.Format("plate{0}", ID++));
            }
        }

        private string Format(ItemInfo itm, bool srcOnTable)
        {
            string srcLabel = "";
            string dstLabel = "";
            if (srcOnTable) //not on shelf
            {
                srcLabel = Translate2LabwareLabel(itm.srcPlateBarcode);
                dstLabel = "dst";
            }
            else
            {
                srcLabel = "src";
                dstLabel = Translate2LabwareLabel(itm.dstPlateBarcode);
            }
            return string.Format("{0},{1},{2},{3},{4}", srcLabel, itm.srcWellID, itm.volumeUL, dstLabel, itm.dstWellID);
        }

        private string Translate2LabwareLabel(string sBarcode)
        {
            return barcode_Label[sBarcode];
        }
    }
}
