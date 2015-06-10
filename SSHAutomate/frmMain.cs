using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace SSHAutomate
{
    public partial class frmMain : Form
    {
        frmLog logForm = new frmLog();
        bool ClickedStop = false;

        /*
         * getCount()
         * 
         * Returns count of actual items in TextBox (not including blank lines, carriage returns, etc)
         * 
         */
        private long getCount(TextBox txtList)
        {
            long itemCount = 0;

            for (int a = 0; a < txtList.Lines.Length; a++)
            {
                if (txtList.Lines[a].Trim() != "") itemCount++;
            }

            return itemCount;
        }

        private void toggleEnabled(bool enabled)
        {
            txtUser.Enabled = enabled;
            txtPass.Enabled = enabled;
            txtPort.Enabled = enabled;
            txtServers.Enabled = enabled;
            chkSudo.Enabled = enabled;
            txtCmds.Enabled = enabled;

            if (enabled == false)
            {
                btnConnect.Visible = false;
                btnStop.Visible = true;
            }
            else
            {
                btnConnect.Visible = true;
                btnStop.Visible = false;

                ClickedStop = false;
            }
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void chkSudo_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            bool HasErrors = false;
            String ErrorFields = "";

            if (txtUser.Text == "")
            {
                HasErrors = true;
                ErrorFields += "Username\n";
            }

            if (txtCmds.Text == "")
            {
                HasErrors = true;
                ErrorFields += "Commands\n";
            }

            if (txtPort.Text == "")
            {
                HasErrors = true;
                ErrorFields += "Port\n";
            }

            if (txtServers.Text == "")
            {
                HasErrors = true;
                ErrorFields += "Servers\n";
            }

            if (HasErrors == true)
            {
                MessageBox.Show("The following required fields were left empty:\n\n" + ErrorFields, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (getCount(txtServers) > 0)
                {
                    prgServers.Minimum = 0;
                    prgServers.Maximum = txtServers.Lines.Length;

                    toggleEnabled(false);

                    if (logForm.IsDisposed == true)
                    {
                        logForm = new frmLog();
                    }
                    logForm.Show();

                    ThreadStart AutoRef = new ThreadStart(AutomationThread);
                    Thread AutoThread = new Thread(AutoRef);

                    stsMain.Text = "Starting automation";

                    AutoThread.Start();
                }
            }
        }

        private void AutomationThread()
        {
            for (int a = 0; a < txtServers.Lines.Length; a++)
            {
                String SingleServer = txtServers.Lines[a].Trim();

                if (SingleServer != "")
                {
                    addLogEntry(SingleServer + ": Connecting as '" + txtUser.Text + "' on port " + txtPort.Text);

                    String ReturnResult = "";

                    try
                    {
                        KeyboardInteractiveAuthenticationMethod kauth = new KeyboardInteractiveAuthenticationMethod(txtUser.Text);
                        PasswordAuthenticationMethod pauth = new PasswordAuthenticationMethod(txtUser.Text, txtPass.Text);

                        kauth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(HandleKeyEvent);

                        ConnectionInfo connectionInfo = new ConnectionInfo(SingleServer, Convert.ToInt32(txtPort.Text), txtUser.Text, pauth, kauth);

                        using (var ssh = new SshClient(connectionInfo))
                        {
                            ssh.Connect();

                            if (chkSudo.Checked == true)
                            {
                                addLogEntry(SingleServer + ": Using sudo");
                            }

                            if (txtCmds.Lines.Length > 0)
                            {
                                for (int b = 0; b < txtCmds.Lines.Length; b++)
                                {
                                    String SingleCommand = txtCmds.Lines[b].Trim();

                                    if (SingleCommand != "")
                                    {
                                        addLogEntry(SingleServer + ": Running " + SingleCommand);

                                        if (chkSudo.Checked == true)
                                        {
                                            String SudoCommand = "echo \"" + txtPass.Text + "\" | sudo -S bash -c \"" + SingleCommand + "\"";
                                            SingleCommand = SudoCommand;
                                        }

                                        var cmd = ssh.RunCommand(SingleCommand);
                                        ReturnResult = cmd.Result;
                                    }

                                    if (ClickedStop == true)
                                    {
                                        addLogEntry("Stopping commands");
                                        
                                        break;
                                    }
                                }
                            }

                            addLogEntry(SingleServer + ": Disconnecting");
                            ssh.Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        addLogEntry(ex.Message);
                    }
                }

                if (ClickedStop == true)
                {
                   addLogEntry("Clicked stop");
                   break;
                }

                try
                {
                    prgServers.Invoke((MethodInvoker)delegate { this.prgServers.Value += 1; });
                }
                catch (Exception)
                {
                    // don't really care about errors
                }
            }

            addLogEntry("Copying log to clipboard");
            copyLogToClipboard();
            
            try
            {
                this.Invoke((MethodInvoker)delegate { this.toggleEnabled(true); });
                this.Invoke((MethodInvoker)delegate { stsMain.Text = "Done"; });

                prgServers.Invoke((MethodInvoker)delegate { this.prgServers.Value = 0; });
            }
            catch (Exception)
            {
                // don't really care about errors
            }
        }

        public void addLogEntry(String txtEntry)
        {
            try
            {
                if (logForm.InvokeRequired)
                {
                    logForm.Invoke(new Action<string>(addLogEntry), txtEntry);
                    return;
                }

                logForm.addLogEntry(txtEntry);
            }
            catch (Exception)
            {
                // don't really care about errors
            }
        }

        public void copyLogToClipboard()
        {
            try
            {
                if (logForm.InvokeRequired)
                {
                    logForm.Invoke((MethodInvoker)delegate { this.copyLogToClipboard(); });
                    return;
                }

                logForm.copyLogToClipboard();
            }
            catch (Exception)
            {
                // don't really care about errors
            }
        }

        void HandleKeyEvent(Object sender, AuthenticationPromptEventArgs e)
        {
            foreach (AuthenticationPrompt prompt in e.Prompts)
            {
                if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = txtPass.Text;
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stsMain.Text = "Stopped";

            ClickedStop = true;
        }

        private void txtUser_TextChanged(object sender, EventArgs e)
        {

        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.ActiveControl = txtUser;
        }

        private void txtServers_TextChanged(object sender, EventArgs e)
        {
            String statusMsg;
            long serverCount = getCount(txtServers);

            if (serverCount > 0)
            {
                statusMsg = serverCount + " Server";
                if (serverCount > 1) statusMsg += "s";
            }
            else
            {
                statusMsg = "0 Servers";
            }

            stsServers.Text = statusMsg;
        }

        private void txtCmds_TextChanged(object sender, EventArgs e)
        {
            String statusMsg;
            long cmdCount = getCount(txtCmds);

            if (cmdCount > 0)
            {
                statusMsg = cmdCount + " Command";
                if (cmdCount > 1) statusMsg += "s";
            }
            else
            {
                statusMsg = "0 Commands";
            }

            stsCmds.Text = statusMsg;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAbout aboutForm = new frmAbout();
            aboutForm.ShowDialog();
        }
    }
}
