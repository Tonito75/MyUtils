using Common.Logger;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.OpenCV.RTSP
{
    public class TimeLapseBuilder : ITimeLapseBuilder
    {

        private readonly ILogService _logService;

        public TimeLapseBuilder(ILogService logService)
        {
            _logService = logService;
        }

        public async Task<(bool,string)> CreateTimelapse(string dossierImages, string fichierSortie, double fps, string extension, int divider)
        {
            try
            {
                // Récupérer toutes les images JPG triées par nom
                var imagePaths = Directory.GetFiles(dossierImages, $"*.{extension}")
                                         .OrderBy(f => f)
                                         .ToArray();

                if (imagePaths.Length == 0)
                {
                    return (false, "Aucune image trouvée");
                }

                _logService.Log($"Traitement de {imagePaths.Length} images...");

                // Lire la première image pour obtenir les dimensions
                using var firstImage = new Mat(imagePaths[0]);
                if (firstImage.Empty())
                {
                    return (false,"Impossible de lire la première image : elle est vide");
                }

                int width = firstImage.Width;
                int height = firstImage.Height;

                // Créer le dossier de sortie si nécessaire
                var outputDir = Path.GetDirectoryName(fichierSortie);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Initialiser le VideoWriter
                var fourcc = VideoWriter.FourCC('m', 'p', '4', 'v');
                using var videoWriter = new VideoWriter(fichierSortie, fourcc, fps, new Size(width, height));

                if (!videoWriter.IsOpened())
                {
                    return (false, "Impossible d'ouvrir le fichier de sortie pour l'écriture");
                }

                int i = 0;

                // Traiter chaque image
                foreach (var imagePath in imagePaths)
                {
                    if (i != divider)
                    {
                        i++;
                        _logService.Log($"Image {Path.GetFileName(imagePath)} ignorée");
                        continue;
                    }
                    else
                    {
                        i = 0;
                    }

                    using var img = new Mat(imagePath);
                    if (img.Empty())
                    {
                        _logService.Log($"Impossible de lire l'image : {Path.GetFileName(imagePath)}");
                        continue;
                    }

                    _logService.Log($"Traitement de l'image {Path.GetFileName(imagePath)}");

                    await Task.Run(() =>
                    {
                        // Redimensionner l'image si nécessaire
                        Mat resizedImg;
                        if (img.Width != width || img.Height != height)
                        {
                            resizedImg = new Mat();
                            Cv2.Resize(img, resizedImg, new Size(width, height));
                        }
                        else
                        {
                            resizedImg = img.Clone();
                        }

                        // Écrire l'image dans la vidéo
                        videoWriter.Write(resizedImg);
                        resizedImg.Dispose();
                    });
                }

                _logService.Log($"✅ Timelapse créé : {fichierSortie}");

                return (true,string.Empty);
            }
            catch (Exception ex)
            {
                return (false,$"Une erreur non gérée s'est produite lors de la création du timelaspe : {ex.Message}");
            }
            
        }
    }
}
