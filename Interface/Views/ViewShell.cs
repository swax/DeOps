using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Interface
{
    public partial class ViewShell : UserControl
    {
        internal ExternalView External;
        

        public ViewShell()
        {
            InitializeComponent();
        }

        internal virtual void Init() {}

        internal virtual bool Fin() { return true; }

        internal virtual string GetTitle() { return ""; }

        internal virtual Size GetDefaultSize()
        {
            return new Size(100, 100);
        }

        internal virtual Icon GetIcon()
        {
            return null;
        }
    }
}
