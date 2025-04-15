namespace DoorLock
{
    partial class Form1
    {
        private System.Windows.Forms.ComboBox cmbComPorts;
        private System.Windows.Forms.Button btnOpenDoor;
        private System.Windows.Forms.Button btnCloseDoor;
        private System.Windows.Forms.Label lblComPort;

        private void InitializeComponent()
        {
            this.cmbComPorts = new System.Windows.Forms.ComboBox();
            this.btnOpenDoor = new System.Windows.Forms.Button();
            this.btnCloseDoor = new System.Windows.Forms.Button();
            this.lblComPort = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cmbComPorts
            // 
            this.cmbComPorts.FormattingEnabled = true;
            this.cmbComPorts.Location = new System.Drawing.Point(30, 50);
            this.cmbComPorts.Name = "cmbComPorts";
            this.cmbComPorts.Size = new System.Drawing.Size(150, 23);
            this.cmbComPorts.TabIndex = 0;
            // 
            // btnOpenDoor
            // 
            this.btnOpenDoor.Location = new System.Drawing.Point(30, 100);
            this.btnOpenDoor.Name = "btnOpenDoor";
            this.btnOpenDoor.Size = new System.Drawing.Size(75, 23);
            this.btnOpenDoor.TabIndex = 1;
            this.btnOpenDoor.Text = "Open Door";
            this.btnOpenDoor.UseVisualStyleBackColor = true;
            this.btnOpenDoor.Click += new System.EventHandler(this.btnOpenDoor_Click);
            // 
            // btnCloseDoor
            // 
            this.btnCloseDoor.Location = new System.Drawing.Point(120, 100);
            this.btnCloseDoor.Name = "btnCloseDoor";
            this.btnCloseDoor.Size = new System.Drawing.Size(75, 23);
            this.btnCloseDoor.TabIndex = 2;
            this.btnCloseDoor.Text = "Close Door";
            this.btnCloseDoor.UseVisualStyleBackColor = true;
            this.btnCloseDoor.Click += new System.EventHandler(this.btnCloseDoor_Click);
            // 
            // lblComPort
            // 
            this.lblComPort.AutoSize = true;
            this.lblComPort.Location = new System.Drawing.Point(30, 30);
            this.lblComPort.Name = "lblComPort";
            this.lblComPort.Size = new System.Drawing.Size(61, 15);
            this.lblComPort.TabIndex = 3;
            this.lblComPort.Text = "COM Port:";
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(284, 161);
            this.Controls.Add(this.lblComPort);
            this.Controls.Add(this.btnCloseDoor);
            this.Controls.Add(this.btnOpenDoor);
            this.Controls.Add(this.cmbComPorts);
            this.Name = "Form1";
            this.Text = "Door Lock Controller";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
