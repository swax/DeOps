using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface.TLVex;
using DeOps.Implementation.Transport;

namespace DeOps.Services.Transfer
{
    internal partial class TransferView : Form
    {
        TransferService Transfers;


        internal TransferView(TransferService transfers)
        {
            InitializeComponent();

            Transfers = transfers;
        }

        private void TransferView_Load(object sender, EventArgs e)
        {
            RefreshView();


            Text = Transfers.Core.Links.GetName(Transfers.Core.LocalDhtID) + "'s Transfers";
        }

        private void FastTimer_Tick(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            // Downloads
            List<TreeListNode> missing = DownloadList.Nodes.GetList();

            foreach (FileDownload download in Transfers.DownloadMap.Values)
            {
                DownloadNode node = FindDownload(download);

                if (node == null)
                {
                    string who = Transfers.Core.Links.GetName(download.Target);
                    DownloadList.Nodes.Add(new DownloadNode(who, download));
                }
                else
                {
                    missing.Remove(node);
                    node.RefreshNode();
                }
            }

            foreach (TreeListNode node in missing)
                DownloadList.Nodes.Remove(node);
   
            DownloadList.ExpandAll();
            

            // Uploads
            missing = UploadList.Nodes.GetList();

            foreach (List<FileUpload> list in Transfers.UploadMap.Values)
                foreach (FileUpload upload in list)
                {
                    UploadNode node = FindUpload(upload);

                    if (node == null)
                        UploadList.Nodes.Add(new UploadNode(upload));
                    else
                    {
                        missing.Remove(node);
                        node.RefreshNode();
                    }
                }

            foreach (TreeListNode node in missing)
                UploadList.Nodes.Remove(node);

            UploadList.ExpandAll();
         

        }

        private DownloadNode FindDownload(FileDownload download)
        {
            foreach (DownloadNode node in DownloadList.Nodes)
                if (node.Download == download)
                    return node;

            return null;
        }

        private UploadNode FindUpload(FileUpload upload)
        {
            foreach (UploadNode node in UploadList.Nodes)
                if (node.Upload == upload)
                    return node;

            return null;
        }
    }

    internal class DownloadNode : TreeListNode
    {
        internal FileDownload Download;
        string Who = "";

        internal DownloadNode(string who, FileDownload download)
        {
            Who = who;
            Download = download;

            RefreshNode();
        }

        internal void RefreshNode()
        {
            // who component  hash  completed/total sources searching
            string status = Who + ",  " +
                Download.Details.Service.ToString() + ",  " +
                Utilities.BytestoHex(Download.Details.Hash).Substring(0, 6).ToUpper() + ",  " +
                Utilities.CommaIze(Download.FilePos.ToString()) + " / " + Utilities.CommaIze(Download.Details.Size.ToString()) + "   ";

            if (Download.Sources.Count > 0)
                status += Download.Sources.Count.ToString() + " Sources";

            if (Download.Searching)
                status += "   Searching";

            Text = status;

            // sessions
            List<TreeListNode> missing = Nodes.GetList();

            foreach (RudpSession session in Download.Sessions)
            {
                SessionNode node = FindSession(session);

                if (node == null)
                    Nodes.Add(new SessionNode(session));
                else
                {
                    missing.Remove(node);
                    node.RefreshNode();
                }
            }

            foreach (TreeListNode node in missing)
                Nodes.Remove(node);
        }

        private SessionNode FindSession(RudpSession session)
        {
            foreach (SessionNode node in Nodes)
                if (node.Session == session)
                    return node;

            return null;
        }
    }

    internal class SessionNode : TreeListNode
    {
        internal RudpSession Session;

        internal SessionNode(RudpSession session)
        {
            Session = session;

            RefreshNode();
        }

        internal void RefreshNode()
        {
            // who   status

            Text = Session.Name + ",  " + Session.Status.ToString();
        }
    }

    internal class UploadNode : TreeListNode
    {
        internal FileUpload Upload;

        internal UploadNode(FileUpload upload)
        {
            Upload = upload;

            Nodes.Add(new TreeListNode());

            RefreshNode();
        }

        internal void RefreshNode()
        {
            // who  component  hash  completed/total  session.status 
            string status = Upload.Session.Name + ",  " +
                Upload.Details.Service.ToString() + ",  " +
                Utilities.BytestoHex(Upload.Details.Hash).Substring(0, 6).ToUpper() + ",  " +
                Utilities.CommaIze(Upload.FilePos.ToString()) + " / " + Utilities.CommaIze(Upload.Details.Size.ToString()) + "   " +
                Upload.Session.Status.ToString() + "   ";

            if (Upload.Done)
                status += "Done    ";

            Text = status;


            // session sendbuff, session encryptbuff, comm sendbuff, comm sendbuff
            status = "Session.SendBuff " + Upload.Session.SendBuffSize.ToString() + " / " + RudpSession.BUFF_SIZE.ToString() + "    " +
                "Session.EncryptBuff " + Upload.Session.EncryptBuffSize.ToString() + " / " + RudpSession.BUFF_SIZE.ToString() + "    " +
                "Comm.SendBuff " + Upload.Session.Comm.SendBuffLength.ToString() + " / " + RudpSocket.SEND_BUFFER_SIZE.ToString() + "    ";

            if (Upload.Session.Comm.RudpSendBlock)
                status += "Blocking    ";


            Nodes[0].Text = status;
        }
    }

}