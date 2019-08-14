using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utility;

namespace SrcDestViceVerse
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> carouselWantedBarcodes;
        ObservableCollection<string> missingBarcodes;
        ObservableCollection<PlateInfo> onTablePlateInfos = new ObservableCollection<PlateInfo>();
        List<PlateInfo> beforeCarouselPlateArrive = new List<PlateInfo>();
        HashSet<string> expectedBarcodes = new HashSet<string>();
        Worklist wklist;
        bool srcPlateOnTable;
        bool readCarouselBarcodes = false;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Pipeserver.Close();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            try
            {
                SetInfo("只有确定后，才允许读取Carousel条码！");
                CreatePipeServer();
                var itemInfos = ReadExcel();
                srcPlateOnTable = bool.Parse(ConfigurationManager.AppSettings["srcPlateOnTable"]);
                wklist = new Worklist(itemInfos, srcPlateOnTable);
                InitHeader();
                InitPlateOnTable(itemInfos);
                InitPlateOnCarousel(itemInfos);
                HintCount(onTablePlateInfos.Count, missingBarcodes.Count);
                Log("读取excel成功");
            }
            catch(Exception ex)
            {
                SetErrorInfo(ex.Message);
            }
        }

        private void Log(string v)
        {
            txtLog.AppendText(v + "\r\n");
        }

        private void HintCount(int cntOnTable, int cntOnCarousel)
        {
            string s = "";
            if(srcPlateOnTable)
            {
                s = string.Format("worktable上的源板数量：{0},carousel上的目标板数量:{1}", cntOnTable,cntOnCarousel);
            }
            else
                s = string.Format("worktable上的目标板数量：{0},carousel上的源板数量:{1}", cntOnTable, cntOnCarousel);
            SetInfo(s);
        }

        private void InitHeader()
        {
            List<string> headers = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                headers.Add(string.Format("Grid{0}", i + 1));
            }
            lstHeader.ItemsSource = headers;
        }

        private void InitPlateOnCarousel(List<ItemInfo> itemInfos)
        {
            
            if (Worklist.srcPlateOnTable)
            {
                carouselWantedBarcodes = itemInfos.Select(x => x.dstPlateBarcode).ToList();
            }
            else
            {
                carouselWantedBarcodes = itemInfos.Select(x => x.srcPlateBarcode).ToList();
            }
            carouselWantedBarcodes = carouselWantedBarcodes.Distinct().ToList();
            missingBarcodes = new ObservableCollection<string>();
            carouselWantedBarcodes.ForEach(x => missingBarcodes.Add(x));
            //lstOnTablePlateBarcodes.ItemsSource = carouselWantedBarcodes;
            lstMissingBarcodes.ItemsSource = missingBarcodes;
        }

        private void InitPlateOnTable(List<ItemInfo> itemInfos)
        {
            if(Worklist.srcPlateOnTable)
            {
                expectedBarcodes = new HashSet<string>(itemInfos.Select(x => x.srcPlateBarcode));
                int maxSrcPlateCnt = int.Parse(ConfigurationManager.AppSettings["maxSrcPlateCnt"]);
                if (expectedBarcodes.Count > maxSrcPlateCnt)
                    throw new Exception(string.Format("最多只允许{0}个源板",maxSrcPlateCnt));
            }
            else
            {
                expectedBarcodes = new HashSet<string>(itemInfos.Select(x => x.dstPlateBarcode));
                int maxDstPlateCnt = int.Parse(ConfigurationManager.AppSettings["maxDstPlateCnt"]);
                if (expectedBarcodes.Count > maxDstPlateCnt)
                    throw new Exception(string.Format("最多只允许{0}个目标板", maxDstPlateCnt));
            }
            foreach(var barcode in expectedBarcodes)
            {
                onTablePlateInfos.Add(new PlateInfo(barcode, true,Worklist.srcPlateOnTable));
            }
            lstOnTablePlateBarcodes.ItemsSource = onTablePlateInfos;
        }

        private List<ItemInfo> ReadExcel()
        {
           
            string folder = ConfigurationManager.AppSettings["workingFolder"];
            if (!Directory.Exists(folder))
            {
                SetErrorInfo(string.Format("找不到文件夹{0}", folder));
                return null;
            }
            var excelFiles = Directory.EnumerateFiles(folder, "*.xls");
            if(excelFiles.Count() == 0)
            {
                SetErrorInfo(string.Format("找不到Excel{0}", folder));
                return null;
            }
            
            var strLists = ExcelHelper.ReadExcel(excelFiles.First());
            var opSheet = new OperationSheet(strLists);
            return opSheet.GetItemInfos();

        }

        private void SetErrorInfo(string message)
        {
            txtInfo.Text = message;
            txtInfo.Foreground = Brushes.Red;
        }
        private void SetInfo(string message)
        {
            txtInfo.Text = message;
            txtInfo.Foreground = Brushes.Black;
        }

        

        private void CreatePipeServer()
        {
            Pipeserver.ownerInvoker = new Invoker(this);
            ThreadStart pipeThread = new ThreadStart(Pipeserver.createPipeServer);
            Thread listenerThread = new Thread(pipeThread);
            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        internal void ExecuteCommand(string sArg)
        {
            try
            {
                FolderHelper.WriteResult(false);
                ExecuteCommandImpl(sArg);
                ProcessHelper.CloseWaiter("FeedMe");
            }
            catch(Exception ex)
            {
                SetErrorInfo(ex.Message);
                return;
            }
            FolderHelper.WriteResult(true);


        }

        private void ExecuteCommandImpl(string sArg)
        {
            Log(sArg); ;
            if (btnConfirm.IsEnabled)
                throw new Exception("请先确认台面条码！");
            if (sArg.ToLower() == "c")//read carousel
            {
                ReadCarouselBarcodes();
                return;
            }
            else if(sArg.ToLower() == "g")
            {
                List<string> thisBatchPlateBarcodes = GetThisBatchBarcodes(beforeCarouselPlateArrive, onTablePlateInfos);
                wklist.Generate(sArg, onTablePlateInfos.Select(x => x.Barcode).ToList());
                SetInfo("已经生成这一批次的worklist。");
                onTablePlateInfos.Clear();
                beforeCarouselPlateArrive.ForEach(x => onTablePlateInfos.Add(x));
            }
            else
            {
                if (!readCarouselBarcodes)
                    throw new Exception("未设置Carousel条码！");

                SetInfo(string.Format("板子到达{0}", sArg));
                if(onTablePlateInfos.Select(x=>x.Barcode).ToList().Contains(sArg))
                {
                    throw new Exception("板号已经存在！请手工移除该板。");
                }

                bool bFound = false;
                for(int i = 0; i< lstAllBarcodes.Items.Count;i++)
                {
                    if(lstAllBarcodes.Items[i].ToString() == sArg)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (!bFound)
                    throw new Exception(string.Format("找不到条码：{0}", sArg));
                onTablePlateInfos.Add(new PlateInfo(sArg, true, !srcPlateOnTable));
                //
                //
            }
        }

        private List<string> GetThisBatchBarcodes(List<PlateInfo> beforeCarouselPlateArrive, ObservableCollection<PlateInfo> onTablePlateInfos)
        {
            var onTablePlateBarcodes = onTablePlateInfos.Select(x => x.Barcode).ToList();
            List<string> thisBatchBarcodes = new List<string>();
            foreach(var barcode in onTablePlateBarcodes)
            {
                if(!beforeCarouselPlateArrive.Exists(x=>x.Barcode == barcode))
                {
                    thisBatchBarcodes.Add(barcode);
                }
            }
            return thisBatchBarcodes;
        }

        private void ReadCarouselBarcodes()
        {
            string sFile = ConfigurationManager.AppSettings["carouselFile"];
            if (!File.Exists(sFile))
                throw new Exception(string.Format("找不到位于{0}的Carousel条码文件：", sFile));

            var strs = File.ReadAllLines(sFile).ToList();
            List<string> errorStrs = new List<string>();
            strs.RemoveAll(x => string.IsNullOrWhiteSpace(x));
            lstAllBarcodes.ItemsSource = strs;
            missingBarcodes.Clear();

            foreach (var str in strs)
            {
                if (!carouselWantedBarcodes.Contains(str))
                    errorStrs.Add(str);
            }

            foreach(var str in carouselWantedBarcodes)
            {
                if (!strs.Contains(str))
                    missingBarcodes.Add(str);
            }

            if(missingBarcodes.Count == 0 &&errorStrs.Count == 0)
                readCarouselBarcodes = true;
            else
            {
                SetErrorInfo("Carousel条码有问题！");
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if(onTablePlateInfos.Count != expectedBarcodes.Count)
            {
                SetErrorInfo(string.Format("台面上需要{0}块板，实际只有{1}块板！"));
                return;
            }

            for(int i = 0; i< onTablePlateInfos.Count;i++)
            {
                if(!expectedBarcodes.Contains(onTablePlateInfos[i].Barcode))
                {
                    onTablePlateInfos[i].BarcodeCorrect = false;
                    SetErrorInfo(string.Format("excel中不存在台面上需要放置的条码为{0}的板子，！", onTablePlateInfos[i].Barcode));
                    return;
                }
                else
                {
                    onTablePlateInfos[i].BarcodeCorrect = true;
                }
            }

            for(int i = 0; i<onTablePlateInfos.Count; i++)
            {
                for(int j = i+1; j < onTablePlateInfos.Count; j++ )
                {

                    if(onTablePlateInfos[j].Barcode == onTablePlateInfos[i].Barcode)
                    {
                        onTablePlateInfos[j].BarcodeCorrect = false;
                        SetErrorInfo("条码重复！");
                        return;
                    }
                }
            }

            beforeCarouselPlateArrive.Clear();
            foreach (var plateInfo in onTablePlateInfos)
            {
                beforeCarouselPlateArrive.Add(plateInfo);
            }
            btnConfirm.IsEnabled = false;
            SetInfo("可以读取Carousel条码了。");
            Log("确认worktable上的条码。");
        }
    }
}
