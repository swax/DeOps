using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace RiseOp.Services.Chat
{
    internal partial class ChatSplit : UserControl
    {
        internal uint RoomID;


        internal ChatSplit(uint id)
        {
            InitializeComponent();

            RoomID = id;
        }
    }
}
