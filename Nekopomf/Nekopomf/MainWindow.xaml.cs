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

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.V))   // if ctrl+v hit upload paste
            {
                if (System.Windows.Clipboard.ContainsImage())
                {
                    URLBox.Text = "";
                    string fileUrl = null;
                    BitmapSource bitmapSource = System.Windows.Clipboard.GetImage();

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    MemoryStream memoryStream = new MemoryStream();
                    BitmapImage bImg = new BitmapImage();

                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    //encoder.Save(memoryStream);

                    string path = "./temp.png";
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fileStream);   // Save temp img
                        fileStream.Close();
                    }

                    #region Upload
                    // credit to https://github.com/mavanmanen/Scotty/blob/master/Scotty/Program.cs for this stuff
                    FileStream fileStream2 = new FileStream(path, FileMode.Open, FileAccess.Read);

                    byte[] fileArray = new byte[fileStream2.Length];
                    fileStream2.Read(fileArray, 0, System.Convert.ToInt32(fileStream2.Length));
                    fileStream2.Close();
                    fileStream2.Dispose();

                    byte[] xArray = Encoding.ASCII.GetBytes("------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"id\"\r\n\r\n\r\n------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"files[]\"; filename=\"temp.png\"\r\nContent-type: image/png\r\n\r\n");
                    byte[] boundaryByteArray = Encoding.ASCII.GetBytes("\r\n------BOUNDARYBOUNDARY----");

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://pomf.se/upload.php");
                    request.Method = "POST";

                    //Client
                    request.Accept = "*/*";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.102 Safari/537.36";

                    //Entity
                    request.ContentLength = fileArray.Length + xArray.Length + boundaryByteArray.Length;
                    request.ContentType = "multipart/form-data; boundary=----BOUNDARYBOUNDARY----";

                    //Miscellaneous                
                    request.Referer = "http://pomf.se/";

                    //Transport
                    request.KeepAlive = true;

                    string responseContent = null;

                    try
                    {
                        Stream requestStream = request.GetRequestStream();
                        requestStream.Write(xArray, 0, xArray.Length);
                        requestStream.Write(fileArray, 0, fileArray.Length);
                        requestStream.Write(boundaryByteArray, 0, boundaryByteArray.Length);
                        requestStream.Close();

                        WebResponse response = request.GetResponse();
                        requestStream = response.GetResponseStream();

                        StreamReader responseReader = new StreamReader(requestStream);
                        responseContent = responseReader.ReadToEnd();

                        responseReader.Close();
                        requestStream.Close();
                        response.Close();

                        responseReader.Dispose();
                        requestStream.Dispose();
                        response.Dispose();
                        request = null;
                        xArray = null;
                        fileArray = null;
                        boundaryByteArray = null;
                    }
                    catch (Exception eeek)
                    {
                        Console.Write(eeek);
                    }

                    if (responseContent != null)
                    {
                        JObject json = JObject.Parse(responseContent);
                        if (Convert.ToBoolean(json["success"]))
                        {
                            JArray files = (JArray)json["files"];
                            fileUrl = (string)files[0]["url"];
                            files = null;
                        }
                        json = null;
                    }
                    #endregion

                    URLBox.Text = "http://a.pomf.se/" + fileUrl + " has been copied to the clipboard.";
                    if (AudioCheck.IsChecked != true)
                        (new System.Media.SoundPlayer("./Resources/eye.wav")).Play();   // Play audio
                    System.Windows.Clipboard.SetText("http://a.pomf.se/" + fileUrl, System.Windows.TextDataFormat.Text);    // Copy to clipboard

                    if (LogCheck.IsChecked == true)
                    {
                        using (var fileStream = new FileStream("./log.txt", FileMode.Append))   // Save log of pastes
                        {
                            string linky = "http://a.pomf.se/" + fileUrl + " " + System.DateTime.Now.ToString() + Environment.NewLine;
                            byte[] linkyArray = Encoding.ASCII.GetBytes(linky);
                            fileStream.Write(linkyArray, 0, linkyArray.Length);
                            fileStream.Close();
                        }
                    }
                    
                    if (LocalCopyCheck.IsChecked == true)
                    {
                        string localCopyPath = "./saved/" + fileUrl + ".png";
                        using (var fileStream = new FileStream(localCopyPath, FileMode.Create)) // Save local copy of paste
                        {
                            PngBitmapEncoder localEncoder = new PngBitmapEncoder();
                            localEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                            localEncoder.Save(fileStream);
                            fileStream.Close();
                        }
                    }

                    File.Delete(path);  // Delete temp image
                }
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
