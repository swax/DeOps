using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using RiseOp;
using RiseOp.Implementation;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Packets;


namespace LicenseOp
{
    public partial class MainForm : Form
    {
        byte[] FileKey;

        byte[] LicenseKey;

        RSACryptoServiceProvider JMG_Key = new RSACryptoServiceProvider();

        bool ChangesMade;


        public MainForm()
        {
            InitializeComponent();

            /*byte[] pass = Utilities.GetPasswordKey("lenstubes", new byte[] { 0x7A, 0x0D });

            RijndaelManaged crypt = new RijndaelManaged();

            string fileKey = Convert.ToBase64String(Utilities.EncryptBytes(crypt.Key, pass));*/

            LicenseKey = Convert.FromBase64String("4mdBmbUIjh2p6sff42O9AfYdWKZfVUgeK6vOv514XVw=");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                GetTextDialog getPass = new GetTextDialog("Enter Password", "Enter pass to access and sign customers", "");
                getPass.ResultBox.UseSystemPasswordChar = true;
                getPass.ShowDialog();

                if (getPass.DialogResult != DialogResult.OK)
                    throw new Exception();

                byte[] pass = Utilities.GetPasswordKey(getPass.ResultBox.Text, new byte[] { 0x7A, 0x0D });

                
                // get customer file key
                byte[] safeFileKey = Convert.FromBase64String("s79TN9sU5ljazv8Gm0g+g58s1hXn43HIY3scvKkvbAiCiEKovHAtW8BnR1L50wt4/GgqjEs9eid0apMz6JtQaA==");
                FileKey = Utilities.DecryptBytes(safeFileKey, safeFileKey.Length, pass);


                // get signing rsa key
                byte[] safeJMGkey = Convert.FromBase64String("J8H+0u4gF5607zW3DhbSExzFHjDM2mftz19pKR4bRLnkqS3MsVy4gEfhd0TCSkaqfSb9ZRavu+MS44wNvVY6/BkGo2RqzzzkuyDJZmVZXrHmUvPVmf84NUhq2vPHo/JHdMWcMICdfmz09gZm+CbCKwKwNdiDHZbgLlJu4Kqe+OFMNV/AkWbQchVXUmyqFJ6e+w5WdukNGVoW+6mQ2k7MOeJxQrtQ822QNqsShYKI5/p51+dnowBqrioCyHq6XBAlQFY/Muit/uv1iUWNbDdmmDH3hdo44qS/jIpbboU1s0m0FWKnPT3mzxfRK0pVqghmr2v3WtBIAVKOlEQ4vWd0cUEFgOtH1aCLgKZy4b4t//LEYuThpRh3b0J/So7MVy662DxM0xA9pd2lDjeuqnWOn4t7P2uHN/x0BSZaat4YOSXIMMsXxr+XO4XKItn8uq7fVdqYn6Xc53Km0tMYffrFng8SwypUeOKedqHiFaNHfZaMHOScw0zYd6SG0nWTEEyG1JYBJt5GkPYPzLnI9ydf/J+V3eOB47tt6x91N93xV1jcPiG/Agr2d3hkB8RSV2z4ab2qjq9byADyRIf2r/eLAn0QNC3gJejlSet9zhtDHdieass5qpzQ0M0G/gqY90aCP7L73tcmkbweW5Xunj+i3m2YgL5o/ouPJ4mfywus+U8SUj504gUMj0qtpFgLLpBdl9NKqMHZ9/HLF/dVvHtn4r7nnJ8kPzwol6+Lv0bl3x7gAB+mO4/zXrop6BKOW/OTL1jyr8hWJxU5TsvxHZxgDSpHFK8SIyM4UmWkYO23m6XU//4yVb4/NKVMdFCpHYuxcOqaMANa5xddy4XJungl9dhfVFWL2OiU5OaKKLlARioB9K+PclcngLzhLC764EgDw9Pe4Xov+GfP28XEgVy9EsUbbWXgyaEPlAlMVgY2YbTI4PcS6VTH+8r+rivOI58RKd4wWUtIxQucjNgO0J4nHr0CNIjjX47v+yEP6lDSwJw0LwyqKzGrK9UT+0lPGOm52vXQFPYRknYpQTUDT8ydrAfQEBf7DN2I94Pbk2r7Plo9Th3/6OLHFSdlvyVP9MXfztrx3b2fEkPHVKf40Iw9qgrsoOMWu83+2JOuiZLPUgMDaOFudgno8Stdsl9f2xOFZvfExx/iKIwhxgZJ5dBH1wqLUG7B/ZcqK/6Al9suJna2pWctxqjm1K0krAwwTJZLb4/J53KKPq5nqT4TVwnpKYjiT4URWlImlu108RGj5Ps=");

                string xml = UTF8Encoding.UTF8.GetString(Utilities.DecryptBytes(safeJMGkey, safeJMGkey.Length, pass));
                JMG_Key.FromXmlString(xml);
            }
            catch
            {
                Application.Exit();
                return;
            }

            LoadCustomers();
        }

        private void RefreshLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            LicenseList.Items.Clear();

            if (!File.Exists("customer.dat"))
                return;

            using (IVCryptoStream crypto = IVCryptoStream.Load("customer.dat", FileKey))
            {
                PacketStream stream = new PacketStream(crypto, new G2Protocol(), FileAccess.Read);

                G2Header root = null;
                while (stream.ReadPacket(ref root))
                    if (root.Name == LicensePacket.Full)
                        LicenseList.Items.Add(new LicenseItem(FullLicense.Decode(root)));
            }
        }

        private void CreateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            EditForm edit = new EditForm();

            if (edit.ShowDialog() == DialogResult.OK)
            {
                LicenseList.Items.Add(new LicenseItem(edit.Customer));
                edit.Customer.Sign(new G2Protocol(), JMG_Key);
                ChangesMade = true;
            }
        }

        private void LicenseList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (LicenseList.SelectedItems.Count == 0)
                return;

            LicenseItem item = LicenseList.SelectedItems[0] as LicenseItem;

            EditForm edit = new EditForm(item.Customer);

            if (edit.ShowDialog() == DialogResult.OK)
            {
                edit.Customer.Sign(new G2Protocol(), JMG_Key);
                ChangesMade = true;
            }
        }

        private void SaveCustomers()
        {
            G2Protocol protocol = new G2Protocol();

            using (IVCryptoStream crypto = IVCryptoStream.Save("customer.dat", FileKey))
            {
                PacketStream stream = new PacketStream(crypto, protocol, FileAccess.Write);

                foreach (LicenseItem item in LicenseList.Items)
                {
                    item.Customer.SaveExtra = true;
                    stream.WritePacket(item.Customer);
                    item.Customer.SaveExtra = false;
                }
            }

            ChangesMade = false;

            MessageBox.Show(LicenseList.Items.Count + " Licenses Saved");
        }

        private void SignButton_Click_1(object sender, EventArgs e)
        {
            G2Protocol protocol = new G2Protocol();

            foreach (LicenseItem item in LicenseList.SelectedItems)
                ExportLicense(item.Customer, protocol);

            MessageBox.Show(LicenseList.SelectedItems.Count + " Licenses Exported");
        }

        private void ExportLicense(FullLicense customer, G2Protocol protocol)
        {
            string tag = Utilities.BytestoHex(BitConverter.GetBytes(customer.LicenseID)).Substring(0, 6);

            string filename = "license-" + tag + ".dat";

            // write license packet
            using (IVCryptoStream crypto = IVCryptoStream.Save(filename, LicenseKey))
            {
                PacketStream stream = new PacketStream(crypto, protocol, FileAccess.Write);

                stream.WritePacket(customer);
                
                LightLicense light = new LightLicense(customer);
                light.Sign(protocol, JMG_Key);
                stream.WritePacket(light);
            }

            Test(filename);
        }

        private void Test(string filename)
        {
            FullLicense full;
            LightLicense light;

            using (IVCryptoStream crypto = IVCryptoStream.Load(filename, LicenseKey))
            {
                PacketStream stream = new PacketStream(crypto, new G2Protocol(), FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == LicensePacket.Full)
                        full = FullLicense.Decode(root);
                    else if (root.Name == LicensePacket.Light)
                        light = LightLicense.Decode(root);
            }
        }

        private void DeleteLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            List<LicenseItem> remove = new List<LicenseItem>();

            foreach (LicenseItem item in LicenseList.SelectedItems)
                remove.Add(item);

            foreach (LicenseItem item in remove)
                LicenseList.Items.Remove(item);

            if (remove.Count > 0)
                ChangesMade = true;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveCustomers();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ChangesMade)
            {
                DialogResult result = MessageBox.Show("Safe Changes?", "Alert", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Cancel)
                    e.Cancel = true;

                if (result == DialogResult.Yes)
                    SaveCustomers();
            }
        }

    }

    internal class LicenseItem : ListViewItem
    {
        internal FullLicense Customer;

        internal LicenseItem(FullLicense customer)
        {
            Customer = customer;

            Text = Customer.Name;

            SubItems.Add(customer.Email);
            SubItems.Add(customer.Index.ToString());
        }
    }
}