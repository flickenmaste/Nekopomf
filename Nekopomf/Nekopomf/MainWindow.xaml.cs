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

            // http://stackoverflow.com/questions/10230579/easiest-way-to-have-a-program-minimize-itself-to-the-system-tray-using-net-4
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("./eek.ico");
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
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.V))
            {
                if (System.Windows.Clipboard.ContainsImage())
                {
                    string fileUrl = null;
                    BitmapSource bitmapSource = System.Windows.Clipboard.GetImage();

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    MemoryStream memoryStream = new MemoryStream();
                    BitmapImage bImg = new BitmapImage();

                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    //encoder.Save(memoryStream);

                    string path = "./temp.jpg";
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                        fileStream.Close();
                    }

                    // credit to https://github.com/mavanmanen/Scotty/blob/master/Scotty/Program.cs for this stuff
                    FileStream fileStream2 = new FileStream(path, FileMode.Open, FileAccess.Read);

                    byte[] fileArray = new byte[fileStream2.Length];
                    fileStream2.Read(fileArray, 0, System.Convert.ToInt32(fileStream2.Length));
                    fileStream2.Close();
                    fileStream2.Dispose();

                    byte[] xArray = Encoding.ASCII.GetBytes("------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"id\"\r\n\r\n\r\n------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"files[]\"; filename=\"temp.jpg\"\r\nContent-type: image/jpg\r\n\r\n");
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

                    URLBox.Text = "http://a.pomf.se/" + fileUrl;
                    File.Delete(path);
                }
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }
    }
}
