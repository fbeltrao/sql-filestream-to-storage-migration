using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace FileStreamToAzureStorageMigrator
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }
       
        static async Task Main(string[] args)
        {
            InitializeConfiguration();

            string sourceSqlServerDatabaseConnectionString = Configuration["source:sqlServerDatabaseConnectionString"];
            string destinationAzureSqlDatabaseConnectionString = Configuration["destination:azureSqlDatabaseConnectionString"];
            string destinationAzureBlobStorageConnectionString = Configuration["destination:azureBlobStorageConnectionString"];
            string filesStreamInfoCsvFile = Configuration["source:filesStreamInfoCsvFile"];

            // Step 1: copy the file contents to Blob Storage
            var sqlServerToAzureBlobStorage = new SqlServerToAzureBlobStorage(sourceSqlServerDatabaseConnectionString, destinationAzureBlobStorageConnectionString, filesStreamInfoCsvFile);
            await sqlServerToAzureBlobStorage.CopyDataAsync();

            // Step 2: copy the files metadata (table) to Azure SQL
            var filesMetadataToAzureSql = new CsvFilesMetadataToAzureSql(destinationAzureSqlDatabaseConnectionString, filesStreamInfoCsvFile);
            await filesMetadataToAzureSql.CopyDataAsync();
        }


        /// <summary>
        /// Configuration initialization
        /// </summary>
        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }


    }
}
