namespace PoC_client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using LUC.Interfaces;
    using LUC.Services.Implementation.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [Export(typeof(ISettingsService))]
    internal class SettingsService : ISettingsService
    {
        public AppSettings AppSettings { get; private set; }

        [Import(typeof(ICurrentUserProvider))]
        private ICurrentUserProvider CurrentUserProvider { get; set; }

        public string AppSettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LightUponCloud", "appsettings.json");

        public SettingsService()
        {
            ReadSettingsFromFile();
        }

        public void ReadSettingsFromFile()
        {
            if (File.Exists(AppSettingsFilePath))
            {
                var json = File.ReadAllText(AppSettingsFilePath);

                if (string.IsNullOrEmpty(json))
                {
                    AppSettings = new AppSettings();
                }
                else
                {
                    AppSettings = JsonConvert.DeserializeObject<AppSettings>(json);

                    if (AppSettings == null)
                    {
                        AppSettings = new AppSettings();
                    }
                }
            }
            else
            {
                AppSettings = new AppSettings();
            }

            //if (current.IsShowConsole)
            //{
            //    ConsoleHelper.CreateConsole();
            //    Console.WriteLine("Console is launched.");
            //}
        }

        private void SerializeSettingToFile()
        {
            try
            {
                var directoryWithSettings = Path.GetDirectoryName(AppSettingsFilePath);
                if (!Directory.Exists(directoryWithSettings))
                {
                    Directory.CreateDirectory(directoryWithSettings);
                }

                var serializer = new JsonSerializer();
                serializer.Converters.Add(new IsoDateTimeConverter());
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;

                using (var sw = new StreamWriter(AppSettingsFilePath))
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, AppSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO Investigate IOException
                Log.Error(ex, "SerializeSettingToFile error");
            }
        }

        public string ReadLanguageCulture()
        {
            return AppSettings.LanguageCulture;
        }

        public string ReadUserRootFolderPath()
        {
            var login = GetUserLogin();

            var userSettings = AppSettings.SettingsPerUser.Single(x => x.Login == login);
            return userSettings.RootFolderPath;
        }

        public void WriteLanguageCulture(string culture)
        {
            if (AppSettings.LanguageCulture != culture)
            {
                AppSettings.LanguageCulture = culture;
                SerializeSettingToFile();
            }
        }

        public void WriteUserRootFolderPath(string userRootFolderPath)
        {
            var login = GetUserLogin();

            var userSettings = AppSettings.SettingsPerUser.Single(x => x.Login == login);

            if (userSettings.RootFolderPath != userRootFolderPath)
            {
                userSettings.RootFolderPath = userRootFolderPath;
                SerializeSettingToFile();
            }
        }

        private string GetUserLogin()
        {
            if (CurrentUserProvider.LoggedUser == null ||
                CurrentUserProvider.LoggedUser.Login == null)
            {
                throw new ArgumentNullException($"{nameof(CurrentUserProvider.LoggedUser)} or {nameof(CurrentUserProvider.LoggedUser.Login)}");
            };

            var login = CurrentUserProvider.LoggedUser.Login;

            var userSettings = AppSettings.SettingsPerUser.SingleOrDefault(x => x.Login == login);

            if (userSettings == null)
            {
                AppSettings.SettingsPerUser.Add(new UserSetting
                {
                    Login = login
                });
            }

            return login;
        }

        public string ReadRememberedLogin()
        {
            var possibleRemembered = AppSettings.SettingsPerUser.Single(x => x.IsRememberLogin).Login;
            return possibleRemembered;
        }

        public void WriteIsRememberPassword(bool isRememberPassword, string base64Password)
        {
            var login = GetUserLogin();

            AppSettings.SettingsPerUser.ForEach(x => x.IsRememberLogin = false);
            AppSettings.SettingsPerUser.ForEach(x => x.IsRememberPassword = false);
            AppSettings.SettingsPerUser.ForEach(x => x.Base64Password = string.Empty);

            AppSettings.SettingsPerUser.Single(x => x.Login == login).IsRememberLogin = true;
            AppSettings.SettingsPerUser.Single(x => x.Login == login).IsRememberPassword = isRememberPassword;

            if (isRememberPassword)
            {
                AppSettings.SettingsPerUser.Single(x => x.Login == login).Base64Password = base64Password;
            }

            SerializeSettingToFile();
        }

        public void WriteLastSyncDateTime()
        {
            var login = GetUserLogin();
            AppSettings.SettingsPerUser.Single(x => x.Login == login).LastSyncDateTime = DateTime.UtcNow;

            SerializeSettingToFile();
        }

        public string ReadBase64Password()
        {
            var possibleRemembered = AppSettings.SettingsPerUser.SingleOrDefault(x => x.IsRememberPassword)?.Base64Password;
            return possibleRemembered;
        }

        public DateTime ReadLastSyncDateTime()
        {
            var possibleDateTime = AppSettings.SettingsPerUser.SingleOrDefault(x => x.IsRememberPassword)?.LastSyncDateTime; // TODO RR Why isremembered

            return possibleDateTime.GetValueOrDefault(DateTime.UtcNow);
        }

        public void WriteBase64EncryptionKey(string base64Key) // TODO RR What else per login?
        {
            var login = GetUserLogin();

            AppSettings.SettingsPerUser.Single(x => x.Login == login).Base64EncryptionKey = base64Key;

            SerializeSettingToFile();
        }

        public string ReadBase64EncryptionKey()
        {
            var login = GetUserLogin();
            var possibleKey = AppSettings.SettingsPerUser.SingleOrDefault(x => x.Login == login)?.Base64EncryptionKey;
            return possibleKey;
        }

        public bool IsLogToTxtFile
        {
            get
            {
                return AppSettings.IsLogToTxtFile;
            }
        }

        public bool IsShowConsole
        {
            get
            {
                return AppSettings.IsShowConsole;
            }
        }

        public IList<string> ReadFoldersToIgnore()
        {
            var login = GetUserLogin();
            var result = AppSettings.SettingsPerUser.SingleOrDefault(x => x.Login == login)?.FoldersToIgnore;

            if (result == null)
            {
                return new List<string>();
            }

            return result;
        }

        public void WriteFoldersToIgnore(IList<string> pathes)
        {
            var login = GetUserLogin();
            AppSettings.SettingsPerUser.Single(x => x.Login == login).FoldersToIgnore = pathes;

            SerializeSettingToFile();
        }
    }
}
