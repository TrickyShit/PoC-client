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
}