namespace VoiceDevelopment
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //ElevenLabsTTS.TextToSpeechAsync("Welcome to Tile Siege. To play, players must hunt all the red targets before stepping into the green safezone. The levels only get harder over time. Good luck! 3... 2... 1... GO!", @"C:\\code\gameintro.mp3");

            ElevenLabsTTS.CreateVoiceFiles(null);
        }
    }
}
