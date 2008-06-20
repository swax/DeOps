using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Transport;

namespace RiseOp.Interface.Tools
{
	/// <summary>
	/// Summary description for GraphForm.
	/// </summary>
	internal class GraphForm : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        OpCore Core;
        DhtNetwork Network;
        DhtRouting Routing;

		Bitmap DisplayBuffer;

		bool Redraw;

		internal delegate void UpdateGraphHandler();
		internal UpdateGraphHandler UpdateGraph;


        internal static void Show(DhtNetwork network)
        {
            if (network.GuiGraph == null)
                network.GuiGraph = new GraphForm(network);

            network.GuiGraph.Show();
            network.GuiGraph.Activate();

        }

        internal GraphForm(DhtNetwork network)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            Network = network;
            Core = Network.Core;
            Routing = Network.Routing;

			UpdateGraph = new UpdateGraphHandler(AsyncUpdateGraph);

            Text = "Graph (" + Network.GetLabel() + ")";

			Redraw = true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraphForm));
            this.SuspendLayout();
            // 
            // GraphForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GraphForm";
            this.Text = "Graph";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.GraphForm_Paint);
            this.Resize += new System.EventHandler(this.GraphForm_Resize);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.GraphForm_Closing);
            this.ResumeLayout(false);

		}
		#endregion

		private void GraphForm_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			if(ClientSize.Width == 0 || ClientSize.Height == 0)
				return;
			
			if(DisplayBuffer == null )
				DisplayBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);

			if( !Redraw )
			{
				e.Graphics.DrawImage(DisplayBuffer, 0, 0);
				return;
			}
			Redraw = false;

			// background
			Graphics buffer = Graphics.FromImage(DisplayBuffer);
			
			buffer.Clear(Color.DarkBlue);
			buffer.SmoothingMode = SmoothingMode.AntiAlias;

			// back black ellipse
			Point centerPoint = new Point(ClientSize.Width / 2, ClientSize.Height / 2);

			int maxRadius = (ClientSize.Height > ClientSize.Width) ? ClientSize.Width / 2 : ClientSize.Height / 2;
			maxRadius -= 5;

			buffer.FillEllipse( new SolidBrush(Color.Black), GetBoundingBox(centerPoint, maxRadius));

			uint localID = IDto32(Network.Local.UserID);

            List<Rectangle> contactPoints = new List<Rectangle>();
            List<Rectangle> cachePoints = new List<Rectangle>();

			// draw circles
			int i = 0;
			float sweepAngle = 360;
			Pen orangePen = new Pen(Color.Orange, 2);
            int arcs = 0;

            int maxLevels = 10;
            int drawBuckets = Routing.BucketList.Count > maxLevels ? maxLevels : Routing.BucketList.Count;

			lock(Routing.BucketList)
				foreach(DhtBucket bucket in Routing.BucketList)
				{
                    if (sweepAngle < 0.1 || i >= maxLevels)
                        break;

					// draw lines
                    if (!bucket.Last)
                    {
                        int rad = maxRadius * i / drawBuckets;

                        uint lowpos = localID >> (32 - i);
                        lowpos = lowpos << (32 - i);
                        uint highpos = lowpos | ((uint)1 << 31 - i);

                        float startAngle = 360 * ((float)lowpos / (float)uint.MaxValue);

                        if (rad > 0)
                        {
                            arcs++;
                            buffer.DrawArc(orangePen, GetBoundingBox(centerPoint, rad), startAngle, sweepAngle);

                            buffer.DrawLine(orangePen, GetCircumPoint(centerPoint, rad, lowpos), GetCircumPoint(centerPoint, maxRadius, lowpos));
                            buffer.DrawLine(orangePen, GetCircumPoint(centerPoint, rad, highpos), GetCircumPoint(centerPoint, maxRadius, highpos));
                        }
                        else
                            buffer.DrawLine(orangePen, GetCircumPoint(centerPoint, maxRadius, 0), GetCircumPoint(centerPoint, maxRadius, uint.MaxValue / 2));

                        // draw text
                        lowpos = localID >> (31 - i);
                        highpos = (lowpos + 1) << (31 - i);
                        lowpos = lowpos << (31 - i);


                        Point textPoint = GetCircumPoint(centerPoint, rad + 10, lowpos + (highpos - lowpos) / 2);

                        if ((localID & ((uint)1 << (31 - i))) > 0)
                            buffer.DrawString("1", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold), new SolidBrush(Color.White), textPoint);
                        else
                            buffer.DrawString("0", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold), new SolidBrush(Color.White), textPoint);
                    }

					foreach(DhtContact contact in bucket.ContactList)
                    {
						contactPoints.Add(GetBoundingBox(GetCircumPoint(centerPoint, maxRadius, IDto32(contact.UserID)), 4));

                        if(Routing.InCacheArea(contact.UserID))
                            cachePoints.Add(GetBoundingBox(GetCircumPoint(centerPoint, maxRadius, IDto32(contact.UserID)), 1));
                    }

					sweepAngle /= 2;
					i++;
				}

			// draw contacts
			foreach(Rectangle rect in contactPoints)
                buffer.FillEllipse(new SolidBrush(Color.White), rect);

            foreach (Rectangle rect in cachePoints)
                buffer.FillEllipse(new SolidBrush(Color.Black), rect);

			// draw proxies
			lock(Network.TcpControl.SocketList)
                foreach (TcpConnect connection in Network.TcpControl.SocketList)
				{
					if(connection.Proxy == ProxyType.Server)
						buffer.FillEllipse(new SolidBrush(Color.Green), GetBoundingBox(GetCircumPoint(centerPoint, maxRadius, IDto32(connection.UserID)), 4));
					
					if(connection.Proxy == ProxyType.ClientNAT || connection.Proxy == ProxyType.ClientBlocked)
						buffer.FillEllipse(new SolidBrush(Color.Red), GetBoundingBox(GetCircumPoint(centerPoint, maxRadius, IDto32(connection.UserID)), 4));
				}

			// draw self
			buffer.FillEllipse(new SolidBrush(Color.Yellow), GetBoundingBox(GetCircumPoint(centerPoint, maxRadius, localID), 4));

		
			// Copy buffer to display
			e.Graphics.DrawImage(DisplayBuffer, 0, 0);
		}

		private void GraphForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Network.GuiGraph = null;
		}

		private void GraphForm_Resize(object sender, System.EventArgs e)
		{
			if(ClientSize.Width > 0 && ClientSize.Height > 0)
			{
				DisplayBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
				Redraw = true;
				Invalidate();
			}
		}

		internal void AsyncUpdateGraph()
		{
			Redraw = true;
			Invalidate();
		}

		Rectangle GetBoundingBox(Point center, int rad)
		{
			return new Rectangle(center.X - rad, center.Y - rad, rad * 2, rad * 2);
		}

		Point GetCircumPoint(Point center, int rad, uint position)
		{
			double fraction = (double) position / (double) uint.MaxValue;

			int xPos = (int) ((double) rad  * Math.Cos( fraction * 2*Math.PI)) + center.X; 
			int yPos = (int) ((double) rad  * Math.Sin( fraction * 2*Math.PI)) + center.Y; 

			return new Point(xPos, yPos);
		}

		uint IDto32(UInt64 id)
		{
			uint retVal = 0;

			for(int i = 0; i < 32; i++)
				if( Utilities.GetBit(id, i) == 1)
					retVal |= ((uint) 1) << (31 - i);

			return retVal;
		}

    }
}
