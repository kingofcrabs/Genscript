namespace SrcDestViceVerse
{

    enum ColumnIndexDefinition
    {
        srcPlateBarcode = 0,
        srcWellID ,
        dstPlateBarcode,
        dstWellID,
        volume,

    };


    public enum DstLabwareType
    {
        Well96 = 0,
        Well384,
    };

    public class ItemInfo
    {
        public int srcWellID;
        public int dstWellID;
        public int volumeUL;
        public string srcPlateBarcode;
        public string dstPlateBarcode;

    }
}