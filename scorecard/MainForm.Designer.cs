using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

partial class MainForm
{
    private void InitializeComponent()
    {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(1216, 604);
            this.Name = "MainForm";
            //this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);
            this.ResumeLayout(false);

    }
}
