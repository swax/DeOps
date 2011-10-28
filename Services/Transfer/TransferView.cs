using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Transport;


namespace DeOps.Services.Transfer
{
    internal partial class TransferView : CustomIconForm
    {

        internal OpCore Core;
        internal TransferService Service;

        Dictionary<ulong, TransferNode> TransferMap = new Dictionary<ulong, TransferNode>();

        bool DefaultCollapse = true;


        internal static void Show(DhtNetwork network)
        {
            var form = new TransferView(network.Core.Transfers);

            form.Show();
            form.Activate();
        }

        internal TransferView(TransferService service)
        {
            InitializeComponent();

            Core = service.Core;
            Service = service;

#if DEBUG
            Service.Logging = true;
#endif
        }

        private void TransferView_Load(object sender, EventArgs e)
        {
            RefreshView();

            Text = Core.GetName(Core.UserID) + "'s Transfers";
        }

        private void FastTimer_Tick(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            Dictionary<ulong, OpTransfer> displayMap = new Dictionary<ulong, OpTransfer>();

            if (ShowDownloads.Checked)
                foreach (OpTransfer download in Service.Transfers.Values.Where(t => t.Status != TransferStatus.Complete))
                    displayMap[download.FileID] = download;

            if (ShowUploads.Checked)
                foreach (OpTransfer upload in Service.Transfers.Values.Where(t => t.Status == TransferStatus.Complete))
                    displayMap[upload.FileID] = upload;

            if (ShowPending.Checked)
                foreach (OpTransfer pending in Service.Pending)
                    displayMap[pending.FileID] = pending;


            if (ShowPartials.Checked)
                foreach (OpTransfer partial in Service.Partials)
                    displayMap[partial.FileID] = partial;

            // remove
            var remove = (from TransferNode node in TransferList.Nodes
                          where !displayMap.ContainsKey(node.Transfer.FileID)
                          select node).ToList();

            foreach (TransferNode node in remove)
            {
                TransferMap.Remove(node.Transfer.FileID);
                TransferList.Nodes.Remove(node);
            }

            // add missing
            var add = from transfer in displayMap.Values
                      where !TransferMap.ContainsKey(transfer.FileID)
                      select transfer;

            foreach (OpTransfer transfer in add)
            {
                TransferNode node = new TransferNode(Service, transfer);
                TransferMap[transfer.FileID] = node;
                TransferList.Nodes.Add(node);

                if(!DefaultCollapse)
                    node.Expand();
            }

            foreach (TransferNode transfer in TransferList.Nodes)
                transfer.Refresh();

            TransferList.Invalidate();
        }

        private void TransferList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            TreeListNode node = TransferList.GetNodeAt(e.Location) as TreeListNode;

            if (node == null || node.GetType() != typeof(TransferNode))
                return;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            TransferNode transfer = node as TransferNode;

            menu.Items.Add(new ToolStripMenuItem("Copy Hash to Clipboaard", null, (s, o) =>
            {
                Clipboard.SetText(Utilities.ToBase64String(transfer.Transfer.Details.Hash));
            }));

            menu.Show(TransferList, e.Location);
        }

        private void DownloadsCheck_CheckedChanged(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void UploadsCheck_CheckedChanged(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void PendingCheck_CheckedChanged(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void PartialsCheck_CheckedChanged(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void TransferView_FormClosing(object sender, FormClosingEventArgs e)
        {
            Service.Logging = false;
        }

        private void ExpandLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DefaultCollapse = false;

            foreach (TreeListNode node in TransferList.Nodes)
                node.Expand();
        }

        private void CollapseLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DefaultCollapse = true;

            foreach (TreeListNode node in TransferList.Nodes)
                node.Collapse();
        }
    }

    internal class TransferNode : TreeListNode
    {
        TransferService Service;
        internal OpTransfer Transfer;

        BitfieldControl Bitfield = new BitfieldControl();
        Dictionary<ulong, bool> PeerMap = new Dictionary<ulong, bool>();


        internal TransferNode(TransferService service, OpTransfer t)
        {
            Service = service;
            Transfer = t;

            SubItems.Add(Bitfield);
        }

        internal void Refresh()
        {
            Bitfield.UpdateField(Transfer.LocalBitfield);

            string text = "";
            // set transfer text / columns
            // time started, service, fileID, completed x of X
			// flags: searching, file loaded, sub-hashes

            text += Service.Core.Context.KnownServices[Transfer.Details.Service] + "-" + Transfer.FileID.ToString().Substring(0, 4) + ", ";
            text += "Started: " + Transfer.Created.ToShortTimeString() + ", ";
            

            if (Transfer.LocalBitfield != null)
                text += Transfer.LocalBitfield.Length + " Pieces, ";

            if (Transfer.Status == TransferStatus.Complete)
                text += "Completed, ";
            else
            {
                text += "Progress: " + Utilities.CommaIze(Transfer.GetProgress()) + " of " + Utilities.CommaIze(Transfer.Details.Size) + ", ";
            }

            if (Transfer.Searching)
                text += "Searching, ";

            if (Transfer.LocalFile == null)
                text += "Unloaded, ";

            Text = text.Substring(0, text.Length - 2);


            // update sub items
            var remove = (from PeerNode peer in Nodes
                          where !Transfer.Peers.ContainsKey(peer.RoutingID)
                          select peer).ToList();

            foreach (PeerNode peer in remove)
            {
                PeerMap.Remove(peer.RoutingID);
                Nodes.Remove(peer);
            }

            // add missing
            var add = from peer in Transfer.Peers.Values
                      where !PeerMap.ContainsKey(peer.RoutingID)
                      select peer;

            foreach (RemotePeer peer in add)
            {
                PeerMap[peer.RoutingID] = true;
                Nodes.Add(new PeerNode(Service, peer.RoutingID));
            }

            foreach (PeerNode peer in Nodes)
                peer.Refresh(Transfer);
        }
    }

    internal class PeerNode : TreeListNode
    {
        TransferService Service;
        internal ulong RoutingID;
        BitfieldControl Bitfield = new BitfieldControl();


        internal PeerNode(TransferService service, ulong id)
        {
            Service = service;
            RoutingID = id;

            SubItems.Add(Bitfield);
        }

        internal void Refresh(OpTransfer transfer)
        {
            if (!transfer.Peers.ContainsKey(RoutingID))
            {
                Text = "Error";
                return;
            }

            RemotePeer peer = transfer.Peers[RoutingID];

            int upPiece = -1, downPiece = -1;
  
            string text = "";
            // remote name / IP - last seen, timeout: x
			// flags: UL (active?, chunk index, progress) / DL (chunk index, progress) / RBU

            if (peer.DhtIndex < transfer.RoutingTable.Length && transfer.RoutingTable[peer.DhtIndex] == peer)
                text += "(B" + peer.DhtIndex + ") ";

            text += Service.Core.GetName(peer.Client.UserID) + ", ";
            text += "Last Seen: " + peer.LastSeen.ToShortTimeString() + ", ";
            //text += "Timeout: " + peer.PingTimeout + ", ";

            if (peer.RemoteBitfieldUpdated)
                text += "Out of Date, ";

            if (Service.UploadPeers.ContainsKey(peer.RoutingID))
            {
                UploadPeer upload = Service.UploadPeers[peer.RoutingID];
                text += "Last Upload: " + upload.LastAttempt.ToShortTimeString() + ", ";

                if (upload.Active == peer)
                {
                    text += "Upload: ";

                    if (upload.Active.LastRequest != null && upload.Active.CurrentPos != 0)
                    {
                        TransferRequest req = upload.Active.LastRequest;
                        upPiece = req.ChunkIndex;
                        int percent = (int) ((req.EndByte - upload.Active.CurrentPos) * 100 / (req.EndByte - req.StartByte));

                        // Piece 4 - 34%,
                        text += "Piece " + upPiece + " - " + percent + "%, ";
                    }
                    else
                        text += "Pending, ";
                }

                if(peer.LastError != null)
                    text += "Stopped: " + peer.LastError + ", ";
            }

            if(Service.DownloadPeers.ContainsKey(peer.RoutingID))
                if (Service.DownloadPeers[peer.RoutingID].Requests.ContainsKey(transfer.FileID))
                {
                    TransferRequest req = Service.DownloadPeers[peer.RoutingID].Requests[transfer.FileID];

                    text += "Download: ";

                    if (req.CurrentPos != 0)
                    {
                        downPiece = req.ChunkIndex;
                        int percent = (int)((req.EndByte - req.CurrentPos) * 100 / (req.EndByte - req.StartByte));
         
                        // Piece 4 - 34%,
                        text += "Piece " + downPiece + " - " + percent + "%, ";
                    }
                    else
                        text += "Pending, ";
                }

            Text = text.Substring(0, text.Length - 2);
 
            Bitfield.UpdateField(peer.RemoteBitfield, upPiece, downPiece);
        }
    }
}