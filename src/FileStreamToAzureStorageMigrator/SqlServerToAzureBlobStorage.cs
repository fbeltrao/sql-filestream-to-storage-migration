using CsvHelper;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileStreamToAzureStorageMigrator
{
    /// <summary>
    /// Gets all files stream contents and metadata from SQL Server database and saves it to Azure Blob Storage and local CSV (metadata) 
    /// </summary>
    public class SqlServerToAzureBlobStorage
    {

        /// <summary>
        /// Source SQL Server database connection string
        /// </summary>
        private readonly string _sourceSqlServerDatabaseConnectionString;

        /// <summary>
        /// Destination Azure (Blob) Storage 
        /// </summary>
        private readonly string _destinationAzureBlobStorageConnectionString;

        /// <summary>
        /// The CSV filename containing the metadata of the SQL FileStream files
        /// </summary>
        private readonly string _filesStreamInfoCsvFile;


        /// <summary>
        /// SQL Query that returns the file stream (actual content) from SQL Server
        /// You can also adjust it to your needs
        /// </summary>
        internal readonly string _selectDocumentsCommand = $@"SELECT [file_stream] FROM [DocumentsTable] WHERE [documentId] = @Id";

        /// <summary>
        /// SQL Query that returns a list of all files to be fectched from SQL Server
        /// You can also adjust it to your needs
        /// </summary>
        internal readonly string _selectFileDocumentsCommand = $@"SELECT DocumentName, documentId, contentType, file_type FROM [DocumentsTable]";


        public SqlServerToAzureBlobStorage(
            string sourceSqlServerDatabaseConnectionString,
            string destinationAzureBlobStorageConnectionString,
            string filesStreamInfoCsvFile)
        {
            _sourceSqlServerDatabaseConnectionString = sourceSqlServerDatabaseConnectionString;
            _destinationAzureBlobStorageConnectionString = destinationAzureBlobStorageConnectionString;
            _filesStreamInfoCsvFile = filesStreamInfoCsvFile;
        }

        public async Task ExtractDataFromDbAsync()
        {
            var fileStreamFiles = new List<FileStreamFile>();

            using (var conn = new SqlConnection(_sourceSqlServerDatabaseConnectionString))
            {
                // List all files to be downloaded
                SqlCommand cmd = new SqlCommand(_selectFileDocumentsCommand, conn);

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // You'll need to adjust the nr. of fields here
                        var fsf = new FileStreamFile(
                            reader.GetValue(0).ToString(),
                            reader.GetValue(1).ToString(),
                            reader.GetValue(2).ToString(),
                            reader.GetValue(3).ToString(),
                            reader.GetValue(4).ToString(),
                            reader.GetValue(5).ToString(),
                            reader.GetValue(6).ToString());

                        fileStreamFiles.Add(fsf);
                    }
                }

                // Foreach file, download content and copy to azure storage
                cmd.CommandText = _selectDocumentsCommand;
                cmd.Parameters.Add(new SqlParameter("@Id", string.Empty));
                foreach (var fsf in fileStreamFiles)
                {
                    cmd.Parameters[0].Value = fsf.StreamId;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        var fileBytes = (byte[])reader[0];
                        await CopyFileStreamFile(fsf, fileBytes);
                    }
                }

                ExportToCsvFile(fileStreamFiles);
            }
        }

        /// <summary>
        /// Copy file bytes to Azure Blob
        /// </summary>
        /// <param name="fsf"></param>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
        private async Task CopyFileStreamFile(FileStreamFile fsf, byte[] fileBytes)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var cloudStorageAccount = CloudStorageAccount.Parse(_destinationAzureBlobStorageConnectionString);

                var blobClient = cloudStorageAccount.CreateCloudBlobClient();

                // Ensure container exits
                var container = blobClient.GetContainerReference(fsf.Container);
                await container.CreateIfNotExistsAsync();

                // open stream writer to file
                var blobFileName = Path.Combine(fsf.Folder, fsf.Name);
                var blob = container.GetBlockBlobReference(blobFileName);
                if (await blob.ExistsAsync())
                {
                    return;
                }

                await blob.UploadFromByteArrayAsync(fileBytes, 0, fileBytes.Length);

                fsf.BlobLocation = blob.StorageUri.PrimaryUri.AbsolutePath;

                stopwatch.Stop();

                Console.WriteLine($"File {fsf.Name} saved to container {fsf.Folder}, {fileBytes.Length} bytes, in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with {fsf.Name}: {ex}");
                using (var writer = new StreamWriter($"{fsf.DocumentId}.csv"))
                {
                    var csv = new CsvWriter(writer);
                    csv.WriteRecord(fsf);
                }
            }
        }


        /// <summary>
        /// Save files stream info to CSV
        /// </summary>
        /// <param name="fsf"></param>
        private void ExportToCsvFile(IList<FileStreamFile> fsf)
        {
            using (var writer = new StreamWriter($@"{_filesStreamInfoCsvFile}"))
            {
                var csv = new CsvWriter(writer);
                csv.WriteRecords(fsf);
            }
        }
    }
}
