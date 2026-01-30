using OneOf;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.OpenCV.RTSP
{
    public class RTSPService : IRTSPService
    {
        public Task<OneOf<byte[], string>> Capture(string rtspUrl)
        {
            try
            {
                using var capture = new VideoCapture(rtspUrl);
                if (!capture.IsOpened())
                {
                    return Task.FromResult<OneOf<byte[], string>>("Impossible d'ouvrir le flux RTSP");
                }

                using var frame = new Mat();
                if (capture.Read(frame) && !frame.Empty())
                {
                    Cv2.ImEncode(".jpg", frame, out var imageBytes);
                    return Task.FromResult<OneOf<byte[], string>>(imageBytes);
                }
                else
                {
                    return Task.FromResult<OneOf<byte[], string>>("Erreur lors de la lecture d'une frame du flux RTSP");
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult<OneOf<byte[], string>>($"Erreur inatendue lors de la lecture du flux RTSP : {ex.Message}");
            }
        }
    }
}
