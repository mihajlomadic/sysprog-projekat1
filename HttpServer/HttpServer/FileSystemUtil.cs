using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    internal class FileSystemUtil
    {
        public static string SearchDirectoryForFile(string dirPath, string fileName)
        {
            if (!Directory.Exists(dirPath))
                throw new ArgumentException("Bad directory path!");

            DirectoryInfo rootDirInfo = new DirectoryInfo(dirPath);

            Stack<DirectoryInfo> dirQueue = new Stack<DirectoryInfo>();
            dirQueue.Push(rootDirInfo);

            while (dirQueue.Any())
            {
                DirectoryInfo currentDirInfo = dirQueue.Pop();

                foreach (var fileInfo in currentDirInfo.EnumerateFiles())
                {
                    if (fileInfo.Name.Equals(fileName))
                        return fileInfo.FullName;
                }

                foreach (var dirInfo in currentDirInfo.EnumerateDirectories())
                    dirQueue.Push(dirInfo);
            }

            return null;
        }
    }
}
