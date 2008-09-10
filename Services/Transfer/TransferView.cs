using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Interface;
using RiseOp.Interface.TLVex;
using RiseOp.Implementation;
using RiseOp.Implementation.Transport;

namespace RiseOp.Services.Transfer
{
    internal partial class TransferView : CustomIconForm
    {

        internal OpCore Core;
        internal TransferService Service;

        Dictionary<ulong, bool> TransferMap = new Dictionary<ulong, bool>();


        internal TransferView(TransferService service)
        {
            InitializeComponent();

            Core = service.Core;
            Service = service;
        }

        private void TransferView_Load(object sender, EventArgs e)
        {
            RefreshView();

            Text = Core.Trust.GetName(Core.UserID) + "'s Transfers";
        }

        private void FastTimer_Tick(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            // remove
            var remove = (from TransferNode node in TransferList.Nodes
                          where !Service.Transfers.ContainsKey(node.FileID)
                          select node).ToList();

            foreach(TransferNode node in remove)
            {
                TransferMap.Remove(node.FileID);
                TransferList.Nodes.Remove(node);
            }

            // add missing
            var add = from transfer in Service.Transfers.Values 
                      where !TransferMap.ContainsKey(transfer.FileID)
                      select transfer;

            foreach (OpTransfer transfer in add)
            {
                TransferMap[transfer.FileID] = true;
                TransferList.Nodes.Add(new TransferNode(Service, transfer.FileID));
            }

            foreach (TransferNode transfer in TransferList.Nodes)
                transfer.Refresh();

            TransferList.ExpandAll();

            TransferList.Invalidate();
        }

        private void TransferList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            TreeListNode node = TransferList.GetNodeAt(e.Location) as TreeListNode;

            if (item == null)
                return;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (node.GetType() == typeof(TransferNode))
            {
                
            }

            else if (node.GetType() == typeof(PeerNode))
            {

            }

            menu.Show(TransferList, e.Location);
        }
    }

    internal class TransferNode : TreeListNode
    {
        TransferService Service;
        internal ulong FileID;

        BitfieldControl Bitfield = new BitfieldControl();
        Dictionary<ulong, bool> PeerMap = new Dictionary<ulong, bool>();


        internal TransferNode(TransferService service, ulong id)
        {
            Service = service;
            FileID = id;

            SubItems.Add(Bitfield);
        }

        internal void Refresh()
        {
            if(!Service.Transfers.ContainsKey(FileID))
            {
                Text = "Error";
                return;
            }

            OpTransfer transfer = Service.Transfers[FileID];

            Bitfield.UpdateField(transfer.LocalBitfield);

            string text = "";
            // set transfer text / columns
            // time started, service, fileID, completed x of X
			// flags: searching, file loaded, sub-hashes

            text += Service.Core.Context.KnownServices[transfer.Details.Service] + "-" + transfer.FileID.ToString().Substring(0, 4) + ", ";
            text += "Started: " + transfer.Created.ToShortTimeString() + ", ";
            

            if (transfer.LocalBitfield != null)
                text += transfer.LocalBitfield.Length + " Pieces, ";

            if (transfer.Status == TransferStatus.Complete)
                text += "Completed, ";
            else
            {
                text += "Progress: " + transfer.GetProgress() + " of " + Utilities.CommaIze(transfer.Details.Size) + ", ";
            }

            if (transfer.Searching)
                text += "Searching, ";

            if (transfer.LocalFile == null)
                text += "Unloaded, ";

            Text = text.Substring(0, text.Length - 2);


            // update sub items
            var remove = (from PeerNode peer in Nodes
                          where !transfer.Peers.ContainsKey(peer.RoutingID)
                          select peer).ToList();

            foreach (PeerNode peer in remove)
            {
                PeerMap.Remove(peer.RoutingID);
                Nodes.Remove(peer);
            }

            // add missing
            var add = from peer in transfer.Peers.Values
                      where !PeerMap.ContainsKey(peer.RoutingID)
                      select peer;

            foreach (RemotePeer peer in add)
            {
                PeerMap[peer.RoutingID] = true;
                Nodes.Add(new PeerNode(Service, peer.RoutingID));
            }

            foreach (PeerNode peer in Nodes)
                peer.Refresh(transfer);
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

            text += "      " + Service.Core.Trust.GetName(peer.Client.UserID) + ", ";
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