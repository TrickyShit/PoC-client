using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace PoC_client
{
    public class UserSetting
    {
        public UserSetting()
        {
            FoldersToIgnore = new List<string>();
        }

        public string Login { get; set; }

        public string RootFolderPath { get; set; }

        public bool IsRememberLogin { get; set; }

        public bool IsRememberPassword { get; set; }

        public string Base64Password { get; set; }

        public string Base64EncryptionKey { get; set; }

        public DateTime LastSyncDateTime { get; set; }

        public IList<string> FoldersToIgnore { get; set; }
    }

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

    public class GroupServiceModel
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

    public class LoginServiceModel
    {
        public LoginServiceModel()
        {
            Groups = new List<GroupServiceModel>();
        }

        public string Token { get; set; }

        public List<GroupServiceModel> Groups { get; set; }

        public string TenantId { get; set; }

        public string Login { get; set; }

        public string Id { get; set; }
    }

    public class ApiSettings
    {
        public ApiSettings()
        {
            var host = ConfigurationManager.AppSettings["RestApiHost"];

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidProgramException("API Host was not specified in configuration");
            }

            Host = host;
        }

        public ApiSettings(String accessToken)
            : this()
        {
            AccessToken = accessToken;
        }

        public string Host
        {
            get;
            private set;
        }

        public string AccessToken
        {
            get;
            private set;
        }

        public void InitializeAccessToken(string accessToken)
        {
            AccessToken = accessToken;
        }
    }

    public static class StaticExtensions
    {
        public static List<GroupServiceModel> ToGroupServiceModelList(this List<GroupSubResponse> groups)
        {
            var result = new List<GroupServiceModel>();

            foreach (var group in groups)
            {
                // read the string as UTF-8 bytes.
                var encodedUtf8Bytes = Encoding.UTF8.GetBytes(group.Name);

                // convert them into unicode bytes.
                var unicodeBytes = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, encodedUtf8Bytes);

                // builds the converted string.
                var unicodeGroupName = Encoding.Unicode.GetString(unicodeBytes);

                if (group.Id == null)
                {
                    throw new ArgumentNullException("id", "group.id is null");
                }

                if (group.Id == null)
                {
                    throw new ArgumentNullException("unicodeGroupName", "unicodeGroupName is null");
                }

                result.Add(new GroupServiceModel
                {
                    Id = group.Id,
                    Name = unicodeGroupName
                });
            }

            return result;
        }

        public static LoginServiceModel ToLoginServiceModel(this LoginResponse response)
        {
            var model = new LoginServiceModel
            {
                TenantId = response.TenantId,
                Token = response.Token,
                Login = response.Login,
                Id = response.Id
            };

            model.Groups = response.Groups.ToGroupServiceModelList();

            return model;
        }

        public static string ToHexPrefix(this string prefix)
        {
            if (prefix == string.Empty)
            {
                return string.Empty;
            }
            else
            {
                var result = string.Empty;

                foreach (var item in prefix.Split(new[] { '\\' }))
                {
                    result = result + item.ToHexString() + "/";
                }

                return result;
            }
        }

        public static string ToHexString(this string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            var hexStringWithDashes = BitConverter.ToString(bytes);
            var result = hexStringWithDashes.Replace("-", "");

            return result;
        }
    }
}