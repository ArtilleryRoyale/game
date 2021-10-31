using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace Jrmgx.Helpers
{
    public static class FileDir
    {
        public static void AppendToTextFile(string name, string data)
        {
            string path = Application.persistentDataPath + "/" + name;
            if (!File.Exists(path)) {
                using (StreamWriter sw = File.CreateText(path)) {
                    sw.WriteLine(data);
                }
            } else {
                using (StreamWriter sw = File.AppendText(path)) {
                    sw.WriteLine(data);
                }
            }
        }

        public static string RemoveExtensions(string filename, List<string> extensions)
        {
            foreach (string extension in extensions) {
                filename = filename.Replace("." + extension, "");
            }
            return filename;
        }

        public static Comparison<FileInfo> FileInfoComparison {
            get {
                return new Comparison<FileInfo>(delegate (FileInfo a, FileInfo b) {
                    return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                });
            }
        }

        public static void SaveTextureToFile(Texture2D texture, string fullPath, int JPG_QUALITY = 51)
        {
            byte[] bytes = texture.EncodeToJPG(JPG_QUALITY);
            File.WriteAllBytes(fullPath, bytes);
        }

        public static IEnumerator LoadTextureFromFile(FileInfo file, Action<Texture2D> callback = null)
        {
            return Basics.LoadTextureFromUrl("file:///" + file.FullName, callback);
        }

        /// <summary>
        /// Copy the source directory to destination
        /// see: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        /// </summary>
        /// <param name="sourceDirName">Source dir name.</param>
        /// <param name="destDirName">Destination dir name.</param>
        /// <param name="recurse">If set to <c>true</c> copy sub dirs.</param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool recurse)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: " + sourceDirName
                );
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName)) {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (recurse) {
                foreach (DirectoryInfo subdir in dirs) {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, recurse);
                }
            }
        }
    }
}
