using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LUC.Services.Implementation;
using Newtonsoft.Json;
using LightClient;

namespace PoC_client
{
    public class ConsoleApiClient
    {
        #region Fields

        private const string Login = "integration1";
        private const string Password = "integration1";
        private const string Host = "https://lightupon.cloud";
        private static readonly UserSetting userSetting;
        private readonly ApiSettings apiSettings;

        private WebClient client;

        #endregion
        private static CurrentUserProvider currentUserProvider = new CurrentUserProvider();
        #region Properties

        public CurrentUserProvider CurrentUserProvider { get; set; }

        [Import(typeof(PathFiltrator))]
        private PathFiltrator PathFiltrator { get; set; }

        private readonly Action currentUploadChunkAction;
        #endregion

        #region Constructors

        [ImportingConstructor]
        public ConsoleApiClient(CurrentUserProvider currentUserProvider)
        {
            apiSettings = new ApiSettings();
            CurrentUserProvider = currentUserProvider;
        }

        public ConsoleApiClient()
        {
        }

        #endregion


        private static async Task Main(string[] args)
        {
            ConsoleApiClient consoleApiClient = new ConsoleApiClient();
            var loginresponse = await LoginAsync(Login, Password);
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
                        await LogoutAsync();
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

            async Task<LoginResponse> LoginAsync(string login, string password)
            {
                CurrentUserProvider currentUserProvider = new CurrentUserProvider();
                ApiSettings apiSettings = new ApiSettings();
                try
                {
                    using (var client = new RepeatableHttpClient())
                    {
                        var stringContent = JsonConvert.SerializeObject(new LoginRequest
                        {
                            Login = "integration1",
                            Password = "integration1"
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
                            this.client.Headers.Add(HttpRequestHeader.Authorization, "Token " + apiSettings.AccessToken);

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
                var result = new LogoutResponse();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(GetLogoutUri(apiSettings.Host));

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
            private static string PostLoginUri(string host)
            {
                var result = Combine(host, "riak", "login");

                return result;
            }

            public static string GetLogoutUri(string host)
            {
                var result = Combine(host, "riak", "logout");

                return result;
            }

            public static string Combine(params string[] uri)
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
}