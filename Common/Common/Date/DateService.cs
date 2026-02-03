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

        public string FormatTimeAgoFrench(DateTime date)
        {
            var timeSpan = DateTime.Now - date;

            if (timeSpan.TotalMinutes < 1)
                return "À l'instant";
            if (timeSpan.TotalMinutes < 60)
                return $"Il y a {(int)timeSpan.TotalMinutes} min";
            if (timeSpan.TotalHours < 24)
                return $"Il y a {(int)timeSpan.TotalHours} h";
            if (timeSpan.TotalDays < 2)
                return "Hier";
            if (timeSpan.TotalDays < 7)
                return $"Il y a {(int)timeSpan.TotalDays} jours";
            if (timeSpan.TotalDays < 30)
                return $"Il y a {(int)(timeSpan.TotalDays / 7)} sem.";
            if (timeSpan.TotalDays < 365)
                return $"Il y a {(int)(timeSpan.TotalDays / 30)} mois";

            return $"Il y a {(int)(timeSpan.TotalDays / 365)} an(s)";
        }
    }
}
