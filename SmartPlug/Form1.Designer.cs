namespace SmartPlug
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
            button1 = new Button();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            txtMiniGolfTimer = new TextBox();
            label3 = new Label();
            lblMessage = new Label();
            checkBox1 = new CheckBox();
            label4 = new Label();
            textBox3 = new TextBox();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(592, 82);
            button1.Name = "button1";
            button1.Size = new Size(178, 23);
            button1.TabIndex = 0;
            button1.Text = "minigolf bottom lights on";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(71, 83);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(138, 23);
            textBox1.TabIndex = 1;
            textBox1.Text = "10.0.1.27";
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(227, 84);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(135, 23);
            textBox2.TabIndex = 2;
            textBox2.Text = "10.0.1.201";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Cursor = Cursors.IBeam;
            label1.Location = new Point(101, 64);
            label1.Name = "label1";
            label1.Size = new Size(95, 15);
            label1.TabIndex = 3;
            label1.Text = "20:23:51:60:4B:24";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(247, 64);
            label2.Name = "label2";
            label2.Size = new Size(94, 15);
            label2.TabIndex = 4;
            label2.Text = "20:23:51:60:54:16";
            // 
            // txtMiniGolfTimer
            // 
            txtMiniGolfTimer.Location = new Point(389, 82);
            txtMiniGolfTimer.Name = "txtMiniGolfTimer";
            txtMiniGolfTimer.Size = new Size(100, 23);
            txtMiniGolfTimer.TabIndex = 5;
            txtMiniGolfTimer.Text = "60";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(420, 55);
            label3.Name = "label3";
            label3.Size = new Size(69, 15);
            label3.TabIndex = 6;
            label3.Text = "shutoff Min";
            // 
            // lblMessage
            // 
            lblMessage.AutoSize = true;
            lblMessage.Location = new Point(71, 368);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(66, 15);
            lblMessage.TabIndex = 7;
            lblMessage.Text = "lblMessage";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(509, 84);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(55, 19);
            checkBox1.TabIndex = 8;
            checkBox1.Text = "onoff";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Cursor = Cursors.IBeam;
            label4.Location = new Point(71, 108);
            label4.Name = "label4";
            label4.Size = new Size(95, 15);
            label4.TabIndex = 10;
            label4.Text = "20:23:51:60:4B:24";
            // 
            // textBox3
            // 
            textBox3.Location = new Point(224, 113);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(138, 23);
            textBox3.TabIndex = 9;
            textBox3.Text = "10.0.1.27";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label4);
            Controls.Add(textBox3);
            Controls.Add(checkBox1);
            Controls.Add(lblMessage);
            Controls.Add(label3);
            Controls.Add(txtMiniGolfTimer);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Label label1;
        private Label label2;
        private TextBox txtMiniGolfTimer;
        private Label label3;
        private Label lblMessage;
        private CheckBox checkBox1;
        private Label label4;
        private TextBox textBox3;
    }
}
