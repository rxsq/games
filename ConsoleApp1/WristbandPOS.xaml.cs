using System.Configuration;
using System.Windows;
using Lib;
using System.Windows.Controls;
using System.Windows.Xps;
using Microsoft.VisualBasic; // This is necessary for WPF controls like Button


namespace WpfApp1
{
    public partial class WristbandPOS : Window
    {
        private readonly NFCReaderWriter readerWriter;
        private string selectedGameType = "";
        private int selectedCount = 0;
        private double selectedTime = 0; // Time in hours

        public WristbandPOS()
        {
            InitializeComponent();
            readerWriter = new NFCReaderWriter("I", ConfigurationManager.AppSettings["server"], 0,0);
            readerWriter.StatusChanged += ReaderWriter_StatusChanged;
        }

        private void CountButton_Click(object sender, RoutedEventArgs e)
        {
            selectedGameType = "count";
            selectedCount = int.Parse((sender as System.Windows.Controls.Button).Content.ToString());

            StatusTextBlock.Text = $"Selected: {selectedCount} games (count-based)";
        }

        private void TimeButton_Click(object sender, RoutedEventArgs e)
        {
            selectedGameType = "time";
            selectedTime = double.Parse((sender as System.Windows.Controls.Button).Content.ToString().Split(' ')[0]);
            StatusTextBlock.Text = $"Selected: {selectedTime} hours (time-based)";
        }

        private void initializeVal()
        {
            
        }
        private void ReaderWriter_StatusChanged(object sender, string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                Dispatcher.Invoke(() =>
                {
                    StatusTextBlock.Text = $"Wristband {uid} scanned, updating database...";
                    if (selectedCount > 0 || selectedTime > 0)
                    {
                       var result =readerWriter.InsertRecord(uid, selectedCount, selectedTime > 0 ? 120 : selectedTime);
                        StatusTextBlock.Text = "Wristband is good to go";
                        selectedCount = 0;
                        selectedTime=0;


                    }
                    else
                    {
                    StatusTextBlock.Text = "Select Time or number of games then try. Please retry";
                    }
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    StatusTextBlock.Text = "Error: Wristband not recognized.";
                });
            }
        }

        private void UpdateWristbandTransaction(string uid)
        {
            string status = "I"; // Example status, update based on your logic

            // Construct the appropriate data based on game type
            string postData = selectedGameType == "count"
                ? $"{{\"uid\":\"{uid}\", \"gameType\":\"count\", \"count\":{selectedCount}, \"status\":\"{status}\"}}"
                : $"{{\"uid\":\"{uid}\", \"gameType\":\"time\", \"time\":{selectedTime * 60}, \"status\":\"{status}\"}}";

            // Update wristband transaction in the database
            string result = readerWriter.updateStatus(uid, status);
            if (string.IsNullOrEmpty(result))
            {
                StatusTextBlock.Text = "Wristband transaction updated successfully.";
            }
            else
            {
                StatusTextBlock.Text = $"Error updating wristband: {result}";
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
