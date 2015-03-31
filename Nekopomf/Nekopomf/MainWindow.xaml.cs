using System;
using System.Collections.Generic;
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
using System.Net;
using System.IO;
// https://www.nuget.org/packages/Newtonsoft.Json/
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using NHotkey;

namespace Nekopomf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            URLBox.IsEnabled = false;

            KeyBinding upload = new KeyBinding();
            NHotkey.Wpf.HotkeyManager.Current.AddOrReplace("Upload", Key.Add, ModifierKeys.Control | ModifierKeys.Alt, UploadScreenShot);

            BitmapImage bimage = new BitmapImage();
            bimage.BeginInit();
            bimage.UriSource = new Uri("./Resources/yayoi.png", UriKind.Relative);  // picture in window
            bimage.EndInit();
            Girl.Source = bimage;

            BitmapImage bimage2 = new BitmapImage();
            bimage2.BeginInit();
            bimage2.UriSource = new Uri("./Resources/logo.png", UriKind.Relative);  // picture in window
            bimage2.EndInit();
            Logo.Source = bimage2;

            // http://stackoverflow.com/questions/10230579/easiest-way-to-have-a-program-minimize-itself-to-the-system-tray-using-net-4
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("./Resources/eek.ico");   // tray icon
            ni.Visible = true;
            ni.DoubleClick +=
                delegate(object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        private void UploadScreenShot(object sender, HotkeyEventArgs e)
        {
            Upload myUpload = new Upload();
            URLBox.IsEnabled = true;
            myUpload.UploadPNG();
            URLBox.IsEnabled = false;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.V))   // if ctrl+v hit upload paste
            {
                Upload myUpload = new Upload();
                URLBox.IsEnabled = true;
                myUpload.UploadPNG();
                URLBox.IsEnabled = false;
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)    // Minimize to tray

                this.Hide();

            base.OnStateChanged(e);
        }
    }
}
