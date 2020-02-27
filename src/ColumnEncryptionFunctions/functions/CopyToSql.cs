using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ColumnEncryption.Data;
using Microsoft.ColumnEncryption.DataProviders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace ColumnEncryption.Functions
{
    public static class CopyToSql
    {
        static string connectionString = System.Environment.GetEnvironmentVariable("SQL_CONNECTION");

        [FunctionName("CopyToSql")]
        public static async Task Run([BlobTrigger("csvprotected/{csvName}", Connection = "AzureWebJobsStorage")]Stream csvFile, string csvName, ILogger log)
        {
            log.LogInformation($"CopyToSql Function processing blob\n name:{csvName} \n Size: {csvFile.Length} Bytes");
            var connString = System.Environment.GetEnvironmentVariable("SQL_CONNECTION");
            CSVDataReader csvDataReader = new CSVDataReader(new StreamReader(csvFile));
            var columns = csvDataReader.Read();
           
            DataTable dt = GetRecordsDt(columns);
            bool success = InsertPatients(dt);
  
            /*
            Patient[] patients = GetRecords(columns);
            foreach(Patient newPatient in patients)
            {
                InsertPatient(newPatient);
            }  
            */       
            
            if(success)
            {
                // cleanup
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("csvprotected");
                var blockBlob = container.GetBlockBlobReference($"{csvName}");
                bool deleted = await blockBlob.DeleteIfExistsAsync();
                if (deleted) { log.LogInformation($"Deleted source file: {csvName}"); }
            }
        }

        static bool InsertPatients(DataTable patients)
        {
            bool success = true;

            using (var connection = new SqlConnection(connectionString))
            {
                using (var bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.AllowEncryptedValueModifications))
                {
                    foreach (var column in patients.Columns)
                        bulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());

                    bulkCopy.DestinationTableName = "Patients";

                    try
                    {
                        connection.Open();
                        bulkCopy.WriteToServer(patients);
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

        static int InsertPatient(Patient newPatient)
        {
            int returnValue = 0;

            string sqlCmdText = @"INSERT INTO [dbo].[Patients] ([SSN], [FirstName], [LastName], [BirthDate]) VALUES (@SSN, @FirstName, @LastName, @BirthDate);";

            SqlCommand sqlCmd = new SqlCommand(sqlCmdText);

            byte[] encryptedssn = Encoding.ASCII.GetBytes(newPatient.SSN.ToString());
            SqlParameter paramSSN = new SqlParameter(@"@SSN", SqlDbType.VarBinary, encryptedssn.Length)
            {
                Direction = ParameterDirection.Input,
                Value = encryptedssn
            };

            SqlParameter paramFirstName = new SqlParameter(@"@FirstName", newPatient.FirstName);
            paramFirstName.DbType = DbType.String;
            paramFirstName.Direction = ParameterDirection.Input;

            SqlParameter paramLastName = new SqlParameter(@"@LastName", newPatient.LastName);
            paramLastName.DbType = DbType.String;
            paramLastName.Direction = ParameterDirection.Input;

            SqlParameter paramBirthDate = new SqlParameter(@"@BirthDate", newPatient.BirthDate);
            paramBirthDate.SqlDbType = SqlDbType.Date;
            paramBirthDate.Direction = ParameterDirection.Input;

            sqlCmd.Parameters.Add(paramSSN);
            sqlCmd.Parameters.Add(paramFirstName);
            sqlCmd.Parameters.Add(paramLastName);
            sqlCmd.Parameters.Add(paramBirthDate);

            using (sqlCmd.Connection = new SqlConnection(connectionString))
            {
                try
                {
                    sqlCmd.Connection.Open();
                    sqlCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    returnValue = 1;
                    Console.WriteLine("The following error was encountered: ");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(Environment.NewLine + "Press Enter key to exit");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
            return returnValue;
        }

        private static Patient[] GetRecords(IEnumerable<ColumnData> columns)
        {
            int numRecords = 0;

            // TODO: This is really stupid but I'm out of patience with parseing all this nonesense just to get a count of records
            foreach (var column in columns)
            {
                numRecords = column.Data.Count;
                break;
            }

            Patient[] patientArr = new Patient[numRecords].Select(x => new Patient()).ToArray();

            foreach (var column in columns)
            {
                var name = column.Name;
                int i = 0;

                foreach(var value in column.Data)
                {
                    switch (name)
                    {
                        case "SSN":
                            patientArr[i].SSN = value.ToString();
                            i++;
                            break;
                        case "FirstName":
                            patientArr[i].FirstName = value.ToString();
                            i++;
                            break;
                        case "LastName":
                            patientArr[i].LastName = value.ToString();
                            i++;
                            break;
                        case "BirthDate":
                            patientArr[i].BirthDate = Convert.ToDateTime(value);
                            i++;
                            break;
                    }
                }
            }

            return patientArr;
        }

        private static DataTable GetRecordsDt(IEnumerable<ColumnData> columns)
        {
            int numRecords = 0;

            // TODO: This is really stupid but I'm out of patience with parseing all this nonesense just to get a count of records
            foreach (var column in columns)
            {
                numRecords = column.Data.Count;
                break;
            }

            DataTable dt = new DataTable();

            /*
            dt.Columns.Add(new DataColumn
            {
                ColumnName = "PatientId",
                DataType = typeof(int),
                AutoIncrement = true,
                AutoIncrementSeed = 10
            });
            */

            // set up columns for the dataTable
            foreach(var column in columns)
            {
                switch(column.Name)
                {
                    case "BirthDate":
                        dt.Columns.Add(column.Name, typeof(System.DateTime));
                        break;
                    
                    case "SSN":
                        dt.Columns.Add(column.Name, typeof(Byte[]));
                        break;
                    
                    default:
                        dt.Columns.Add(column.Name, typeof(string));
                        break;
                }
            }

            // Add rows
            for (int i = 0; i < numRecords; i++) { dt.Rows.Add(); }

            // Populate data
            foreach (var column in columns)
            {
                var name = column.Name;
                int i = 0;

                foreach(var value in column.Data)
                {
                    switch (name)
                    {
                        case "SSN":
                            Byte[] ssnByte = Microsoft.ColumnEncryption.Common.Converter.FromHexString(value.ToString());
                            dt.Rows[i].SetField(name, ssnByte);
                            i++;
                            break; 
                        
                        case "BirthDate":
                            dt.Rows[i].SetField(name, Convert.ToDateTime(value));
                            i++;
                            break; 
                        default:
                            dt.Rows[i].SetField(name, value.ToString());
                            i++;
                            break;
                    } 
                }
            }

            dt.AcceptChanges();

            string data = string.Empty;
            StringBuilder sb = new StringBuilder();

            foreach(DataRow row in dt.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    sb.Append(item);
                    sb.Append(',');
                }
                sb.AppendLine();
            }

            data = sb.ToString();
            Console.WriteLine(data);

            return dt;
        }

    }

    public class Patient
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SSN { get; set; }
        public DateTime BirthDate { get; set; }
    }
}
