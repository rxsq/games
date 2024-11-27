using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CsvHelper;
using System.Collections.Generic;
using System.Data.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace importdata
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string connectionString = "Server=192.186.105.194;Initial Catalog=analysis;User Id=admin;Password=Aero@password1;TrustServerCertificate=True";

        private void btnSelectFiles_Click_1(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    lstSelectedFiles.Items.Clear();
                    foreach (string file in openFileDialog.FileNames)
                    {
                        lstSelectedFiles.Items.Add(file);
                    }
                }
            }
        }

        private void btnImport_Click_1(object sender, EventArgs e)
        {
            if (cmbLocation.Text == "")
            {
                MessageBox.Show("Please select a location.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (cmbImporttype.Text ==   "")
            {
                MessageBox.Show("Please select a import type.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (lstSelectedFiles.Items.Count == 0)
            {
                MessageBox.Show("Please select files and provide a valid connection string.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            importData();


        }
        private void importData()
        {
            foreach (string csvFilePath in lstSelectedFiles.Items)
            {
                if (cmbImporttype.Text == "bookings")
                {
                    importBookings(csvFilePath);
                }
                else
                {
                    ImportDataWithTableCheck(csvFilePath,cmbImporttype.Text);
                }
            }
        }

       
        private void ImportDataWithTableCheck(string csvFilePath, string tableName)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                connection.Open();

                // Check if the table exists, if not, create it
                if (!TableExists(connection, tableName))
                {
                    csv.Read();
                    csv.ReadHeader();
                    CreateTable(connection, tableName, csv);
                }

                // Import data into the table
                ImportTable(csvFilePath, tableName, connection);
            }
        }
        private void CreateTable(SqlConnection connection, string tableName, CsvReader csv)
        {
            string createTableQuery = GenerateCreateTableQuery(tableName, csv);

            using (SqlCommand cmd = new SqlCommand(createTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private string GenerateCreateTableQuery(string tableName, CsvReader csv)
        {
            List<string> columns = csv.HeaderRecord.ToList();
          columns.Add("location");
            var sqlColumns = columns.Select(column => $"[{column.Replace(" ", "")}] NVARCHAR(MAX)").ToArray(); // Defaulting to NVARCHAR(MAX)
            
            string createTableQuery = $@"
        CREATE TABLE [{tableName}] (
            {string.Join(", ", sqlColumns)}
        )";

            return createTableQuery;
        }

        private bool TableExists(SqlConnection connection, string tableName)
        {
            string query = @"IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName) 
                     SELECT 1 ELSE SELECT 0";
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@tableName", tableName);
                return (int)cmd.ExecuteScalar() == 1;
            }
        }
        private void ImportTable(string csvFilePath, string tableName, SqlConnection connection)
        {
            try
            {
                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();

                    // Create a DataTable with columns derived from CSV headers
                    DataTable dataTable = new DataTable();
                    var headerMapping = csv.HeaderRecord.ToDictionary(
                        header => header.Replace(" ", ""),
                        header => header
                    );

                    foreach (var header in headerMapping.Keys)
                    {
                        dataTable.Columns.Add(new DataColumn(header));
                    }
                    dataTable.Columns.Add(new DataColumn("location"));
                    // Read data from CSV and populate DataTable
                    while (csv.Read())
                    {
                        DataRow row = dataTable.NewRow();
                        foreach (var header in headerMapping.Keys)
                        {
                            row[header] = csv.GetField(headerMapping[header]);
                        }
                        row["location"] = cmbLocation.Text;
                        dataTable.Rows.Add(row);
                    }

                    // Bulk insert into SQL Server
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName =$"dbo.{tableName}" ;

                        foreach (DataColumn column in dataTable.Columns)
                        {
                            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                        }

                        bulkCopy.WriteToServer(dataTable);
                    }

                    MessageBox.Show($"Data from {Path.GetFileName(csvFilePath)} has been successfully imported.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while importing {Path.GetFileName(csvFilePath)}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void importBookings(string csvFilePath)
        {
           
                try
                {
                    using (var reader = new StreamReader(csvFilePath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        csv.Read();
                        csv.ReadHeader();

                        var headerMapping = csv.HeaderRecord.ToDictionary(
                            header => header.Replace(" ", "").ToLowerInvariant(),
                            header => header);

                        var sqlColumns = new[]
                        {
                            "Comments", "POSNotes", "Items", "Device", "Product", "Locations", "BookingName",
                            "BookingDate", "TransactionDate", "Status", "FirstName", "ContactName",
                            "LastName", "SessionStartTime", "SessionEndTime", "Cost", "Balance",
                            "CustomerId", "Guests", "ContactNumber", "Email", "PurchaseLocation",
                            "CreatedDate", "Company", "BookingID","location"
                        };

                        // Get column lengths once and store them in a dictionary
                        var columnLengths = GetColumnMaxLengths(connection, "EventBookings", sqlColumns);

                        DataTable eventBookingsTable = new DataTable();
                        foreach (var column in sqlColumns)
                        {
                            if (headerMapping.ContainsKey(column.ToLowerInvariant()))
                            {
                                eventBookingsTable.Columns.Add(new DataColumn(column));
                            }
                        }
                        eventBookingsTable.Columns.Add("location", typeof(string));
                        DataTable productsTable = new DataTable();
                        productsTable.Columns.Add("BookingID", typeof(string));
                        productsTable.Columns.Add("Product", typeof(string));

                        while (csv.Read())
                        {
                            DataRow eventRow = eventBookingsTable.NewRow();
                            foreach (var column in sqlColumns)
                            {
                                string normalizedColumn = column.Replace(" ", "").ToLowerInvariant();
                                if (headerMapping.ContainsKey(normalizedColumn))
                                {
                                    string value = csv.GetField(headerMapping[normalizedColumn]);
                                    int maxLength = columnLengths.ContainsKey(column) ? columnLengths[column] : int.MaxValue;

                                    if (maxLength > 0 && value.Length > maxLength)
                                    {
                                        value = value.Substring(0, maxLength);
                                    }
                                    eventRow[column] = value;
                                }
                            }
                            eventRow["location"] = cmbLocation.Text;
                            eventBookingsTable.Rows.Add(eventRow);

                            string bookingID = csv.GetField(headerMapping["bookingid"]);
                            string items = csv.GetField(headerMapping["items"]);

                            if (!string.IsNullOrWhiteSpace(items))
                            {
                                var products = items.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var product in products)
                                {
                                    DataRow productRow = productsTable.NewRow();
                                    productRow["BookingID"] = bookingID;
                                    productRow["Product"] = product.Trim();
                                    productsTable.Rows.Add(productRow);
                                }
                            }
                        }

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                        {
                            bulkCopy.DestinationTableName = "EventBookings";

                            foreach (DataColumn column in eventBookingsTable.Columns)
                            {
                                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                            }

                            bulkCopy.WriteToServer(eventBookingsTable);
                        }

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                        {
                            bulkCopy.DestinationTableName = "Products";
                            bulkCopy.ColumnMappings.Add("BookingID", "BookingID");
                            bulkCopy.ColumnMappings.Add("Product", "Product");

                            bulkCopy.WriteToServer(productsTable);
                        }
                    }

                    MessageBox.Show($"Data from {Path.GetFileName(csvFilePath)} has been successfully imported.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while importing {Path.GetFileName(csvFilePath)}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            
        }
        private Dictionary<string, int> GetColumnMaxLengths(SqlConnection connection, string tableName, string[] columns)
        {
            var lengths = new Dictionary<string, int>();
            string query = @"
                SELECT COLUMN_NAME, CHARACTER_MAXIMUM_LENGTH
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName AND COLUMN_NAME IN ({0})";

            string columnList = string.Join(", ", columns.Select(c => $"'{c}'"));
            query = string.Format(query, columnList);

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string columnName = reader["COLUMN_NAME"].ToString();
                        int maxLength = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value
                            ? Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"])
                            : int.MaxValue;

                        lengths[columnName] = maxLength > 0 ? maxLength : int.MaxValue;
                    }
                }
            }

            return lengths;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
