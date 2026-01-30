using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.IO
{
    public class IOService : IIOService
    {
        public bool FileExists(string filePath)
        {
            try
            {
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                throw new IOServiceException("Erreur lors de la vérification de l'existence du fichier.", ex);
            }
        }

        public bool DirectoryExists(string folderPath)
        {
            try
            {
                return Directory.Exists(folderPath);
            }
            catch (Exception ex)
            {
                throw new IOServiceException("Erreur lors de la vérification de l'existence du dossier.", ex);
            }
        }

        public void CopyFileToFolder(string fileSourcePath, string folderDestinationPath, bool overwrite = false)
        {
            try
            {
                if (!FileExists(fileSourcePath))
                {
                    throw new IOServiceException($"Le fichier source n'existe pas: {fileSourcePath}.");
                }

               
                if (!string.IsNullOrEmpty(folderDestinationPath) && !DirectoryExists(folderDestinationPath))
                {
                    Directory.CreateDirectory(folderDestinationPath);
                }

                var sourceFileName = Path.GetFileName(fileSourcePath);
                File.Copy(fileSourcePath, Path.Combine(folderDestinationPath, sourceFileName), overwrite);

            }
            catch(Exception ex)
            {
                throw new IOServiceException($"Error copying file {fileSourcePath} to {folderDestinationPath}.",ex);
            }
        }

        public void CopyFile(string fileSourcePath, string fileDestinationPath, bool overwrite = false)
        {
            try
            {
                if (!FileExists(fileSourcePath))
                {
                    throw new IOServiceException($"Le fichier source n'existe pas: {fileSourcePath}");
                }

                string? destDirectory = Path.GetDirectoryName(fileDestinationPath);
                if (!string.IsNullOrEmpty(destDirectory) && !DirectoryExists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Copy(fileSourcePath, fileDestinationPath, overwrite);
            }
            catch (Exception ex)
            {
                throw new IOServiceException("Erreur lors de la copie du fichier.", ex);
            }
        }

        public async Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            try
            {
                if (!FileExists(sourcePath))
                {
                    throw new IOServiceException($"Le fichier source n'existe pas: {sourcePath}");
                }

                string? destDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDirectory) && !DirectoryExists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var destinationStream = new FileStream(destinationPath,
                    overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }
            catch (Exception ex)
            {
                throw new IOServiceException("Erreur lors de la copie asynchrone du fichier.", ex);
            }
        }

        public IEnumerable<string> ListFiles(string folderPath, string searchPattern = "*.*", bool searchSubdirectories = false)
        {
            try
            {
                if (!DirectoryExists(folderPath))
                {
                    throw new IOServiceException($"Le dossier n'existe pas: {folderPath}");
                }

                var searchOption = searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(folderPath, searchPattern, searchOption);
            }
            catch (Exception ex)
            {
                throw new IOServiceException("Erreur lors du listing des fichiers.", ex);
            }
        }

        public string ReadFileContent(string filePath)
        {
            try
            {
                if (!FileExists(filePath))
                {
                    throw new IOServiceException($"Le fichier n'existe pas: {filePath}");
                }

                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                throw new IOServiceException("Erreur lors de la lecture du fichier.", ex);
            }
        }

        public void WriteFileContent(string filePath, string content, bool overwrite = false)
        {
            try
            {
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (FileExists(filePath) && !overwrite)
                {
                    throw new IOServiceException("Le fichier existe déjà et l'écrasement est désactivé.");
                }

                File.WriteAllText(filePath, content);
            }
            catch (Exception ex)
            {
                throw new IOServiceException("Erreur lors de l'écriture dans le fichier.", ex);
            }
        }

        public DirectoryInfo GetDirectoryInfo(string path)
        {
            try
            {
                return new DirectoryInfo(path);
            }
            catch(Exception ex)
            {
                throw new IOServiceException($"Error while getting the directory info of {path}.",ex);
            }
        }

        public FileInfo GetFileInfo(string path)
        {
            try
            {
                return new FileInfo(path);
            }
            catch (Exception ex)
            {
                throw new IOServiceException($"Error while getting the file info of {path}.", ex);
            }
        }

        public async Task CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true)
        {
            // Crée le dossier de destination s'il n'existe pas
            Directory.CreateDirectory(destinationDir);

            // Copie tous les fichiers
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destinationDir, fileName);
                File.Copy(filePath, destFilePath, overwrite);
            }

            // Recurse sur tous les sous-dossiers
            foreach (string dirPath in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dirPath);
                string destSubDir = Path.Combine(destinationDir, dirName);
                await CopyDirectory(dirPath, destSubDir, overwrite);
            }
        }

        public void CleanDirectory(string path)
        {
            // Delete all files
            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }

            // Delete all subdirectories recursively
            foreach (string dir in Directory.GetDirectories(path))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }


}
