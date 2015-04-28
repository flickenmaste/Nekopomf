﻿using System;
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
    class Upload
    {
        public static bool UploadFile(string path)
        {
            const int NEKOPOMF_FILESIZELIMIT = 52428800;    // that's 50MB if we're talking binary

            // check if we are within nekopomf's file upload limit
            if (new FileInfo(path).Length > NEKOPOMF_FILESIZELIMIT)
            {
                ((MainWindow) System.Windows.Application.Current.MainWindow).URLBox.Text =
                    "The file you are trying to upload is too big for pomf.se! (50MB)";
                return false;
            }

            string fileUrl = null;
            string fileName = System.IO.Path.GetFileName(path);

            MemoryStream memoryStream = new MemoryStream();

            #region Upload
            // credit to https://github.com/mavanmanen/Scotty/blob/master/Scotty/Program.cs for this stuff

            // byte data for file to upload
            byte[] fileArray;
            using (var fileStream2 = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileArray = new byte[fileStream2.Length];
                fileStream2.Read(fileArray, 0, System.Convert.ToInt32(fileStream2.Length));
                fileStream2.Close();
                fileStream2.Dispose();
            }
            
            // byte data for header
            byte[] xArray = Encoding.ASCII.GetBytes("------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"id\"\r\n\r\n\r\n------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"files[]\"; filename=\"" +
                                                    fileName + "\"\r\nContent-type: application/octet-stream\r\n\r\n");

            // byte data for boundary
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
                return false;
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

            // show URL to upload
            ((MainWindow)System.Windows.Application.Current.MainWindow).URLBox.Text = "http://a.pomf.se/" + fileUrl + " has been copied to the clipboard.";
            
            // play upload sound
            if (((MainWindow)System.Windows.Application.Current.MainWindow).AudioCheck.IsChecked != true)
                (new System.Media.SoundPlayer("./Resources/eye.wav")).Play();   // Play audio
            System.Windows.Clipboard.SetText("http://a.pomf.se/" + fileUrl, System.Windows.TextDataFormat.Text);    // Copy to clipboard

            // copy upload URL into log
            if (((MainWindow)System.Windows.Application.Current.MainWindow).LogCheck.IsChecked == true)
            {
                using (var fileStream = new FileStream("./log.txt", FileMode.Append))   // Save log of pastes
                {
                    string linky = "http://a.pomf.se/" + fileUrl + " " + System.DateTime.Now.ToString() + Environment.NewLine;
                    byte[] linkyArray = Encoding.ASCII.GetBytes(linky);
                    fileStream.Write(linkyArray, 0, linkyArray.Length);
                    fileStream.Close();
                }
            }

            return true;
        }

        // Uploads a single bitmap image to pomf.se
        public static void UploadPNG(BitmapSource bitmapSource)
        {
            string fileUrl = null;

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bImg = new BitmapImage();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            //encoder.Save(memoryStream);

            Directory.CreateDirectory(System.IO.Path.GetTempPath() + "Nekopomf/");
            string path = System.IO.Path.GetTempPath() + "Nekopomf/temp.png";
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                encoder.Save(fileStream);   // Save temp img
                fileStream.Close();
            }

            #region Upload
            // credit to https://github.com/mavanmanen/Scotty/blob/master/Scotty/Program.cs for this stuff

            if (((MainWindow) System.Windows.Application.Current.MainWindow).DisableUpload.IsChecked != true)
            {
                byte[] fileArray;
                using (var fileStream2 = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    fileArray = new byte[fileStream2.Length];
                    fileStream2.Read(fileArray, 0, System.Convert.ToInt32(fileStream2.Length));
                    fileStream2.Close();
                    fileStream2.Dispose();
                }
                byte[] xArray = Encoding.ASCII.GetBytes("------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"id\"\r\n\r\n\r\n------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"files[]\"; filename=\"temp.png\"\r\nContent-type: null\r\n\r\n");
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

                // copy upload to clipboard
                System.Windows.Clipboard.SetText("http://a.pomf.se/" + fileUrl, System.Windows.TextDataFormat.Text);    // Copy to clipboard
                ((MainWindow)System.Windows.Application.Current.MainWindow).URLBox.Text = "http://a.pomf.se/" + fileUrl + " has been copied to the clipboard.";

            }
            #endregion



            // play da sound
            if (((MainWindow)System.Windows.Application.Current.MainWindow).AudioCheck.IsChecked != true)
                (new System.Media.SoundPlayer("./Resources/eye.wav")).Play();   // Play audio
            
            // Log check
            if (((MainWindow)System.Windows.Application.Current.MainWindow).LogCheck.IsChecked == true)
            {
                if (((MainWindow) System.Windows.Application.Current.MainWindow).DisableUpload.IsChecked == true)
                {
                    fileUrl = "          ";
                }

                using (var fileStream = new FileStream("./log.txt", FileMode.Append))   // Save log of pastes
                {
                    string linky = "http://a.pomf.se/" + fileUrl + " " + System.DateTime.Now.ToString() + Environment.NewLine;
                    byte[] linkyArray = Encoding.ASCII.GetBytes(linky);
                    fileStream.Write(linkyArray, 0, linkyArray.Length);
                    fileStream.Close();
                }
            }

            // Store local copy
            if (((MainWindow)System.Windows.Application.Current.MainWindow).LocalCopyCheck.IsChecked == true)
            {
                if (!Directory.Exists("./saved/"))
                {
                    Directory.CreateDirectory("./saved");
                }

                string localCopyPath = "./saved/" + fileUrl;
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

        // Alias that uploads images stored in the system clipboard
        static public void UploadPNG()
        {
            if (System.Windows.Clipboard.ContainsImage())
            {
                UploadPNG(System.Windows.Clipboard.GetImage());
            }
        }
    }
}
