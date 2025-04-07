namespace VoiceDevelopment
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox textBoxFilename;
        private System.Windows.Forms.TextBox textBoxVoiceline;
        private System.Windows.Forms.Button buttonSubmit;
        private System.Windows.Forms.ProgressBar progressBar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.textBoxFilename = new System.Windows.Forms.TextBox();
            this.textBoxVoiceline = new System.Windows.Forms.TextBox();
            this.buttonSubmit = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // textBoxFilename
            // 
            this.textBoxFilename.Location = new System.Drawing.Point(12, 12);
            this.textBoxFilename.Name = "textBoxFilename";
            this.textBoxFilename.Size = new System.Drawing.Size(260, 20);
            this.textBoxFilename.TabIndex = 0;
            this.textBoxFilename.PlaceholderText = "Enter filename";
            // 
            // textBoxVoiceline
            // 
            this.textBoxVoiceline.Location = new System.Drawing.Point(12, 38);
            this.textBoxVoiceline.Name = "textBoxVoiceline";
            this.textBoxVoiceline.Size = new System.Drawing.Size(260, 20);
            this.textBoxVoiceline.TabIndex = 1;
            this.textBoxVoiceline.PlaceholderText = "Enter voiceline";
            // 
            // buttonSubmit
            // 
            this.buttonSubmit.Location = new System.Drawing.Point(12, 64);
            this.buttonSubmit.Name = "buttonSubmit";
            this.buttonSubmit.Size = new System.Drawing.Size(75, 23);
            this.buttonSubmit.TabIndex = 2;
            this.buttonSubmit.Text = "Submit";
            this.buttonSubmit.UseVisualStyleBackColor = true;
            this.buttonSubmit.Click += new System.EventHandler(this.buttonSubmit_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 93);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(260, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 3;
            this.progressBar.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 128);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.buttonSubmit);
            this.Controls.Add(this.textBoxVoiceline);
            this.Controls.Add(this.textBoxFilename);
            this.Name = "Form1";
            this.Text = "Voice Development";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
