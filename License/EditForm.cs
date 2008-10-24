using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp;
using RiseOp.Implementation.Protocol.Packets;


namespace LicenseOp
{
    internal partial class EditForm : Form
    {
        Random rnd = new Random();

        internal FullLicense Customer;


        internal EditForm()
        {
            InitializeComponent();

            Customer = new FullLicense();
        }

        internal EditForm(FullLicense customer)
        {
            InitializeComponent();

            Customer = customer;

            LicenseBox.Text = Customer.LicenseID.ToString();
            NameBox.Text = Customer.Name;
            EmailBox.Text = Customer.Email;
            AddressBox.Text = Customer.Address;
            ReceiptBox.Text = Customer.Receipt;
            IndexBox.Text = Customer.Index.ToString();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                Customer.LicenseID = ulong.Parse(LicenseBox.Text);
                Customer.Name = NameBox.Text;
                Customer.Email = EmailBox.Text;
                Customer.Address = AddressBox.Text;
                Customer.Receipt = ReceiptBox.Text;
                Customer.Index = int.Parse(IndexBox.Text);
                Customer.Date = DateTime.Now;

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void GenLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LicenseBox.Text = Utilities.RandUInt64(rnd).ToString();
        }
    }
}
