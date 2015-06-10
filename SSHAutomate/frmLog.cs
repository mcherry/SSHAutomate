using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SSHAutomate
{
    public partial class frmLog : Form
    {
        public frmLog()
        {
            InitializeComponent();
        }

        public void addLogEntry(String entry)
        {
            lstLog.Items.Add("[" + DateTime.Now + "]: " + entry);

            int visibleItems = lstLog.ClientSize.Height / lstLog.ItemHeight;
            lstLog.TopIndex = Math.Max(lstLog.Items.Count - visibleItems + 1, 0);
        }

        public void copyLogToClipboard()
        {
            string s1 = "";
            foreach (object item in lstLog.Items) s1 += item.ToString() + "\r\n";
            Clipboard.SetText(s1);
        }
    }
}
