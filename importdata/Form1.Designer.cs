using System.ComponentModel;

namespace importdata
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            openFileDialog1 = new OpenFileDialog();
            btnSelectFiles = new Button();
            lstSelectedFiles = new ListBox();
            btnImport = new Button();
            cmbLocation = new ComboBox();
            cmbImporttype = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.FileOk += openFileDialog1_FileOk;
            // 
            // btnSelectFiles
            // 
            btnSelectFiles.Location = new Point(270, 41);
            btnSelectFiles.Name = "btnSelectFiles";
            btnSelectFiles.Size = new Size(75, 23);
            btnSelectFiles.TabIndex = 0;
            btnSelectFiles.Text = "select files";
            btnSelectFiles.UseVisualStyleBackColor = true;
            btnSelectFiles.Click += btnSelectFiles_Click_1;
            // 
            // lstSelectedFiles
            // 
            lstSelectedFiles.FormattingEnabled = true;
            lstSelectedFiles.ItemHeight = 15;
            lstSelectedFiles.Location = new Point(29, 195);
            lstSelectedFiles.Name = "lstSelectedFiles";
            lstSelectedFiles.Size = new Size(120, 94);
            lstSelectedFiles.TabIndex = 1;
            // 
            // btnImport
            // 
            btnImport.Location = new Point(270, 235);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(75, 23);
            btnImport.TabIndex = 2;
            btnImport.Text = "Import";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click_1;
            // 
            // cmbLocation
            // 
            cmbLocation.FormattingEnabled = true;
            cmbLocation.Items.AddRange(new object[] { "windsor", "stc", "oakville", "london" });
            cmbLocation.Location = new Point(83, 42);
            cmbLocation.Name = "cmbLocation";
            cmbLocation.Size = new Size(121, 23);
            cmbLocation.TabIndex = 3;
            // 
            // cmbImporttype
            // 
            cmbImporttype.FormattingEnabled = true;
            cmbImporttype.Items.AddRange(new object[] { "redemptions", "bookings", "membership" });
            cmbImporttype.Location = new Point(83, 103);
            cmbImporttype.Name = "cmbImporttype";
            cmbImporttype.Size = new Size(121, 23);
            cmbImporttype.TabIndex = 4;
            cmbImporttype.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(20, 45);
            label1.Name = "label1";
            label1.Size = new Size(50, 15);
            label1.TabIndex = 5;
            label1.Text = "location";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(8, 106);
            label2.Name = "label2";
            label2.Size = new Size(62, 15);
            label2.TabIndex = 6;
            label2.Text = "import file";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(cmbImporttype);
            Controls.Add(cmbLocation);
            Controls.Add(btnImport);
            Controls.Add(lstSelectedFiles);
            Controls.Add(btnSelectFiles);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
           
        }

        #endregion

        private OpenFileDialog openFileDialog1;
        private Button btnSelectFiles;
        private ListBox lstSelectedFiles;
        private Button btnImport;
        private ComboBox cmbLocation;
        private ComboBox cmbImporttype;
        private Label label1;
        private Label label2;
    }
}
