using Common.Classes;
using Domain.FreeBox.Device;
using FluentFTP.Helpers;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;

namespace DiscordBot.Extensions;

public static class LanDeviceExtensions
{
    public static PropertyInfo[] GetFields(this PropertyInfo[] properties)
    {
        return properties.Where(p => p.Name == "OriginalName" || p.Name == "MacAddress" || p.Name == "IpAddress").ToArray();
    }

    public class Grid<T> where T : class
    {
        public string[,] Mat { get; private set; }

        public Dictionary<string, (Func<string, string>? Action, string ColumnName)> Actions { get; private set; }

        public Grid(IList<T> objs, Dictionary<string, (Func<string, string>? Action, string ColumnName)> actions)
        {
            Actions = actions;

            Mat = new string[objs.Count() + 1, actions.Count];

            int row = 0;
            // Les headers
            foreach(var prop in actions.Keys)
            {
                if(Actions.TryGetValue(prop, out var pair))
                {
                    var colName = pair.ColumnName;

                    Mat[0, row] = colName;
                    row++;
                }
               
            }

            // Le reste
            row = 1;
            foreach(var obj in objs)
            {
                int col = 0;
                foreach(var prop in actions.Keys)
                {
                    // La propriété de T
                    var propValue = obj.GetType().GetProperty(prop);

                    if(propValue == null || propValue.GetValue(obj) == null)
                    {
                        Mat[row, col] = "";
                    }
                    else
                    {
                        // La valeur de la propriété de T
                        var strValue = propValue.GetValue(obj).ToString();

                        if(!string.IsNullOrEmpty(strValue))
                        {
                            // Si action de transfo pour cette prop, on l'exec, sinon osef
                            if(Actions.TryGetValue(prop, out var pair))
                            {
                                var newStrValue = pair.Action?.Invoke(strValue) ?? strValue;
                                Mat[row, col] = newStrValue;
                            }
                            else
                            {
                                Mat[row, col] = strValue;
                            }
                        }
                        else
                        {
                            Mat[row, col] = "";
                        }
                    }

                    col++;
                }
                row++;
            }
        }

        public string FormatToGrid()
        {
            var sb = new StringBuilder();
            sb.AppendLine("```");

            for(int i = 0; i < Mat.GetLength(0); i++)
            {
                for(int j=0; j < Mat.GetLength(1); j++)
                {
                    // No '|' for headers
                    sb.Append(Mat[i, j].PadRight(GetLongerColumn(j))).Append(j == Mat.GetLength(1) - 1 ? "" : i == 0 ? "  " : "| ");
                }

                if(i == 0)
                {
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            sb.AppendLine("```");
            return sb.ToString();
        }

        private int GetLongerColumn(int col)
        {
            var len = 0;
            for(int r = 0; r< Mat.GetLength(1);r++)
            {
                var current = Mat[r, col];
                if (current.Length > len)
                {
                    len = current.Length;
                }
            }

            return len +1 ;
        }
    }

    public static string FormatVerboseList(this IList<LanDevice> devices)
    {
        var dict = new Dictionary<string, (Func<string, string>? Action, string ColumnName)>
        {
            {"IsConnected", ((c) => (c == "True" ? Emojis.Connected : Emojis.UnConnected),"") },
            {"Name", (null,"Name") },
            {"IpAddress", (null, "Ip Address") },
            {"LastConnected", (DateAsStrIntoDaysFromToday, "LastConnected") },
            {"Vendor", ((s) => !string.IsNullOrEmpty(s) ? s : "Unknown", "Vendor") },
        };

        var grid = new Grid<LanDevice>(devices.OrderByDescending(d => d.IsConnected).OrderByDescending(d => d.LastConnected).ToList(), dict);

        return grid.FormatToGrid();
    }

    public static string FormatSimpleList(this IList<LanDevice> devices)
    {
        var dict = new Dictionary<string, (Func<string, string>? Action, string ColumnName)>
        {
            {"IsConnected", ((c) => (c == "True" ? Emojis.Connected : Emojis.UnConnected),"") },
            {"Name", (null,"Name") },
        };

        var grid = new Grid<LanDevice>(devices.OrderByDescending(d => d.IsConnected).OrderByDescending(d => d.LastConnected).ToList(), dict);

        return grid.FormatToGrid();
    }

    public static string ToStringOnConnect(this LanDevice device)
    {
        var sb = new StringBuilder();

        // Nom (priorité au nom custom)
        var displayName = !string.IsNullOrWhiteSpace(device.CustomName)
            ? device.CustomName
            : device.OriginalName;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            sb.Append(displayName);
        }
        else
        {
            sb.Append("Unknown Device");
        }

        // Adresse IP et type
        if (!string.IsNullOrWhiteSpace(device.IpAddress))
        {
            sb.Append($" | {device.IpAddress} ({device.AddressType})");
        }

        // MAC Address
        sb.Append($" | MAC: {device.MacAddress}");

        // Vendor/Type
        if (!string.IsNullOrWhiteSpace(device.Vendor))
        {
            sb.Append($" | {device.Vendor}");
        }

        if (!string.IsNullOrWhiteSpace(device.HostType))
        {
            sb.Append($" ({device.HostType})");
        }

        // Statut de connexion
        if (device.IsConnected)
        {
            sb.Append(" | ✓ Connected");
            if (device.ConnectedSince != default)
            {
                var duration = DateTime.Now - device.ConnectedSince;
                sb.Append($" since {FormatDuration(duration)}");
            }
        }
        else
        {
            sb.Append(" | ✗ Offline");
            if (device.LastConnected != default)
            {
                sb.Append($" (last seen: {FormatLastSeen(device.LastConnected)})");
            }
        }

        if (device.Isfavourite)
        {
            sb.Append(" ⭐");
        }

        return sb.ToString();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{(int)duration.TotalMinutes}m";
    }

    private static string FormatLastSeen(DateTime lastConnected)
    {
        var duration = DateTime.Now - lastConnected;
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays} days ago";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} hours ago";
        return $"{(int)duration.TotalMinutes} minutes ago";
    }

    public static string DateAsStrIntoDaysFromToday(string dateString)
    {
        DateTime date;

        bool isValid = DateTime.TryParseExact(
            dateString,
            "dd/MM/yyyy HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date
        );

        if (!isValid)
            return "Invalid date";

        DateTime aujourdHui = DateTime.Today;

        int jours = (date.Date - aujourdHui).Days;

        if (jours > 0)
            return $"In {jours} days";
        else if (jours < 0)
            return $"{-jours} days ago";
        else
            return "Today";
    }
}
