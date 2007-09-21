using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Components.Link;

namespace DeOps.Components.Profile
{
    internal partial class EditProfile : Form
    {
        internal OpCore Core;
        LinkControl     Links;
        ProfileControl  Profiles;
        internal ProfileView MainView;

        List<ProfileTemplate> Templates = new List<ProfileTemplate>();

        internal Dictionary<string, string> TextFields = new Dictionary<string, string>();
        internal Dictionary<string, string> FileFields = new Dictionary<string, string>();


        internal EditProfile(ProfileControl control, ProfileView view)
        {
            InitializeComponent();

            Core     = control.Core;
            Links    = Core.Links;
            Profiles = Core.Profiles;
            MainView = view;

            TextFields = new Dictionary<string, string>(view.TextFields);
            FileFields = new Dictionary<string, string>(view.FileFields);
        }

        private void EditProfile_Load(object sender, EventArgs e)
        {
            RefreshTemplates();
        }

        private void RefreshTemplates()
        {
            Templates.Clear();

            // list chain of command first
            List<ulong> chainIDs = new List<ulong>();

            if (Links.LocalLink.Uplink.ContainsKey(0))
            {
                OpLink parent = Links.LocalLink.Uplink[0];

                while (parent != null)
                {
                    chainIDs.Add(parent.DhtID);
                    parent = parent.Uplink.ContainsKey(0) ? parent.Uplink[0] : null;
                }
            }
            chainIDs.Reverse();
            chainIDs.Add(Links.LocalLink.DhtID);

            // list higher level users, indent also
            // dont repeat names using same template+
            int space = 0;
            foreach (ulong id in chainIDs)
            {
                ProfileTemplate add = GetTemplate(id);

                if (add == null || add.Hash == null)
                    continue;

                foreach(ProfileTemplate template in TemplateCombo.Items)
                    if (Utilities.MemCompare(add.Hash, template.Hash))
                        continue;

                for (int i = 0; i < space; i++)
                    add.User = " " + add.User;

                TemplateCombo.Items.Add(add);

                space += 4;
            }

            // sort rest alphabetically
            List<ProfileTemplate> templates = new List<ProfileTemplate>();

            // read profile header file
            foreach (ulong id in Profiles.ProfileMap.Keys)
            {
                ProfileTemplate add = GetTemplate(id);

                if (add == null || add.Hash == null)
                    continue;

                bool dupe = false; 

                foreach (ProfileTemplate template in TemplateCombo.Items)
                    if (Utilities.MemCompare(add.Hash, template.Hash))
                        dupe = true;

                foreach (ProfileTemplate template in templates)
                    if (Utilities.MemCompare(add.Hash, template.Hash))
                        dupe = true;

                if(!dupe)
                    templates.Add(add);
            }

            // add space between chain items and other items
            if (TemplateCombo.Items.Count > 0 && templates.Count > 0)
                TemplateCombo.Items.Add(new ProfileTemplate(true, false));

            templates.Sort();
            foreach (ProfileTemplate template in templates)
                TemplateCombo.Items.Add(template);

            // select local template
            ProfileTemplate local = GetTemplate(Core.LocalDhtID);

            if(local != null)
                foreach (ProfileTemplate template in TemplateCombo.Items)
                    if (Utilities.MemCompare(local.Hash, template.Hash))
                    {
                        TemplateCombo.SelectedItem = template;
                        break;
                    }
        }

        private ProfileTemplate GetTemplate(ulong id)
        {
            if (!Profiles.ProfileMap.ContainsKey(id) || !Links.LinkMap.ContainsKey(id))
                return null;

            if ( !Links.LinkMap[id].Loaded)
                return null;

            OpProfile profile = Profiles.ProfileMap[id];

            ProfileTemplate template = new ProfileTemplate(false, true);

            template.User     = Links.LinkMap[id].Name;
            template.FilePath = Profiles.GetFilePath(profile.Header);
            template.FileKey  = profile.Header.FileKey;

            if (!profile.Loaded)
                Profiles.LoadProfile(profile.DhtID);
            
            try
            {
                FileStream stream = new FileStream(template.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(stream, template.FileKey.CreateDecryptor(), CryptoStreamMode.Read);

                int buffSize = 4096;
                byte[] buffer = new byte[4096];
                long bytesLeft = profile.Header.EmbeddedStart;
                while (bytesLeft > 0)
                {
                    int readSize = (bytesLeft > (long)buffSize) ? buffSize : (int)bytesLeft;
                    int read = crypto.Read(buffer, 0, readSize);
                    bytesLeft -= (long)read;
                }

                foreach (ProfileFile file in profile.Files)
                    if (file.Name.StartsWith("template"))
                    {
                        byte[] html = new byte[file.Size];
                        crypto.Read(html, 0, (int)file.Size);

                        template.Html = Core.Protocol.UTF.GetString(html);
                        SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
                        template.Hash = sha1.ComputeHash(html);

                        break;
                    }

                Utilities.ReadtoEnd(crypto);
                crypto.Close();
            }
            catch
            {
                return null;
            }

            return template;
        }

        int NewCount = 0;

        private void LinkNew_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProfileTemplate newTemplate = new ProfileTemplate(false, false);
            newTemplate.User = "New";
            newTemplate.Html = "";

            if (NewCount > 0)
                newTemplate.User += " " + NewCount.ToString();
            NewCount++;

            EditTemplate edit = new EditTemplate(newTemplate, this);
            edit.ShowDialog(this);

        }

        private void LinkEdit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (TemplateCombo.SelectedItem == null)
                return;

            ProfileTemplate template = (ProfileTemplate)TemplateCombo.SelectedItem;
            if (template.Inactive)
                return;

            // if ondisk, copy before editing
            if (template.OnDisk)
            {
                ProfileTemplate copy = new ProfileTemplate(false, false);
                copy.User = template.User + " (edited)";
                copy.User = copy.User.TrimStart(new char[] { ' ' });
                copy.Html = template.Html;

                template = copy;
            }

            EditTemplate edit = new EditTemplate(template, this);
            edit.ShowDialog(this);
        }
        
        private void LinkPreview_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (TemplateCombo.SelectedItem == null)
                return;

            ProfileTemplate template = (ProfileTemplate) TemplateCombo.SelectedItem;
            if (template.Inactive)
                return;

            PreviewTemplate preview = new PreviewTemplate(template.Html, this);
            preview.ShowDialog(this);
        }
        
        internal void TemplateCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            FieldsCombo.Items.Clear();
            ValueTextBox.Text = "";

            if (TemplateCombo.SelectedItem == null)
                return;

            ProfileTemplate template = (ProfileTemplate)TemplateCombo.SelectedItem;
            if (template.Inactive)
                return;

            List<TemplateTag> tags = new List<TemplateTag>();

            // extract tag names from html
            string html = template.Html;

            // replace fields
            while (html.Contains("<?"))
            {
                int start = html.IndexOf("<?");
                int end = html.IndexOf("?>");

                if (end == -1)
                    break;

                string fulltag = html.Substring(start, end + 2 - start);
                string tag = fulltag.Substring(2, fulltag.Length - 4);

                string[] parts = tag.Split(new char[] { ':' });

                if (parts.Length == 2)
                {
                    if (parts[0] == "text")
                        tags.Add(new TemplateTag(parts[1], ProfileFieldType.Text));
                    else if (parts[0] == "file")
                        tags.Add(new TemplateTag(parts[1], ProfileFieldType.File));
                }

                html = html.Replace(fulltag, "");
            }

            tags.Sort();

            bool motdAdded = false; // add just 1 to change
            foreach (TemplateTag tag in tags)
            {
                // if motd for this project, allow
                if (tag.Name.StartsWith("MOTD"))
                    if (!motdAdded)
                    {
                        tag.Name = "MOTD";
                        motdAdded = true;
                    }
                    else
                        continue;

                FieldsCombo.Items.Add(tag);
            }

            if (FieldsCombo.Items.Count > 0)
            {
                FieldsCombo.SelectedItem = FieldsCombo.Items[0];
                FieldsCombo_SelectedIndexChanged(null, null);
            }
        }

        private void FieldsCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FieldsCombo.SelectedItem == null)
                return;

            TemplateTag tag = (TemplateTag)FieldsCombo.SelectedItem;

            if (tag.FieldType == ProfileFieldType.Text)
            {
                LinkBrowse.Enabled = false;
                ValueTextBox.ReadOnly = false;
            }
            else
            {
                LinkBrowse.Enabled = true;
                ValueTextBox.ReadOnly = true;
            }

            // set value text
            if (tag.FieldType == ProfileFieldType.Text)
            {
                string fieldName = tag.Name;

                if (fieldName == "MOTD")
                    fieldName = "MOTD-" + MainView.ProjectID.ToString();

                if (TextFields.ContainsKey(fieldName))
                    ValueTextBox.Text = TextFields[fieldName];
                else
                    ValueTextBox.Text = "";
            }

            else if (tag.FieldType == ProfileFieldType.File && FileFields.ContainsKey(tag.Name))
                ValueTextBox.Text = FileFields[tag.Name];
            else
                ValueTextBox.Text = "";

            ValueTextBox_TextChanged(null, null);
        }

        private void ValueTextBox_TextChanged(object sender, EventArgs e)
        {
            if (FieldsCombo.SelectedItem == null)
                return;

            TemplateTag tag = (TemplateTag) FieldsCombo.SelectedItem;

            if (tag.FieldType == ProfileFieldType.Text)
            {
                string fieldName = tag.Name;

                if (fieldName == "MOTD")
                    fieldName = "MOTD-" + MainView.ProjectID.ToString();

                TextFields[fieldName] = ValueTextBox.Text;
            }

            if (tag.FieldType == ProfileFieldType.File)
                FileFields[tag.Name] = ValueTextBox.Text;
        }

        private void LinkBrowse_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (FieldsCombo.SelectedItem == null)
                return;

            TemplateTag tag = (TemplateTag)FieldsCombo.SelectedItem;

            OpenFileDialog open = new OpenFileDialog();
            open.Multiselect = true;
            open.Title = "Browse for File";
            open.Filter = "All files (*.*)|*.*";

            if (open.ShowDialog() == DialogResult.OK)
            {
                ValueTextBox.Text = open.FileName;
                FileFields[tag.Name] = open.FileName;
            }
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (TemplateCombo.SelectedItem == null)
            {
                Close();
                return;
            }

            ProfileTemplate template = (ProfileTemplate)TemplateCombo.SelectedItem;
            if (template.Inactive)
            {
                Close();
                return;
            }

            // remove text fields that are not in template
            List<string> removeKeys = new List<string>();

            foreach (string key in TextFields.Keys)
                if (!key.StartsWith("MOTD") && !template.Html.Contains("<?text:" + key))
                    removeKeys.Add(key);

            foreach (string key in removeKeys)
                TextFields.Remove(key);

            // remove files that are not in template
            removeKeys.Clear();

            foreach (string key in FileFields.Keys)
                if (!template.Html.Contains("<?file:" + key))
                    removeKeys.Add(key);

            foreach (string key in removeKeys)
                FileFields.Remove(key);

            // save profile will also update underlying interface
            Profiles.SaveLocal(template.Html, TextFields, FileFields);

            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    internal class ProfileTemplate : IComparable
    {
        internal bool Inactive;
        internal bool OnDisk;
        internal string User = "";
        
        internal string FilePath;
        internal RijndaelManaged FileKey;
        
        internal string Html = "";
        internal byte[] Hash;
       

        internal ProfileTemplate(bool inactive, bool ondisk)
        {
            Inactive = inactive;
            OnDisk = ondisk;
        }

        public override string ToString()
        {
            return User;
        }

        public int CompareTo(object obj)
        {
            return User.CompareTo(((ProfileTemplate)obj).User);
        }
    }

    internal class TemplateTag : IComparable
    {
        internal string Name ;
        internal ProfileFieldType FieldType;

        internal TemplateTag(string name, ProfileFieldType type)
        {
            Name = name;
            FieldType = type;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(object obj)
        {
            return Name.CompareTo(((TemplateTag)obj).Name);
        }
    }
}