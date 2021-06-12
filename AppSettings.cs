namespace PoC_client
{
    using LUC.Services.Implementation.Models;
    using System.Collections.Generic;

    internal class AppSettings
    {
        internal AppSettings()
        {
            SettingsPerUser = new List<UserSetting>();
            IsShowConsole = false;
            IsLogToTxtFile = true;
        }

        public List<UserSetting> SettingsPerUser { get; set; }

        public bool IsShowConsole { get; set; }

        public bool IsLogToTxtFile { get; set; }

        public string LanguageCulture { get; set; }
    }
}
