using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ColumnEncryption.Functions
{
    public static class CopyToSql
    {
        static string connectionString = System.Environment.GetEnvironmentVariable("SQL_CONNECTION");

        [Function("CopyToSql")]
        public static void Run([BlobTrigger("userdata/{csvName}", Connection = "AzureWebJobsStorage")]string csvData, string csvName, FunctionContext context)
        {
            context.Items.FirstOrDefault();
            var logger = context.GetLogger("CopyToSql");
            logger.LogInformation($"CopyToSql Function processing blob\n name:{csvName} \n Size: {csvData.Length} Bytes");
            var connString = System.Environment.GetEnvironmentVariable("SQL_CONNECTION");
            var records = csvData.Split("\r\n");
            DataTable dt = GetRecords(records);
            bool success = InsertUserData(dt);

            if(success)
                logger.LogInformation($"Data from {csvName} inserted to SQL database");
        }

        static bool InsertUserData(DataTable records)
        {
            bool success = true;

            using (var connection = new SqlConnection(connectionString))
            {
                using (var bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.AllowEncryptedValueModifications))
                {

                    string[] dbColumns = {"FirstName", "LastName", "SSN", "Email", "CreditRating", "Gender" };

                    foreach (var column in records.Columns)
                    {
                        if(dbColumns.Any(column.ToString().Contains))
                            bulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());

                    }

                    bulkCopy.DestinationTableName = "userdata";

                    try
                    {
                        connection.Open();
                        bulkCopy.WriteToServer(records);
                    }
                    catch (SqlException ex)
                    {
                        success = false;
                        Console.WriteLine(ex.Message);
                    } 
                }
            }

            return success;
        }

        private static DataTable GetRecords(string[] data)
        {
            int numRecords = data.Length - 2;

            // Get headers
            var headers = data[0].Split(',');

            DataTable dt = new DataTable();

            foreach(string header in headers)
            {
                // only picking out a subset of all possible columns
                switch(header)
                {
                    case "ID":
                        dt.Columns.Add(header, typeof(int));
                        break;                    
                    case "SSN":
                        dt.Columns.Add(header, typeof(Byte[]));
                        break;
                    case "CreditRating":
                        dt.Columns.Add(header, typeof(int));
                        break;
                    case "BirthDate":
                        dt.Columns.Add(header, typeof(DateTime));
                        break;
                    default:
                        dt.Columns.Add(header, typeof(string));
                        break;
                }
            }

            // Add rows
            for (int i = 0; i < numRecords; i++) { dt.Rows.Add(); }

            // Populate data
            int headerIndex = 0;
            foreach (var header in headers)
            {
                var name = header;

                for (int rowIndex = 1; rowIndex < data.Length - 1; rowIndex++)
                {
                    var record = data[rowIndex].Split(',');

                    switch (name)
                    {
                        case "ID":
                            dt.Rows[rowIndex-1].SetField(name, Convert.ToInt32(record[headerIndex]));
                            break;                        
                        case "SSN":
                            Byte[] ssnByte = ColumnEncrypt.Util.Converter.FromHexString(record[headerIndex]);
                            dt.Rows[rowIndex-1].SetField(name, ssnByte);
                            break; 
                        case "BirthDate":
                            dt.Rows[rowIndex-1].SetField(name, Convert.ToDateTime(record[headerIndex]));
                            break;
                        case "CreditRating":
                            dt.Rows[rowIndex-1].SetField(name, Convert.ToInt32(record[headerIndex]));
                            break;
                        default:
                            dt.Rows[rowIndex-1].SetField(name, record[headerIndex]);
                            break;
                    }
                }

                headerIndex++;
            }

            dt.AcceptChanges();
            return dt;
        }
    }

}
