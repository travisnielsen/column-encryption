using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ColumnEncryption;
using Microsoft.ColumnEncryption.Auth;
using Microsoft.ColumnEncryption.Config;
using Microsoft.ColumnEncryption.Data;
using Microsoft.ColumnEncryption.DataProviders;
using Microsoft.ColumnEncryption.Encoders;
using Microsoft.ColumnEncryption.EncryptionProviders;
using Microsoft.ColumnEncryption.Metadata;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ColumnEncryption.Functions
{
    public static class CopyToSql
    {
        static string connectionString = System.Environment.GetEnvironmentVariable("SQL_CONNECTION");

        [FunctionName("CopyToSql")]
        public static void Run([BlobTrigger("csvprotected/{csvName}", Connection = "AzureWebJobsStorage")]Stream csvFile, string csvName, ILogger log)
        {
            log.LogInformation($"CopyToSql Function processing blob\n name:{csvName} \n Size: {csvFile.Length} Bytes");
            var connString = System.Environment.GetEnvironmentVariable("SQL_CONNECTION");
            CSVDataReader csvDataReader = new CSVDataReader(new StreamReader(csvFile));
            var columns = csvDataReader.Read();
           
            Patient[] patients = GetRecords(columns);

            foreach(Patient newPatient in patients)
            {
                InsertPatient(newPatient);
            } 
        }

        static int InsertPatient(Patient newPatient)
        {
            int returnValue = 0;

            string sqlCmdText = @"INSERT INTO [dbo].[Patients] ([SSN], [FirstName], [LastName], [BirthDate]) VALUES (@SSN, @FirstName, @LastName, @BirthDate);";

            SqlCommand sqlCmd = new SqlCommand(sqlCmdText);

            SqlParameter paramSSN = new SqlParameter(@"@SSN", newPatient.SSN);
            // paramSSN.DbType = DbType.AnsiStringFixedLength;
            paramSSN.Direction = ParameterDirection.Input;
            // paramSSN.Size = 11;

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

                foreach(var value in column.Data)
                {
                    int i = 0;
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
   
    }

    public class Patient
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SSN { get; set; }
        public DateTime BirthDate { get; set; }
    }
}
