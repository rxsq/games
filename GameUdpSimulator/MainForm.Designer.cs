using System;
using System.Drawing;
using System.Windows.Forms;

public partial class MainForm : Form
{
    private void InitializeComponent()
    {
            this.lblNumberOfButtons = new System.Windows.Forms.Label();
            this.txtNumberOfButtons = new System.Windows.Forms.TextBox();
            this.lblButtonsPerRow = new System.Windows.Forms.Label();
            this.txtButtonsPerRow = new System.Windows.Forms.TextBox();
            this.lblControllersCount = new System.Windows.Forms.Label();
            this.txtControllersCount = new System.Windows.Forms.TextBox();
            this.btnGenerateButtons = new System.Windows.Forms.Button();
            this.lblPlayersCount = new System.Windows.Forms.Label();
            this.txtPlayersCount = new System.Windows.Forms.TextBox();
            this.btnGeneratePlayers = new System.Windows.Forms.Button();
            this.panelContainer = new System.Windows.Forms.Panel();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.playersContainer = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // lblNumberOfButtons
            // 
            this.lblNumberOfButtons.AutoSize = true;
            this.lblNumberOfButtons.Location = new System.Drawing.Point(13, 13);
            this.lblNumberOfButtons.Name = "lblNumberOfButtons";
            this.lblNumberOfButtons.Size = new System.Drawing.Size(95, 13);
            this.lblNumberOfButtons.TabIndex = 0;
            this.lblNumberOfButtons.Text = "Number of Buttons";
            // 
            // txtNumberOfButtons
            // 
            this.txtNumberOfButtons.Location = new System.Drawing.Point(13, 30);
            this.txtNumberOfButtons.Name = "txtNumberOfButtons";
            this.txtNumberOfButtons.Size = new System.Drawing.Size(100, 20);
            this.txtNumberOfButtons.TabIndex = 1;
            this.txtNumberOfButtons.Text = "336";
            // 
            // lblButtonsPerRow
            // 
            this.lblButtonsPerRow.AutoSize = true;
            this.lblButtonsPerRow.Location = new System.Drawing.Point(13, 60);
            this.lblButtonsPerRow.Name = "lblButtonsPerRow";
            this.lblButtonsPerRow.Size = new System.Drawing.Size(86, 13);
            this.lblButtonsPerRow.TabIndex = 2;
            this.lblButtonsPerRow.Text = "Buttons per Row";
            // 
            // txtButtonsPerRow
            // 
            this.txtButtonsPerRow.Location = new System.Drawing.Point(13, 77);
            this.txtButtonsPerRow.Name = "txtButtonsPerRow";
            this.txtButtonsPerRow.Size = new System.Drawing.Size(100, 20);
            this.txtButtonsPerRow.TabIndex = 3;
            this.txtButtonsPerRow.Text = "14";
            // 
            // lblControllersCount
            // 
            this.lblControllersCount.AutoSize = true;
            this.lblControllersCount.Location = new System.Drawing.Point(13, 107);
            this.lblControllersCount.Name = "lblControllersCount";
            this.lblControllersCount.Size = new System.Drawing.Size(87, 13);
            this.lblControllersCount.TabIndex = 4;
            this.lblControllersCount.Text = "Controllers Count";
            // 
            // txtControllersCount
            // 
            this.txtControllersCount.Location = new System.Drawing.Point(13, 124);
            this.txtControllersCount.Name = "txtControllersCount";
            this.txtControllersCount.Size = new System.Drawing.Size(100, 20);
            this.txtControllersCount.TabIndex = 5;
            this.txtControllersCount.Text = "3";
            // 
            // btnGenerateButtons
            // 
            this.btnGenerateButtons.Location = new System.Drawing.Point(13, 160);
            this.btnGenerateButtons.Name = "btnGenerateButtons";
            this.btnGenerateButtons.Size = new System.Drawing.Size(100, 23);
            this.btnGenerateButtons.TabIndex = 6;
            this.btnGenerateButtons.Text = "Generate Buttons";
            this.btnGenerateButtons.UseVisualStyleBackColor = true;
            this.btnGenerateButtons.Click += new System.EventHandler(this.btnGenerateButtons_Click);
            // 
            // lblPlayersCount
            // 
            this.lblPlayersCount.AutoSize = true;
            this.lblPlayersCount.Location = new System.Drawing.Point(13, 218);
            this.lblPlayersCount.Name = "lblPlayersCount";
            this.lblPlayersCount.Size = new System.Drawing.Size(72, 13);
            this.lblPlayersCount.TabIndex = 4;
            this.lblPlayersCount.Text = "Players Count";
            // 
            // txtPlayersCount
            // 
            this.txtPlayersCount.Location = new System.Drawing.Point(13, 234);
            this.txtPlayersCount.Name = "txtPlayersCount";
            this.txtPlayersCount.Size = new System.Drawing.Size(100, 20);
            this.txtPlayersCount.TabIndex = 5;
            this.txtPlayersCount.Text = "1";
            // 
            // btnGeneratePlayers
            // 
            this.btnGeneratePlayers.Location = new System.Drawing.Point(13, 260);
            this.btnGeneratePlayers.Name = "btnGeneratePlayers";
            this.btnGeneratePlayers.Size = new System.Drawing.Size(100, 23);
            this.btnGeneratePlayers.TabIndex = 6;
            this.btnGeneratePlayers.Text = "Generate Players";
            this.btnGeneratePlayers.UseVisualStyleBackColor = true;
            this.btnGeneratePlayers.Click += new System.EventHandler(this.btnGeneratePlayers_Click);
            // 
            // panelContainer
            // 
            this.panelContainer.Location = new System.Drawing.Point(120, 10);
            this.panelContainer.Name = "panelContainer";
            this.panelContainer.Size = new System.Drawing.Size(798, 540);
            this.panelContainer.TabIndex = 7;
            this.panelContainer.Paint += new System.Windows.Forms.PaintEventHandler(this.panelContainer_Paint);
            // 
            // playersContainer
            // 
            this.playersContainer.Location = new System.Drawing.Point(0, 0);
            this.playersContainer.Name = "playersContainer";
            this.playersContainer.Size = new System.Drawing.Size(200, 100);
            this.playersContainer.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1584, 1061);
            this.Controls.Add(this.lblNumberOfButtons);
            this.Controls.Add(this.txtNumberOfButtons);
            this.Controls.Add(this.lblButtonsPerRow);
            this.Controls.Add(this.txtButtonsPerRow);
            this.Controls.Add(this.lblControllersCount);
            this.Controls.Add(this.txtControllersCount);
            this.Controls.Add(this.btnGenerateButtons);
            this.Controls.Add(this.lblPlayersCount);
            this.Controls.Add(this.txtPlayersCount);
            this.Controls.Add(this.btnGeneratePlayers);
            this.Controls.Add(this.panelContainer);
            this.Name = "MainForm";
            this.Text = "Button Generator";
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    private Panel playersContainer;
}
