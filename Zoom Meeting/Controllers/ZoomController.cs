using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Configuration;
using System.Web.Mvc;

namespace Zoom_Meeting.Controllers
{
    public class ZoomController : Controller
    {
        private string AuthorizationHeader
        {
            get
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{ConfigurationManager.AppSettings["ClientId"]}:{ConfigurationManager.AppSettings["ClientSecret"]}");
                var encodedString = System.Convert.ToBase64String(plainTextBytes);
                return $"Basic {encodedString}";
            }
        }

        public ActionResult SignIn()
        {
            return Redirect(string.Format(ConfigurationManager.AppSettings["AuthorizationUrl"], ConfigurationManager.AppSettings["ClientId"], ConfigurationManager.AppSettings["RedirectUrl"]));
        }

        public ActionResult OAuthRedirect(string code)
        {
            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest();

            request.AddQueryParameter("grant_type", "authorization_code");
            request.AddQueryParameter("code", code);
            request.AddQueryParameter("redirect_uri", ConfigurationManager.AppSettings["RedirectUrl"]);
            request.AddHeader("Authorization", string.Format(AuthorizationHeader));

            restClient.BaseUrl = new Uri("https://zoom.us/oauth/token");
            var response = restClient.Post(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                System.IO.File.WriteAllText(ConfigurationManager.AppSettings["TokenFilePath"], response.Content);
                var token = JObject.Parse(response.Content);
                this.GetUserDetails(token["access_token"].ToString());
                return RedirectToAction("Index", "Home");
            }

            return View("Error");
        }

        public void GetUserDetails(string accessToken)
        {
            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest();

            request.AddHeader("Authorization", string.Format("Bearer {0}", accessToken));

            restClient.BaseUrl = new Uri("https://api.zoom.us/v2/users/me");
            var response = restClient.Get(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                System.IO.File.WriteAllText(ConfigurationManager.AppSettings["UserDetailsPath"], response.Content);
            }
        }

        public ActionResult RefreshToken()
        {
            var token = JObject.Parse(System.IO.File.ReadAllText(ConfigurationManager.AppSettings["TokenFilePath"]));

            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest();

            request.AddQueryParameter("grant_type", "refresh_token");
            request.AddQueryParameter("refresh_token", token["refresh_token"].ToString());
            request.AddHeader("Authorization", string.Format(AuthorizationHeader));

            restClient.BaseUrl = new Uri("https://zoom.us/oauth/token");
            var response = restClient.Post(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                System.IO.File.WriteAllText(ConfigurationManager.AppSettings["TokenFilePath"], response.Content);
                return RedirectToAction("Index", "Home");
            }

            return View("Error");
        }
    }
}