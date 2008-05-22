using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


namespace RiseOp.Services.Profile
{
    internal partial class PreviewTemplate : Form
    {
        ProfileService Profiles;
        EditProfile EditForm;
        string Html;

        internal PreviewTemplate(string html, EditProfile edit)
        {
            InitializeComponent();

            Profiles = edit.Profiles;
            EditForm = edit;
            Html = html;
        }

        private void PreviewTemplate_Load(object sender, EventArgs e)
        {
            Browser.DocumentText = ProfileView.FleshTemplate(Profiles, Profiles.Core.UserID, 0, Html, EditForm.TextFields, EditForm.FileFields);
        }
    }
}