using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PoC_client
{
    public class ConsoleApiClient
    {
        private const string Login = "integration1";
        private const string Password = "integration1";
        private const string Host = "https://lightupon.cloud";

        [Import(typeof(ICurrentUserProvider))]
        private static ICurrentUserProvider currentUserProvider = new CurrentUserProvider();

        private static SettingsService settingsService = new SettingsService();
        private ICurrentUserProvider currentUserProvider1;
        private static readonly UserSetting userSetting;
        public readonly ApiSettings apiSettings;
        private WebClient client;

        static ConsoleApiClient()
        {
            userSetting = settingsService.AppSettings.SettingsPerUser.Single((settings) => settings.Login == Login);
            currentUserProvider.RootFolderPath = userSetting.RootFolderPath;
        }

        public ConsoleApiClient(ICurrentUserProvider currentUserProvider1)
        {
            this.currentUserProvider1 = currentUserProvider1;
        }

        public static async Task Main(string[] args)
        {
            var consoleApiClient = new ConsoleApiClient(currentUserProvider);
            var loginresponse = await consoleApiClient.LoginAsync(Login, Password);
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
                        await consoleApiClient.LogoutAsync();
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
                var response = await lightClient.Upload(Host, loginresponse.Token, loginresponse.Id, "the-integrationtests-integration1-res", fileInfo.FullName, "");
                Console.WriteLine(response.ToString());
            }
        }

        private async Task<LoginResponse> LoginAsync(string login, string password)
        {
            CurrentUserProvider currentUserProvider = new CurrentUserProvider();
            ApiSettings apiSettings = new ApiSettings();
            try
            {
                using (var client = new HttpClient())
                {
                    var stringContent = JsonConvert.SerializeObject(new LoginRequest
                    {
                        Login = login,
                        Password = password
                    });

                    var content = new StringContent(stringContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(PostLoginUri(Host), content);

                    if (response.IsSuccessStatusCode)
                    {
                        LoginResponse result;

                        try
                        {
                            result = JsonConvert.DeserializeObject<LoginResponse>(
                                await response.Content.ReadAsStringAsync());

                            var model = result.ToLoginServiceModel();
                            currentUserProvider.SetLoggedUser(model);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.HelpLink, ex.Message);
                            return new LoginResponse
                            {
                                IsSuccess = false,
                                Message = "Can't read content from the response."
                            };
                        }

                        apiSettings.InitializeAccessToken(result.Token);

                        //InitOperations(apiSettings.AccessToken);   sync with server is not necessary for PoC-client

                        this.client = new WebClient();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiSettings.AccessToken);

                        Console.WriteLine($"Logged as '{result.Login}' at {DateTime.UtcNow.ToLongTimeString()} {DateTime.UtcNow.ToLongDateString()}");

                        return result;
                    }
                    else
                    {
                        var stringResult = await response.Content.ReadAsStringAsync();

                        Console.WriteLine($"Can't login: {stringResult}. Status code = {response.StatusCode}");

                        if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            return new LoginResponse
                            {
                                IsSuccess = false,
                                Message = "Wrong login or password"
                            };
                        }
                        else
                        {
                            return new LoginResponse
                            {
                                IsSuccess = false,
                                Message = string.Format("Can not login. Please contact our support team. Status code: {0}", response.StatusCode)
                            };
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    Message = "No connection at the moment..."
                };
            }
            catch (WebException)
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    Message = "No connection at the moment..."
                };
            }
            catch (SocketException)
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    Message = "No connection at the moment..."
                };
            }
        }

        private async Task<LogoutResponse> LogoutAsync()
        {
            var consoleApiClient = new ConsoleApiClient(currentUserProvider);
            var result = new LogoutResponse();
            ApiSettings apiSettings = new ApiSettings();

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(consoleApiClient.GetLogoutUri(Host));

                if (response.IsSuccessStatusCode)
                {
                    apiSettings.InitializeAccessToken(string.Empty);
                    return result;
                }
                else
                {
                    var stringResult = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Can't logout: " + stringResult);

                    result.IsSuccess = false;
                    result.Message = stringResult;
                    return result;
                }
            }
        }

        private string PostLoginUri(string host)
        {
            var result = Combine(host, "riak", "login");

            return result;
        }

        private string GetLogoutUri(string host)
        {
            var result = Combine(host, "riak", "logout");

            return result;
        }

        private string Combine(params string[] uri)
        {
            uri[0] = uri[0].TrimEnd('/');
            string result = "";
            result += uri[0] + "/";
            for (var i = 1; i < uri.Length; i++)
            {
                uri[i] = uri[i].TrimStart('/');
                uri[i] = uri[i].TrimEnd('/');
                result += uri[i] + "/";
            }
            return result;
        }
    }
}