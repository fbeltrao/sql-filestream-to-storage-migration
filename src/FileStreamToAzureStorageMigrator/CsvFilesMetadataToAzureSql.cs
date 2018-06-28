using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace FileStreamToAzureStorageMigrator
{
    public class CsvFilesMetadataToAzureSql
    {
        private readonly string _filesStreamInfoCsvFile;
        private readonly SqlConnection _sqlConnection;

        public CsvFilesMetadataToAzureSql(string connectionString, string filesStreamInfoCsvFile)
        {
            _filesStreamInfoCsvFile = filesStreamInfoCsvFile;

            _sqlConnection = new SqlConnection(connectionString);
            _sqlConnection.Open();
        }

        public async Task CopyDataAsync()
        {
            var files = this.ReadFromFile();

            for (int i = 0; i < files.Count; i++)
            {
                Console.WriteLine($"Updating {i} from {files.Count}");

                try
                {
                    await InsertData(files[i], _sqlConnection);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private IList<FileStreamFile> ReadFromFile()
        {
            using (TextReader reader = File.OpenText(_filesStreamInfoCsvFile))
            {
                var csv = new CsvReader(reader);
                IList<FileStreamFile> records;
                try
                {
                    records = csv.GetRecords<FileStreamFile>().ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                return records;
            }
        }

        private async Task InsertData(FileStreamFile fsf, SqlConnection conn)
        {
            string sqlCommand = @"INSERT INTO [Model].[DocumentMeta]
                                               ([DocumentId]
                                               ,[DocumentType]
                                               ,[Container]
                                               ,[Name]
                                               ,[FileType]
                                               ,[ContentType]
                                               ,[AzureBlobPath])
                                         VALUES
                                               (@DocumentId
                                               ,@DocumentType
                                               ,@Container
                                               ,@Name
                                               ,@FileType
                                               ,@ContentType
                                               ,@AzureBlobPath)";

            var cmd = new SqlCommand(sqlCommand, conn);

            cmd.Parameters.Add(new SqlParameter("@DocumentId", fsf.DocumentId));
            cmd.Parameters.Add(new SqlParameter("@DocumentType", fsf.DocumentType));
            cmd.Parameters.Add(new SqlParameter("@Container", fsf.Container));
            cmd.Parameters.Add(new SqlParameter("@Name", fsf.Name));
            cmd.Parameters.Add(new SqlParameter("@FileType", fsf.FileType));
            cmd.Parameters.Add(new SqlParameter("@ContentType", fsf.ContentType));
            cmd.Parameters.Add(new SqlParameter("@AzureBlobPath", fsf.BlobLocation));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
