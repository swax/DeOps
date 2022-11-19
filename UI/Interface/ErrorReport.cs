﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace DeOps.Interface
{
    public partial class ErrorReport : CustomIconForm
    {
        public Exception Details;


        public ErrorReport(Exception details)
        {
            InitializeComponent();

            Details = details;

            DetailsBox.Text = Details.Message + ": \r\n" + Details.StackTrace;
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            SendButton.Text = "Sending...";

            try
            {
                Dictionary<string, string> post = new Dictionary<string, string>();


                post["message"] = Details.Message;
                post["stacktrace"] = Details.StackTrace;
                post["notes"] = NotesBox.Text;
                post["deops"] = Application.ProductVersion;
                post["windows"] = Environment.OSVersion.Version.ToString();
                post["net"] = Environment.Version.ToString();
                post["culture"] = Thread.CurrentThread.CurrentCulture.EnglishName;
                post["email"] = EmailBox.Text;
                // date will be inserted by php script


                // Create a request using a URL that can receive a post. 
                WebRequest request = WebRequest.Create("http://www.c0re.net/deops/error/handler.php");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                StringBuilder report = new StringBuilder(4096);

                foreach (var pair in post)
                    report.Append(pair.Key + "=" + WebUtility.UrlEncode(pair.Value) + "&");

                byte[] data = UTF8Encoding.UTF8.GetBytes(report.ToString());
                request.ContentLength = data.Length;

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(data, 0, data.Length);
                dataStream.Close();

                // Get the response.
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseFromServer = reader.ReadToEnd();


                // Clean up the streams.
                reader.Close();
                dataStream.Close();
                response.Close();

                MessageBox.Show(responseFromServer, "Error Report");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to report error\r\n" + ex.Message, "Error Report");
            }

            SendButton.Enabled = false;
            SendButton.Text = "Sent";
            Close();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
