using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;

namespace RiseOp.Interface.Settings
{
    internal partial class CacheSetup : CustomIconForm
    {
        OpCore Core;
        Identity Profile;

        internal WebCache Cache;


        internal CacheSetup(OpCore core, WebCache cache)
            : base(core)
        {
            InitializeComponent();

            Core = core;
            Profile = Core.Profile;

            Cache = cache;

            AddressBox.Text = cache.Address;
            KeyBox.Text = (cache.AccessKey != null) ? Convert.ToBase64String(cache.AccessKey) : "";
            OpBox.Text = core.Network.OpID.ToString();
        }

        private void TestLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                WebCache test = new WebCache();
                test.Address = AddressBox.Text;
                test.AccessKey = Convert.FromBase64String(KeyBox.Text);

                string response = Core.Network.Cache.MakeWebCacheRequest(test, "ping:" + Core.Network.OpID.ToString());

                if (response.StartsWith("pong"))
                    MessageBox.Show("Success");
                else
                    MessageBox.Show(response);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void NewLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RijndaelManaged crypt = new RijndaelManaged();
            crypt.GenerateKey();
            KeyBox.Text = Convert.ToBase64String(crypt.Key);
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            try
            {
                Cache.Address = AddressBox.Text;
                Cache.AccessKey = Convert.FromBase64String(KeyBox.Text);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
