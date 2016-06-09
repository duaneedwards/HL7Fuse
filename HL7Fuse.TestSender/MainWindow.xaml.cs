using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using NHapi.Base.Model;
using NHapi.Base.Util;
using NHapiTools.Base.Net;

namespace HL7Fuse.TestSender
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private void btnClose_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void btnSend_Click(object sender, RoutedEventArgs e)
      {
         try
         {
            var toSend = txtMessage.Text;

            string hostname = txtRemoteHost.Text;
            int port = 0;
            if (!int.TryParse(txtRemotePort.Text, out port))
            {
               MessageBox.Show(string.Format("Remote Port {0} is not a number", txtRemotePort.Text));
               return;
            }

            var mllpClient = new SimpleMLLPClientThatTimesOut(hostname, port);
            string response = mllpClient.SendHL7Message(toSend);

            MessageBox.Show(response);

            //Terser terser = new Terser(response);
            //string ackCode = terser.Get("/MSA-1");

            //bool acked = (ackCode == "AA");
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.ToString());
         }
      }

      private void btnLoadMessage_Click(object sender, RoutedEventArgs e)
      {
         Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

         dlg.DefaultExt = "*.*";
         dlg.Filter = "Any File (*.*)|*.*";

         Nullable<bool> result = dlg.ShowDialog();
         
         if (result == true)
         {
            string filename = dlg.FileName;
            txtMessage.Text = File.ReadAllText(filename);
         }
      }
   }
}
