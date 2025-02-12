using System;
using System.Configuration;
using System.Threading;
using System.Windows.Forms;
using Lib;

namespace POS
{
    public partial class Form1 : Form
    {
        private readonly BaseScanner _readerWriter;

        private string _selectedGameType = string.Empty;
        private int _selectedCount;
        private double _selectedTime; // Time in hours

        public Form1()
        {
            InitializeComponent();

            _readerWriter = new NFCReaderWriter("I", ConfigurationManager.AppSettings["server"]);
            if (!_readerWriter.isScannerActive)
            {
                _readerWriter = new Lib.HandScanner("I", ConfigurationSettings.AppSettings["server"], ConfigurationSettings.AppSettings["HandScannerComPort"]);
                Thread.Sleep(1000);
            }
            _readerWriter.StatusChanged += ReaderWriter_StatusChanged;
        }


        private void cmbNoOfGames_SelectedValueChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbNoOfGames.Text, out int count))
            {
                _selectedCount = count;
                logger.Log($"Number of games selected: {_selectedCount}");
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
            logger.Log($"Status updated: {text}");
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
            logger.Log($"Selection reset to: {text}");
        }

        //private void ReaderWriter_StatusChanged(object sender, string resultp)
        //{
        //    string uid = resultp.Split(':')[0];
        //    string resultreader = resultp.Split(':')[1];

        //    logger.Log($"Wristband scanned: {uid}");
        //    if (_selectedCount == 0 && _selectedTime == 0)
        //    {
        //        MessageBox.Show("Please select no of games");

        //        setStatus("Please select number of games! ");
        //        return;
        //    }
        //    if (uid.Length == 0)
        //    {
        //        setStatus(resultreader);
        //        MessageBox.Show($"{resultreader}");
        //        return;

        //    }
        //    // setStatus($"Wristband {uid} scanned, updating database...");


        //    logger.Log($"Attempting to insert record with count: {_selectedCount}, time: {(_selectedCount > 0 ? 120.0 : _selectedTime)}");

        //    string result = _readerWriter.InsertRecord(uid, _selectedCount, _selectedCount > 0 ? 120.0 : _selectedTime);

        //    if (string.IsNullOrEmpty(result))
        //    {
        //        setStatus("Wristband is good to go");
        //        _selectedCount = 0;
        //        resetSelection(string.Empty);
        //        logger.Log("Wristband successfully processed.");

        //    }
        //    else
        //    {
        //        if(result== "Wristband still has time and count.")
        //        {

        //        }
        //        logger.Log($"Insert record failed with result: {result}");
        //        setStatus(result);
        //    }

        //}

        private void ReaderWriter_StatusChanged(object sender, string resultp)
        {
            string uid = resultp.Split(':')[0];
            string resultreader = resultp.Split(':')[1];

            logger.Log($"Wristband scanned: {uid}");

            if (_selectedCount == 0 && _selectedTime == 0)
            {
                MessageBox.Show("Please select no of games");

                setStatus("Please select number of games!");
                return;
            }

            if (string.IsNullOrEmpty(uid))
            {
                setStatus(resultreader);
                MessageBox.Show(resultreader);
                return;
            }

            logger.Log($"Attempting to insert record with count: {_selectedCount}, time: {(_selectedCount > 0 ? 120.0 : _selectedTime)}");

            string result = _readerWriter.InsertRecord(uid, _selectedCount, _selectedCount > 0 ? 120.0 : _selectedTime);

            if (string.IsNullOrEmpty(result))
            {
                _readerWriter.updateStatus(uid, "R", 10);
                setStatus("Wristband is good to go");
                _selectedCount = 0;
                resetSelection(string.Empty);
                logger.Log("Wristband successfully processed.");
            }
            else
            {
                if (result == "Error: BadRequest - Wristband still has time and count.")
                {
                    // Show a dialog with Reset and OK buttons
                    DialogResult dialogResult = MessageBox.Show("Wristband still has time and count. Do you want to reset it and reinitiallize?", "Wristband Status", MessageBoxButtons.YesNo);

                    if (dialogResult == DialogResult.Yes) // Reset button pressed
                    {
                        result = _readerWriter.InvalidateStatus(uid);
                        logger.Log($"Wristband status reset result: {result}");
                        result = _readerWriter.InsertRecord(uid, _selectedCount, _selectedCount > 0 ? 120.0 : _selectedTime);
                        _readerWriter.updateStatus(uid, "R", 10);
                        setStatus("Wristband is good to go");
                        _selectedCount = 0;
                        resetSelection(string.Empty);
                        logger.Log("Wristband successfully processed.");
                    }
                    else if (dialogResult == DialogResult.No) // OK button pressed
                    {
                        // Just close the dialog, no action needed
                        logger.Log("User chose to keep current wristband status.");
                    }
                }
                else
                {
                    logger.Log($"Insert record failed with result: {result}");
                    setStatus(result);
                }
            }
        }
    }
}
