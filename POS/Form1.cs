using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Lib;
using static System.Net.Mime.MediaTypeNames;
namespace POS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            readerWriter = new NFCReaderWriter("I", ConfigurationManager.AppSettings["server"], 0, 0);
            readerWriter.StatusChanged += ReaderWriter_StatusChanged;
        }

        private void cmbNoOfGames_SelectedValueChanged(object sender, EventArgs e)
        {
            selectedCount = int.Parse(cmbNoOfGames.Text.ToString());
        }

        private readonly NFCReaderWriter readerWriter;
        private string selectedGameType = "";
        private int selectedCount = 0;
        private double selectedTime = 0; // Time in hours

     

      
       private void setStatus(string text)
        {
            if (label2.InvokeRequired)
            {
                // Invoke on the UI thread
                label2.Invoke(new Action<string>(setStatus), text);
          
            }
            else
            {
                // Update the control
                label2.Text = text;
            }
        }
        private void resetSelection(string text)
        {
            if (cmbNoOfGames.InvokeRequired)
            {
                // Invoke on the UI thread
                cmbNoOfGames.Invoke(new Action<string>(resetSelection), text);

            }
            else
            {
                // Update the control
                cmbNoOfGames.Text = text;
            }
        }
        private void ReaderWriter_StatusChanged(object sender, string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                //   Dispatcher.Invoke(() =>
                //  {
                //label2.Text = $"Wristband {uid} scanned, updating database...";
                setStatus($"Wristband {uid} scanned, updating database...");
                if (selectedCount > 0 || selectedTime > 0)
                    {
                    string result =  readerWriter.InsertRecord(uid, selectedCount, selectedTime > 0 ? 120.0 : selectedTime);

                    if (result.Length == 0)
                        {
                            setStatus("Wristband is good to go");
                            resetSelection("");
                        }
                        else
                        {
                           setStatus( result);
                        }

                    }
                    else
                    {
                        setStatus("Select Time or number of games then try. Please retry");
                    }
              //  });
            }
            //else
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        label2.Text = "Error: Wristband not recognized.";
            //    });
            //}
        }

    }
}
