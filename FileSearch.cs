﻿using System;
using System.Collections.Generic;
using System.IO;

namespace PoC_client
{
    class FileSearch
    {
        public static IEnumerable<String> FilesInDirAndSubdir(String rootFolderPath, String fileSearchPattern)
        {
            Func<String, IEnumerable<String>> funcToFind;
            if (!String.IsNullOrWhiteSpace(fileSearchPattern))
            {
                funcToFind = (pathToFile) => Directory.GetFiles(pathToFile, fileSearchPattern);
            }
            else
            {
                funcToFind = (pathToFile) => Directory.GetFiles(pathToFile);
            }

            return FindFiles(rootFolderPath, funcToFind);
        }

        private static List<String> FindFiles(String rootFolderPath, Func<String, IEnumerable<String>> funcToFind)
        {
            List<String> findFiles = new List<String>();
            Queue<String> dirsToSearchFiles = new Queue<String>();
            dirsToSearchFiles.Enqueue(rootFolderPath);

            while (dirsToSearchFiles.Count > 0)
            {
                String folderPath = dirsToSearchFiles.Dequeue();
                IEnumerable<String> filesInOneDir;
                try
                {
                    filesInOneDir = funcToFind(folderPath);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                findFiles.AddRange(filesInOneDir);
                //foreach (var file in filesInOneDir)
                //{
                //    findFiles.Addfile;
                //}

                var dirsInCurrentDir = Directory.GetDirectories(folderPath);
                foreach (var dir in dirsInCurrentDir)
                {
                    dirsToSearchFiles.Enqueue(dir);
                }
            }

            return findFiles;
        }

        public static IEnumerable<String> FilesInDirAndSubdir(String rootFolderPath) => 
            FindFiles(rootFolderPath, (pathToDir) => Directory.GetFiles(pathToDir));
    }
}