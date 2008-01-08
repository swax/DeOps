using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Services.Profile
{
    internal partial class PreviewTemplate : Form
    {
        OpCore Core;
        EditProfile EditForm;
        string Html;

        internal PreviewTemplate(string html, EditProfile edit)
        {
            InitializeComponent();

            Core = edit.Core;
            EditForm = edit;
            Html = html;
        }

        private void PreviewTemplate_Load(object sender, EventArgs e)
        {
            Browser.DocumentText = ProfileView.FleshTemplate(Core, Core.LocalDhtID, 0, Html, EditForm.TextFields, EditForm.FileFields);
        }
    }
}