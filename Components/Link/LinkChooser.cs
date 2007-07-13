using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface.TLVex;
using DeOps.Components.Link;


namespace DeOps.Components.Link
{
    internal partial class LinkChooser : Form
    {
        LinkControl Links;
        uint ProjectID;

        internal List<ulong> People = new List<ulong>();


        internal LinkChooser(LinkControl links, uint project)
        {
            InitializeComponent();

            Links = links;
            ProjectID = project;
        }

        private void LinkChooser_Load(object sender, EventArgs e)
        {
            LinkNode parent = new LinkNode(Links.LocalLink);

            AddChildren(parent, Links.LocalLink);

            // dont add root

            foreach(LinkNode child in parent.Nodes)
                LinkTree.Nodes.Add(child);

            parent.Expand();
        }

        private void AddChildren(LinkNode parent, OpLink link)
        {
            if(link.Confirmed.ContainsKey(ProjectID) && link.Downlinks.ContainsKey(ProjectID))
                foreach(OpLink downlink in link.Downlinks[ProjectID])
                    if (link.Confirmed[ProjectID].Contains(downlink.DhtID))
                    {
                        LinkNode child = new LinkNode(downlink);
                        AddChildren(child, downlink);
                        parent.Nodes.Add(child);
                    }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            foreach (LinkNode node in LinkTree.SelectedNodes)
                People.Add(node.DhtID);

            DialogResult = DialogResult.OK;

            Close();
        }

        private void TheCancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

    }

    internal class LinkNode : TreeListNode
    {
        internal ulong DhtID;

        internal LinkNode(OpLink link)
        {
            Text = link.Name;
            DhtID = link.DhtID;
        }
    }
}