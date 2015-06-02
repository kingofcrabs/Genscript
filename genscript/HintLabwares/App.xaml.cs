using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;

namespace HintLabwares
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if(e.Args.Count() == 0)
            {
                File.WriteAllText(GlobalVars.resultFile, "false");
                return;
            }
            else
            {
                GlobalVars.batchID = int.Parse(e.Args[0]);
                MainWindow mainForm = new MainWindow();
                mainForm.ShowDialog();
            }
            
        }   
    }
}
