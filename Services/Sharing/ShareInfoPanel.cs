using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RiseOp.Services.Sharing
{
    public partial class ShareInfoPanel : UserControl
    {
        const string HelpPage = @"<html>
                                <head>
                                <style>
                                    body { font-family:tahoma; font-size:12px;margin-top:3px;}
                                    td { font-size:10px;vertical-align: middle; }
                                </style>
                                <script>
                                     function UpdateStatus(id, text)
                                    {
                                        document.getElementById(id).innerHTML = text;
                                    }
                                                                                                                    
                                </script>
                                </head>

                                <body bgcolor=#f5f5f5>
                                <div style='left-margin:10'>

                                <ul style='margin-top:3'>
                                    <li>Files in your Share are only available when you are online.</li>
                                    <li>To share a file give someone the riseop:// link associated with the file.</li>
                                    <li>Others can remotely browse your files marked 'public'</li>
                                    <li>When sending to multiple people, transfers are automatically multi-sourced to speed the process.</li>
                                </ul>

                                </div>
                                </body>
                                </html>";

        public ShareInfoPanel()
        {
            InitializeComponent();

            InfoBrowser.DocumentText = HelpPage;
        }
    }
}
