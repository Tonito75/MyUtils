using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.IO
{
    public class IOServiceException : Exception
    {
        public IOServiceException() { }

        public IOServiceException(string message)
            : base(message)
        {
        }

        public IOServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

}
