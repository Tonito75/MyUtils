using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.OpenCV.RTSP
{
    public interface IRTSPService
    {
        Task<OneOf<byte[],string>> Capture(string url);
    }
}
