using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Date
{
    public class DateService : IDateService
    {
        public string GetCurrentDateForFile()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }

        public string GetCurrentDateForFolderYYYYMMDD()
        {
            return DateTime.Now.ToString("yyyy/MM/dd");
        }

        public string GetCurrentDateForFolderYesterdayYYYYMMDD()
        {
            return DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd");
        }

        public string GetCurrentDateForFolderSinceYYYYMMDD(int days)
        {
            return DateTime.Now.AddDays(-days).ToString("yyyy/MM/dd");
        }
    }
}
