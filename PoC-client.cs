using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LUC.ApiClient;
using LUC.Interfaces;
using LUC.Services.Implementation;
using LUC.Services.Implementation.Models;

namespace PoC_client
{

    public class ConsoleApiClient
    {
        private const string Login = "integration1";
        private const string Password = "integration1";

        [Import(typeof(ILoggingService))]
        private static LoggingService loggingService = new LoggingService();

        [Import(typeof(ICurrentUserProvider))]
        private static ICurrentUserProvider currentUserProvider = new CurrentUserProvider();

        [Import(typeof(ISyncingObjectsList))]
        private static ISyncingObjectsList syncingObjectsList = new SyncingObjectsList();

        private static SettingsService settingsService = new SettingsService();
        private static UserSetting userSetting;

        private readonly ApiClient apiClient;

        static ConsoleApiClient()
        {
            loggingService.SettingsService = settingsService;
            userSetting = settingsService.AppSettings.SettingsPerUser.Single((settings) => settings.Login == Login);
            currentUserProvider.RootFolderPath = userSetting.RootFolderPath;
        }

        ~ConsoleApiClient()
        {
            apiClient?.LogoutAsync();
        }

        static async Task Main(string[] args)
        {
            ApiClient apiClient = new ApiClient(currentUserProvider, loggingService)
            {
                SyncingObjectsList = syncingObjectsList
            };

            var loginresponse = await apiClient.LoginAsync(Login, Password);
            Console.WriteLine(loginresponse.Message);

            while (true)
            {
                IEnumerable<String> filesInRootFolder;
                string filename;
                string fullPath;
                string rootFolder = userSetting.RootFolderPath + "\\integration1";
                do
                {
                    Console.WriteLine("Please copy your file to folder integration1");
                    Console.WriteLine("Enter a name of file or press Enter to quit");
                    if (args.Count() == 0) filename = Console.ReadLine();
                    else filename = args[0];

                    if (filename.Length == 0) //added logout and exit of the program
                    {
                        await apiClient.LogoutAsync();
                        return;
                    }
                    filesInRootFolder = FileSearch.FilesInDirAndSubdir(rootFolder);
                    fullPath = rootFolder + "\\" + filename;
                    if (!filesInRootFolder.Contains(fullPath))
                    {
                        Console.WriteLine("Folder " + rootFolder + " don`t contains a " + filename);
                    }
                }
                while (!filesInRootFolder.Contains(fullPath));
                FileInfo fileInfo = new FileInfo(fullPath);
                var lightClient = new LightClient.LightClient();
                var response = await lightClient.Upload("https://lightupon.cloud", loginresponse.Token, loginresponse.Id, "the-integrationtests-integration1-res", fileInfo.FullName,"");
                Console.WriteLine(response.ToString());
            }
        }
    }
}
