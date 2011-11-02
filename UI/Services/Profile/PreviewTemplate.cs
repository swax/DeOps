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
    public partial class PreviewTemplate : DeOps.Interface.CustomIconForm
    {
        ProfileService Profiles;
        EditProfile EditForm;
        string Html;

        public PreviewTemplate(string html, EditProfile edit)
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