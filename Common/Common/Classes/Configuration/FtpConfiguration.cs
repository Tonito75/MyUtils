using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Classes.Configuration
{
    public class FtpConfiguration
    {
        public string? Host {  get; set; }

        public string? Port { get; set; }

        public string? UserName { get; set; }

        public string? Password { get; set; }

        public string? Folder {  get; set; }

        public override string ToString()
        {
            return $"FTP CONFIGURATION : [host : {Host}, port : {Port}, username : {UserName}, passwordset : {!string.IsNullOrEmpty(Password)} : folder : {Folder}";
        }

    }
}
