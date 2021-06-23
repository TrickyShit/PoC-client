using System.Collections.Generic;
using LightClient;
using Newtonsoft.Json;

namespace PoC_client
{
    public class LoginRequest
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    public class LoginResponse : BaseResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("groups")]
        public List<GroupSubResponse> Groups { get; set; }

        [JsonProperty("tenant_Id")]
        public string TenantId { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("staff")] // TODO Server Sync naming and meaning.
        public bool IsAdmin { get; set; }
    }

    public class LogoutResponse : BaseResponse
    {
    }

    public class GroupSubResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}