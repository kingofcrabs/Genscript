using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace HintLabwares
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closed += new EventHandler(MainWindow_Closed);
            ReadInfo();
        }

        private void ReadInfo()
        {
            var plates = File.ReadAllLines(GlobalVars.outputFolder + string.Format("src_{0}.txt", GlobalVars.batchID));
            var dstLabwares = File.ReadAllLines(GlobalVars.outputFolder + string.Format("dst_{0}.txt", GlobalVars.batchID));
            lstSrcPlates.ItemsSource = plates;
            lstDest.ItemsSource = dstLabwares;
            lblDest.Content += " " + dstLabwares.Count().ToString();
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            File.WriteAllText(GlobalVars.resultFile, "true");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
