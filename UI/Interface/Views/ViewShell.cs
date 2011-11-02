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
        public ExternalView External;

        public bool BlockReinit;

        public ViewShell()
        {
            InitializeComponent();
        }

        public virtual void Init() {}

        public virtual bool Fin() { return true; }

        public virtual string GetTitle(bool small) { return ""; }

        public virtual Size GetDefaultSize()
        {
            return new Size(100, 100);
        }

        public virtual Icon GetIcon()
        {
            return null;
        }
    }
}
