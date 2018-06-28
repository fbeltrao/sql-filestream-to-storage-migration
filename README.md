# SQL Server FileStream to Azure Storage
A simple .NET Core 2.1 tool to help you migrate your [SQL Server FileStream](https://docs.microsoft.com/en-us/sql/relational-databases/blob/filestream-sql-server?view=sql-server-2017) contents to an Azure SQL database and Azure Blob Storage.

![](./images/sql-filestream-to-storage-migration.png "SQL Server FileStream to Azure Storage")

## Getting started
- Visual Studio 2017, .NET Core 2.1
- Azure SQL Database
- Azure Blob Storage account
- Replace the necessary database and storage connection strings in [appsettings.json](src/FileStreamToAzureStorageMigrator/appsettings.json)

```json
{
  "source": {
    "sqlServerDatabaseConnectionString": "<-- replace with SQL Server connection string -->",
    "filesStreamInfoCsvFile": "filestream.csv"
  },
  "destination": {
    "azureSqlDatabaseConnectionString": "<-- replace with Azure SQL connection string -->",
    "azureBlobStorageConnectionString": "<-- replace with Azure Blob Storage connection string -->"
  }
}
```
- Go to the command line, to the directory where you cloned the repo:
```csharp
> dotnet restore
> dotnet build src/FileStreamToAzureStorageMigrator.sln
> dotnet run --project src/FileStreamToAzureStorageMigrator/FileStreamToAzureStorageMigrator.csproj
```

- Simply start [here](/src/FileStreamToAzureStorageMigrator/Program.cs) 
```csharp
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

            var sqlServerToAzureBlobStorage = new SqlServerToAzureBlobStorage(sourceSqlServerDatabaseConnectionString, destinationAzureBlobStorageConnectionString, filesStreamInfoCsvFile);
            await sqlServerToAzureBlobStorage.ExtractDataFromDbAsync();

            var updateTask = new CsvFilesMetadataToAzureSql(destinationAzureSqlDatabaseConnectionString, filesStreamInfoCsvFile);
            await updateTask.UpdateAsync();
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
```

- You can also adjust:
  - The [FileStreamFile.cs](/src/FileStreamToAzureStorageMigrator/FileStreamFile.cs) class with the desired file metadata, to be migrated to the Azure SQL database table 
  - The [SqlServerToAzureBlobStorage.cs](/src/FileStreamToAzureStorageMigrator/SqlServerToAzureBlobStorage.cs) SQL queries in this class with the queries you need to fetch from you SQL Server database
  
### Contributors
[@damirkusar](https://github.com/damirkusar), [@DjolePetrovic](https://github.com/DjolePetrovic), [@fbeltrao](https://github.com/fbeltrao) and [@CarlosSardo](https://github.com/carlossardo)
