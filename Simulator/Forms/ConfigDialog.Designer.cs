namespace Simulator.Forms
{
    partial class ConfigDialog
    {
        private void InitializeComponent()
        {
            this.lblNumberOfButtons = new Label();
            this.txtNumberOfButtons = new TextBox();
            this.lblButtonsPerRow = new Label();
            this.txtButtonsPerRow = new TextBox();
            this.lblControllersCount = new Label();
            this.txtControllersCount = new TextBox();
            this.btnGenerate = new Button();

            // lblNumberOfButtons
            this.lblNumberOfButtons.AutoSize = true;
            this.lblNumberOfButtons.Location = new Point(13, 13);
            this.lblNumberOfButtons.Name = "lblNumberOfButtons";
            this.lblNumberOfButtons.Size = new Size(95, 13);
            this.lblNumberOfButtons.TabIndex = 0;
            this.lblNumberOfButtons.Text = "Number of Buttons";

            // txtNumberOfButtons
            this.txtNumberOfButtons.Location = new Point(13, 30);
            this.txtNumberOfButtons.Name = "txtNumberOfButtons";
            this.txtNumberOfButtons.Size = new Size(100, 20);
            this.txtNumberOfButtons.TabIndex = 1;
            this.txtNumberOfButtons.Text = "336";

            // lblButtonsPerRow
            this.lblButtonsPerRow.AutoSize = true;
            this.lblButtonsPerRow.Location = new Point(13, 60);
            this.lblButtonsPerRow.Name = "lblButtonsPerRow";
            this.lblButtonsPerRow.Size = new Size(86, 13);
            this.lblButtonsPerRow.TabIndex = 2;
            this.lblButtonsPerRow.Text = "Buttons per Row";

            // txtButtonsPerRow
            this.txtButtonsPerRow.Location = new Point(13, 77);
            this.txtButtonsPerRow.Name = "txtButtonsPerRow";
            this.txtButtonsPerRow.Size = new Size(100, 20);
            this.txtButtonsPerRow.TabIndex = 3;
            this.txtButtonsPerRow.Text = "14";

            // lblControllersCount
            this.lblControllersCount.AutoSize = true;
            this.lblControllersCount.Location = new Point(13, 107);
            this.lblControllersCount.Name = "lblControllersCount";
            this.lblControllersCount.Size = new Size(87, 13);
            this.lblControllersCount.TabIndex = 4;
            this.lblControllersCount.Text = "Controllers Count";

            // txtControllersCount
            this.txtControllersCount.Location = new Point(13, 124);
            this.txtControllersCount.Name = "txtControllersCount";
            this.txtControllersCount.Size = new Size(100, 20);
            this.txtControllersCount.TabIndex = 5;
            this.txtControllersCount.Text = "3";

            // btnGenerate
            this.btnGenerate.Location = new Point(13, 160);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new Size(100, 23);
            this.btnGenerate.TabIndex = 6;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new EventHandler(this.btnGenerate_Click);

            // ConfigDialog
            this.ClientSize = new Size(284, 201);
            this.Controls.Add(this.lblNumberOfButtons);
            this.Controls.Add(this.txtNumberOfButtons);
            this.Controls.Add(this.lblButtonsPerRow);
            this.Controls.Add(this.txtButtonsPerRow);
            this.Controls.Add(this.lblControllersCount);
            this.Controls.Add(this.txtControllersCount);
            this.Controls.Add(this.btnGenerate);
            this.Name = "ConfigDialog";
            this.Text = "Configuration";
        }

        private Label lblNumberOfButtons;
        private TextBox txtNumberOfButtons;
        private Label lblButtonsPerRow;
        private TextBox txtButtonsPerRow;
        private Label lblControllersCount;
        private TextBox txtControllersCount;
        private Button btnGenerate;
    }
}