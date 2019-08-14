using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrcDestViceVerse
{
    public class PlateInfo : BindableBase
    {
        string barcode;
        bool barcodeCorrect;
        bool isSrcPlate;

   

        public PlateInfo(string barcode, bool barcodeCorrect,bool isSrcPlate)
        {
            this.barcode = barcode;
            this.barcodeCorrect = barcodeCorrect;
            this.isSrcPlate = isSrcPlate;
        }

        public string Barcode
        {
            get
            {
                return barcode;
            }
            set
            {
                SetProperty(ref barcode, value);
            }
        }
        public bool BarcodeCorrect
        {
            get
            {
                return barcodeCorrect;
            }
            set
            {
                SetProperty(ref barcodeCorrect, value);
            }
        }

        public bool IsSourcePlate
        {
            get
            {
                return isSrcPlate;
            }
            set
            {
                SetProperty(ref isSrcPlate, value);
            }
        }


    }
}
