namespace VoiceDevelopment
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void buttonSubmit_Click(object sender, EventArgs e)
        {
            string filename = textBoxFilename.Text;
            string voiceline = textBoxVoiceline.Text;

            if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(voiceline))
            {
                progressBar.Visible = true;
                buttonSubmit.Enabled = false;

                await ElevenLabsTTS.CreateVoiceFiles(filename, voiceline);

                progressBar.Visible = false;
                buttonSubmit.Enabled = true;

                textBoxFilename.Clear();
                textBoxVoiceline.Clear();

                MessageBox.Show("Voice file created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Please enter both filename and voiceline.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
