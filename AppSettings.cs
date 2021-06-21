namespace PoC_client
{
    using System.Collections.Generic;
    using LUC.Services.Implementation.Models;

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
