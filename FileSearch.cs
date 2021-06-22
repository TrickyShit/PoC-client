using System;
using System.Collections.Generic;
using System.IO;

namespace PoC_client
{
    internal class FileSearch
    {
        public static IEnumerable<string> FilesInDirAndSubdir(string rootFolderPath, string fileSearchPattern)
        {
            Func<string, IEnumerable<string>> funcToFind;
            if (!string.IsNullOrWhiteSpace(fileSearchPattern))
            {
                funcToFind = (pathToFile) => Directory.GetFiles(pathToFile, fileSearchPattern);
            }
            else
            {
                funcToFind = (pathToFile) => Directory.GetFiles(pathToFile);
            }

            return FindFiles(rootFolderPath, funcToFind);
        }

        private static List<string> FindFiles(string rootFolderPath, Func<string, IEnumerable<string>> funcToFind)
        {
            List<string> findFiles = new List<string>();
            Queue<string> dirsToSearchFiles = new Queue<string>();
            dirsToSearchFiles.Enqueue(rootFolderPath);

            while (dirsToSearchFiles.Count > 0)
            {
                string folderPath = dirsToSearchFiles.Dequeue();
                IEnumerable<string> filesInOneDir;
                try
                {
                    filesInOneDir = funcToFind(folderPath);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                findFiles.AddRange(filesInOneDir);

                var dirsInCurrentDir = Directory.GetDirectories(folderPath);
                foreach (var dir in dirsInCurrentDir)
                {
                    dirsToSearchFiles.Enqueue(dir);
                }
            }

            return findFiles;
        }

        public static IEnumerable<string> FilesInDirAndSubdir(string rootFolderPath) =>
            FindFiles(rootFolderPath, (pathToDir) => Directory.GetFiles(pathToDir));
    }

    public interface IBucketName
    {
        string ServerName { get; }

        string LocalName { get; }

        bool IsSuccess { get; }

        string ErrorMessage { get; }
    }

    public class BucketName : IBucketName
    {
        public BucketName(string serverName, string localName)
        {
            ServerName = serverName;
            LocalName = localName;
            IsSuccess = true;
            ErrorMessage = string.Empty;
        }

        public BucketName(string errorMessage)
        {
            ErrorMessage = errorMessage;
            IsSuccess = false;
        }

        public string ServerName { get; }

        public string LocalName { get; }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public override string ToString()
        {
            return $"{ServerName} => {LocalName}";
        }
    }

}