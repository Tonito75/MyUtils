using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.IO
{
    /// <summary>
    /// Interface définissant des opérations de base sur le système de fichiers.
    /// </summary>
    public interface IIOService
    {
        /// <summary>
        /// Vérifie si un fichier existe à un chemin donné.
        /// </summary>
        /// <param name="filePath">Chemin complet du fichier à vérifier.</param>
        /// <returns>True si le fichier existe, sinon false.</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Vérifie si un dossier existe à un chemin donné.
        /// </summary>
        /// <param name="folderPath">Chemin complet du dossier à vérifier.</param>
        /// <returns>True si le dossier existe, sinon false.</returns>
        bool DirectoryExists(string folderPath);

        /// <summary>
        /// Copie un fichier vers un nouveau chemin.
        /// </summary>
        /// <param name="sourcePath">Chemin du fichier source.</param>
        /// <param name="destinationPath">Chemin de destination.</param>
        /// <param name="overwrite">Indique s’il faut écraser un fichier existant.</param>
        void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// Copy a file to a new directory.
        /// </summary>
        /// <param name="fileSourcePath"></param>
        /// <param name="folderDestinationPath"></param>
        /// <param name="overwrite"></param>
        void CopyFileToFolder(string fileSourcePath, string folderDestinationPath, bool overwrite = false);

        /// <summary>
        /// Liste les fichiers dans un dossier donné.
        /// </summary>
        /// <param name="folderPath">Chemin du dossier.</param>
        /// <param name="searchPattern">Filtre de recherche (ex: *.txt).</param>
        /// <param name="searchSubdirectories">Inclure les sous-dossiers ?</param>
        /// <returns>Liste des chemins de fichiers.</returns>
        IEnumerable<string> ListFiles(string folderPath, string searchPattern = "*.*", bool searchSubdirectories = false);

        /// <summary>
        /// Lit le contenu d’un fichier texte.
        /// </summary>
        /// <param name="filePath">Chemin du fichier.</param>
        /// <returns>Contenu du fichier sous forme de chaîne.</returns>
        string ReadFileContent(string filePath);

        /// <summary>
        /// Écrit du contenu texte dans un fichier.
        /// </summary>
        /// <param name="filePath">Chemin du fichier.</param>
        /// <param name="content">Contenu à écrire.</param>
        /// <param name="overwrite">Écraser si le fichier existe ?</param>
        void WriteFileContent(string filePath, string content, bool overwrite = false);

        /// <summary>
        /// Gets the directory info.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        DirectoryInfo GetDirectoryInfo(string path);

        /// <summary>
        /// Gets the file info.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        FileInfo GetFileInfo(string path);

        /// <summary>
        /// Copy a full directory to an other one.
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        Task CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true);

        void CleanDirectory(string path);
    }
}
