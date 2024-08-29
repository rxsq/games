using System;
using System.Configuration;
using System.Windows.Forms;
using Lib;

namespace POS
{
    public partial class Form1 : Form
    {
        private readonly NFCReaderWriter _readerWriter;
        private readonly AsyncLogger _logger;
        private string _selectedGameType = string.Empty;
        private int _selectedCount;
        private double _selectedTime; // Time in hours

        public Form1()
        {
            InitializeComponent();
            _logger = new AsyncLogger("pos");
            _readerWriter = new NFCReaderWriter(
                "I",
                ConfigurationManager.AppSettings["server"],
                _logger
            );
            _readerWriter.StatusChanged += ReaderWriter_StatusChanged;
        }

        
        private void cmbNoOfGames_SelectedValueChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbNoOfGames.Text, out int count))
            {
                _selectedCount = count;
                _logger.Log($"Number of games selected: {_selectedCount}");
            }
            
               
            
        }

        private void setStatus(string text)
        {
            if (label2.InvokeRequired)
            {
                label2.Invoke(new Action<string>(setStatus), text);
            }
            else
            {
                label2.Text = text;
            }
            _logger.Log($"Status updated: {text}");
        }

        private void resetSelection(string text)
        {
            if (cmbNoOfGames.InvokeRequired)
            {
                cmbNoOfGames.Invoke(new Action<string>(resetSelection), text);
            }
            else
            {
                cmbNoOfGames.Text = text;
            }
            _logger.Log($"Selection reset to: {text}");
        }

        private void ReaderWriter_StatusChanged(object sender, string resultp)
        {
            string uid= resultp.Split(':')[0];
            string resultreader = resultp.Split(':')[1];
           
                _logger.Log($"Wristband scanned: {uid}");
                if (_selectedCount == 0 && _selectedTime == 0)
                {
                    MessageBox.Show("Please select no of games");

                    setStatus("Select Time or number of games then try. Please retry");
                    return;
                }
                    if (uid.Length == 0)
                {
                    setStatus(resultreader);
                    MessageBox.Show($"{resultreader}");
                    return;

                }
                // setStatus($"Wristband {uid} scanned, updating database...");

              
                    _logger.Log($"Attempting to insert record with count: {_selectedCount}, time: {(_selectedCount > 0 ? 120.0 : _selectedTime)}");

                    string result = _readerWriter.InsertRecord(uid, _selectedCount, _selectedCount > 0 ? 120.0 : _selectedTime);

                    if (string.IsNullOrEmpty(result))
                    {
                        setStatus("Wristband is good to go");
                _selectedCount = 0;
                        resetSelection(string.Empty);
                        _logger.Log("Wristband successfully processed.");
                       
                    }
                    else
                    {
                       
                        _logger.Log($"Insert record failed with result: {result}");
                        setStatus(result);
                    }
           
        }
    }
}
