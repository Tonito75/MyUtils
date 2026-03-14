namespace PortalCameras.Services;

public class DateService
{
    /// <summary>
    /// Retourne le chemin de dossier pour la date d'aujourd'hui moins `days` jours.
    /// Format : "YYYY/MM/DD"
    /// </summary>
    public string GetDateFolderDaysAgo(int days)
    {
        var date = DateTime.Now.AddDays(-days);
        return date.ToString("yyyy/MM/dd");
    }

    public string FormatTimeAgoFrench(DateTime date)
    {
        var diff = DateTime.Now - date;

        if (diff.TotalMinutes < 1) return "à l'instant";
        if (diff.TotalMinutes < 60) return $"il y a {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"il y a {(int)diff.TotalHours} h";
        if (diff.TotalDays < 7) return $"il y a {(int)diff.TotalDays} jour(s)";
        return date.ToString("dd/MM/yyyy");
    }
}
