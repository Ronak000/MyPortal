using Microsoft.AspNetCore.Mvc;
using MyPortal.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using NAV;
using Microsoft.OData;
using System.Xml.Linq;

namespace MyPortal.Services
{
    public class UserServices
    {
        private static List<ClientUser> cachedUsers;
        private static string accessToken;

        public IConfiguration _configuration { get; }
        public IHttpContextAccessor _context { get; }

        public UserServices(IConfiguration configuration, IHttpContextAccessor context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task InitializeAsync()
        {
            var tenantId = _configuration.GetSection("AzureAD").GetValue<string>("TenantId");
            var clientId = _configuration.GetSection("AzureAD").GetValue<string>("ClientId");
            var clientSecret = _configuration.GetSection("AzureAd").GetValue<string>("ClientSecret");
            // Step 1: Get the access token
            accessToken = await TokenManager.GetAccessTokenAsync(tenantId, clientId, clientSecret);

        }

        private async Task<List<ClientUser>> FetchUsersFromService(string accessToken)
        {
            var BCCustomerLoginApiUrl = _configuration.GetSection("BusinessCentralServices").GetValue<string>("CustomerLoginUrl");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.GetAsync(BCCustomerLoginApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    // Console.WriteLine("Data from Business Central OData Service: " + data);
                    var users = JsonConvert.DeserializeObject<ODataResponse<ClientUser>>(data);
                    return users.Value;
                }
                else
                {
                    Console.WriteLine("Failed to get data: " + response.StatusCode);
                    return null;
                }
            }
        }

        public async Task<IActionResult> LoginUser(string email, string password)
        {
            // Step 2: Fetch the users from the OData service and store them in memory
            cachedUsers = await FetchUsersFromService(accessToken);
            // Step 3: Check if the email and password match a user in the cached data
            var user = cachedUsers.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user != null)
            {
                // if (user.Temporary == false)
                // {
                // Call the web service to get the user's details using the unique UserId
                var UserDetails = await GetUserDetailsFromWebService(user.No.ToString());

                if (UserDetails != null)
                {
                    // Pass the user details to the view
                    return new OkObjectResult(UserDetails);
                }
                else
                {
                    return new BadRequestObjectResult("Failed to retrieve user details two");

                }
                // }
                // else
                // {

                // }
            }
            else
            {
                return new BadRequestObjectResult("User not found");
            }
        }
        private async Task<IActionResult> GetUserDetailsFromWebService(string UniqueNo)
        {
            // Set up the request (you may need to add authentication tokens here)
            string CustomerDetailsUrl = _configuration.GetSection("BusinessCentralServices").GetValue<string>("CustomerDetailsUrl");
            string FilterUrl = $"{CustomerDetailsUrl}/?$filter=No eq '{UniqueNo}'";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                HttpResponseMessage response = await client.GetAsync(FilterUrl);

                if (response.IsSuccessStatusCode)
                {
                    var ResponseData = await response.Content.ReadAsStringAsync();
                    var CustomerData = JsonDocument.Parse(ResponseData);
                    var Customer = CustomerData.RootElement.GetProperty("value");
                    // Console.WriteLine(Customer);      
                    return new OkObjectResult(Customer);
                }
                else
                {
                    // Handle failure
                    Console.WriteLine("Failed to retrieve user details one: " + response.StatusCode);
                    return null;
                }
            }
        }
        public async Task<bool> CheckUSerExist(string email)
        {
            cachedUsers = await FetchUsersFromService(accessToken);
            var IsUserExist = cachedUsers.Any(x => x.Email == email);
            return IsUserExist;
        }

        public async Task<bool> GetTempPassword(string Email)
        {

            var BCCustomerLoginApiUrl = _configuration.GetSection("BusinessCentralServices").GetValue<string>("ChangePassword");
            string SOAPAction = "urn:microsoft-dynamics-schemas/codeunit/CP_Functionality_WS:GenerateTempPassword";
            var SoapXml = $@"
                    <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:urn='urn:microsoft-dynamics-schemas/codeunit/CP_Functionality_WS'>
                    <soapenv:Header/>
                    <soapenv:Body>
                        <urn:GenerateTempPassword>
                            <urn:email_iTxt>{Email}</urn:email_iTxt>
                        </urn:GenerateTempPassword>
                    </soapenv:Body>
                    </soapenv:Envelope>";


            using (HttpClient client = new HttpClient())
            {
                var Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(BCCustomerLoginApiUrl),
                    Content = new StringContent(SoapXml, Encoding.UTF8, "text/xml")
                };
                Request.Headers.Add("SOAPAction", SOAPAction);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage Response = await client.SendAsync(Request);

                if (Response.IsSuccessStatusCode)
                {
                    var Data = await Response.Content.ReadAsStringAsync();
                    var RootElement = XElement.Parse(Data);
                    var IsSent = bool.Parse(RootElement.Value);
                    return IsSent;
                }
                else
                {
                    Console.WriteLine("Error while sending temporary password:" + Response.IsSuccessStatusCode);
                    return false;
                }
            }

        }

        public async Task<bool> GetChangePassword(string email, string password)
        {
            var BCCustomerLoginApiUrl = _configuration.GetSection("BusinessCentralServices").GetValue<string>("ChangePassword");
            string SOAPAction = "urn:microsoft-dynamics-schemas/codeunit/CP_Functionality_WS:ChangePassword";
            var SoapXml = $@"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' 
                            xmlns:urn='urn:microsoft-dynamics-schemas/codeunit/CP_Functionality_WS'>
                                <soapenv:Header/>
                                <soapenv:Body>
                                    <urn:ChangePassword>
                                        <urn:email_iTxt>{email}</urn:email_iTxt>
                                        <urn:newPass>{password}</urn:newPass>
                                    </urn:ChangePassword>
                                </soapenv:Body>
                            </soapenv:Envelope>";
            using (HttpClient client = new HttpClient())
            {
                var Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(BCCustomerLoginApiUrl),
                    Content = new StringContent(SoapXml, Encoding.UTF8, "text/xml")
                };
                Request.Headers.Add("SOAPAction", SOAPAction);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage Response = await client.SendAsync(Request);

                if (Response.IsSuccessStatusCode)
                {
                    var Data = await Response.Content.ReadAsStringAsync();
                    var RootElement = XElement.Parse(Data);
                    var IsSent = bool.Parse(RootElement.Value);
                    return IsSent;
                }
                else
                {
                    Console.WriteLine("Error while sending temporary password:" + Response.IsSuccessStatusCode);
                    return false;
                }
            }

        }
    }
}
