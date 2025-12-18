using System.IO.Compression;
using System.Text;

namespace JAFDTC.Core.IO
{
    public static class FileHelper
    {
        public static string ReadAllText(string filePath)
        {
            return System.IO.File.ReadAllText(filePath);
        }

        public static string ReadAllText(string filePath, string zipEntryName)
        {
            using var archive = ZipFile.Open(filePath, ZipArchiveMode.Read);
            using var entry = archive.GetEntry(zipEntryName).Open();
            using var memoryStream = new MemoryStream();
            entry.CopyTo(memoryStream);
            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static string ReadAllText(string filePath, int zipEntryIndex)
        {
            using var archive = ZipFile.Open(filePath, ZipArchiveMode.Read);
            using var entry = archive.Entries[zipEntryIndex].Open();
            using var memoryStream = new MemoryStream();
            entry.CopyTo(memoryStream);
            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }


    }
}
