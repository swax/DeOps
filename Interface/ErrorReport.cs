using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;


namespace RiseOp.Interface
{
    internal partial class ErrorReport : Form
    {
        bool Sent;
        SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
        
        internal Exception Details;


        internal ErrorReport(Exception details)
        {
            InitializeComponent();

            Details = details;

            DetailsBox.Text = Details.Message + ": \r\n" + Details.StackTrace;

            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential("riseop.errors", "r1530p3rr0r5");
            smtp.Timeout = 10;
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if (Sent)
            {
                Close();
                return;
            }

            MailMessage mail = new MailMessage();

            // set the addresses
            mail.From = new MailAddress("riseop.errors@gmail.com");
            mail.To.Add("riseop.errors@gmail.com");

            // set the content
            mail.Subject = Details.Message;
            mail.Body = DateTime.Now.ToString() + "\r\n\r\n";
            mail.Body += Details.Message + "\r\n\r\n";
            mail.Body += "Stack Trace\r\n" + Details.StackTrace + "\r\n\r\n";
            mail.Body += "Additional Notes\r\n" + NotesBox.Text + "\r\n\r\n";
            mail.Body += "Version: " + Application.ProductVersion.ToString() + "\r\n\r\n";

            Object userState = mail;

            smtp.SendCompleted += new SendCompletedEventHandler(SmtpClient_OnCompleted);

            smtp.SendAsync(mail, userState);

            SendButton.Enabled = false;
            SendButton.Text = "Sending...";
        }

        internal void SmtpClient_OnCompleted(object sender, AsyncCompletedEventArgs e )
        {
            SendButton.Enabled = true;
            SendButton.Text = "Done";
            Sent = true;

            //Get the Original MailMessage object
            MailMessage mail = e.UserState as MailMessage;

            if (mail == null)
                return;

            //write out the subject
            string subject = mail.Subject;

            if( e.Cancelled )
            {
                Console.WriteLine("Send canceled for mail with subject [{0}].", subject);
            }
            if(e.Error != null) 
            {
                MessageBox.Show(e.Error.ToString());
                Console.WriteLine("Error {1} occurred when sending mail [{0}] ", subject, e.Error.ToString());
            }
            else
            {
                Console.WriteLine("Message [{0}] sent.", subject);
            }
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
