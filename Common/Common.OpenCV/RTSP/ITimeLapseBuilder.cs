using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.OpenCV.RTSP
{
    public interface ITimeLapseBuilder
    {
        Task<(bool, string)> CreateTimelapse(string dossierImages, string fichierSortie, double fps, string extension, int divider);
    }
}
