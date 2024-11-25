namespace LockTester
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox txtComPort;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnRelayOn;
        private System.Windows.Forms.Button btnRelayOff;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtComPort = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnRelayOn = new System.Windows.Forms.Button();
            this.btnRelayOff = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // txtComPort
            this.txtComPort.Location = new System.Drawing.Point(20, 20);
            this.txtComPort.Name = "txtComPort";
            this.txtComPort.Size = new System.Drawing.Size(200, 22);
            this.txtComPort.TabIndex = 0;

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(20, 60);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(100, 16);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Status: Not connected";

            // btnConnect
            this.btnConnect.Location = new System.Drawing.Point(230, 20);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(100, 23);
            this.btnConnect.TabIndex = 2;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);

            // btnRelayOn
            this.btnRelayOn.Location = new System.Drawing.Point(20, 100);
            this.btnRelayOn.Name = "btnRelayOn";
            this.btnRelayOn.Size = new System.Drawing.Size(100, 23);
            this.btnRelayOn.TabIndex = 3;
            this.btnRelayOn.Text = "Turn ON";
            this.btnRelayOn.UseVisualStyleBackColor = true;
            this.btnRelayOn.Enabled = false;
            this.btnRelayOn.Click += new System.EventHandler(this.btnRelayOn_Click);

            // btnRelayOff
            this.btnRelayOff.Location = new System.Drawing.Point(130, 100);
            this.btnRelayOff.Name = "btnRelayOff";
            this.btnRelayOff.Size = new System.Drawing.Size(100, 23);
            this.btnRelayOff.TabIndex = 4;
            this.btnRelayOff.Text = "Turn OFF";
            this.btnRelayOff.UseVisualStyleBackColor = true;
            this.btnRelayOff.Enabled = false;
            this.btnRelayOff.Click += new System.EventHandler(this.btnRelayOff_Click);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 150);
            this.Controls.Add(this.btnRelayOff);
            this.Controls.Add(this.btnRelayOn);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtComPort);
            this.Name = "MainForm";
            this.Text = "Lock Controller Test App";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }


        #endregion
    }
}

