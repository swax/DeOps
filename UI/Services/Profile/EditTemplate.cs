using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Services.Profile
{
    public partial class EditTemplate : DeOps.Interface.CustomIconForm
    {
        EditProfile EditForm;
        ProfileTemplate Template;

        public EditTemplate(ProfileTemplate template, EditProfile form)
        {
            InitializeComponent();

            EditForm = form;
            Template = template;
        }

        private void EditTemplate_Load(object sender, EventArgs e)
        {
            HtmlBox.Text = Template.Html;
        }

        private void LinkPreview_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PreviewTemplate preview = new PreviewTemplate(HtmlBox.Text, EditForm);
            preview.ShowDialog(this);
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            Template.Html = HtmlBox.Text;

            if(!EditForm.TemplateCombo.Items.Contains(Template))
                EditForm.TemplateCombo.Items.Insert(0, Template);
            
            EditForm.TemplateCombo.SelectedItem = Template;
            EditForm.TemplateCombo_SelectedIndexChanged(null, null);

            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        
    }
}