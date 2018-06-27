using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FileStreamToAzureStorageMigrator
{
    /// <summary>
    /// Class that represents the metadata info of your files, in SQL Server
    /// Can be adjusted to your needs
    /// </summary>
    public class FileStreamFile
    {

        public string StreamId { get; set; }

        public string DocumentId { get; set; }

        public string Site { get; set; }

        public string ContentType { get; set; }

        public string DocumentType { get; set; }

        public string FileType { get; set; }

        public string Name { get; set; }

        public string Folder { get; set; }

        public string Container { get; set; }

        public string BlobLocation { get; set; }


        public FileStreamFile()
        {

        }

        public FileStreamFile(string streamId, string fullPath, string documentId, string site, string contentType, string documentType, string fileType)
        {
            var filename = Path.GetFileName(fullPath);
            var path = Path.GetDirectoryName(fullPath);
            var indexOfSlash = path.IndexOf('\\');

            string container;
            if (indexOfSlash != -1)
            {
                container = path.Substring(0, indexOfSlash);
                path = path.Substring(indexOfSlash + 1);
            }
            else
            {
                container = path;
            }

            StreamId = streamId;
            DocumentId = documentId;
            Site = site;
            ContentType = contentType;
            DocumentType = documentType;
            FileType = fileType;
            Name = NormalizeName(filename);
            Folder = NormalizeName(path);
            Container = container;
        }


        /// <summary>
        /// Method that can be used to normalize file/folder names 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string NormalizeName(string name)
        {
            return Regex.Replace(name.ToLowerInvariant(), @"\s+", "-");
        }

    }
}