using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using Zoom_Meeting.Models;

namespace Zoom_Meeting.Controllers
{
    public class ZoomMeetingController : Controller
    {

        public ActionResult Meeting(string identifier)
        {
            var token = JObject.Parse(ConfigurationManager.AppSettings["TokenFilePath"]);

            RestClient restClient = new RestClient();
            RestRequest restRequest = new RestRequest();

            restRequest.AddHeader("Authorization", "Bearer " + token["access_token"]);

            restClient.BaseUrl = new Uri($"https://api.zoom.us/v2/meetings/{identifier}");
            var response = restClient.Get(restRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var zoomMeeting = JObject.Parse(response.Content).ToObject<ZoomMeeting>();
                zoomMeeting.Start_Time = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(zoomMeeting.Start_Time.Ticks, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                return View(zoomMeeting);
            }

            return View("Error");
        }

        public ActionResult AllMeetings()
        {
            var token = JObject.Parse(System.IO.File.ReadAllText(ConfigurationManager.AppSettings["TokenFilePath"]));
            var userDetails = JObject.Parse(System.IO.File.ReadAllText(ConfigurationManager.AppSettings["UserDetailsPath"]));
            var access_token = token["access_token"];
            var userId = userDetails["id"];

            RestClient restClient = new RestClient();
            RestRequest restRequest = new RestRequest();
            restRequest.AddHeader("Authorization", "Bearer " + access_token);

            restClient.BaseUrl = new Uri($"https://api.zoom.us/v2/users/{userId}/meetings");
            var response = restClient.Get(restRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var zoomMeetings = JObject.Parse(response.Content)["meetings"].ToObject<IEnumerable<ZoomMeeting>>();
                foreach (ZoomMeeting meeting in zoomMeetings)
                {
                    meeting.Start_Time = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(meeting.Start_Time.Ticks, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                }

                return View(zoomMeetings);
            }

            return View("Error");
        }

        [HttpPost]
        public ActionResult CreateMeeting(Meeting meeting)
        {
            var token = JObject.Parse(System.IO.File.ReadAllText(ConfigurationManager.AppSettings["TokenFilePath"]));
            var userDetails = JObject.Parse(System.IO.File.ReadAllText(ConfigurationManager.AppSettings["UserDetailsPath"]));
            var access_token = token["access_token"];
            var userId = userDetails["id"];

            var meetingModel = new JObject();
            meetingModel["topic"] = meeting.Topic;
            meetingModel["agenda"] = meeting.Agenda;
            meetingModel["start_time"] = meeting.Date.ToString("yyyy-MM-dd") + "T" + TimeSpan.FromHours(meeting.Time).ToString("hh':'mm':'ss");
            meetingModel["duration"] = meeting.Duration;

            var model = JsonConvert.SerializeObject(meetingModel);

            RestClient restClient = new RestClient();
            RestRequest restRequest = new RestRequest();

            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Authorization", string.Format("Bearer {0}", access_token));
            restRequest.AddParameter("application/json", model, ParameterType.RequestBody);

            restClient.BaseUrl = new Uri(string.Format(ConfigurationManager.AppSettings["MeetingUrl"], userId));
            var response = restClient.Post(restRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                System.IO.File.WriteAllText(ConfigurationManager.AppSettings["MeetingResponsePath"], response.Content);
                return RedirectToAction("Index", "Home");
            }

            return View("Error");
        }

        [HttpGet]
        public ActionResult UpdateMeeting(string identifier)
        {
            var token = JObject.Parse(System.IO.File.ReadAllText("C:/Users/Purshotam/Desktop/Zoom Meeting/Zoom Meeting/Credentials/OauthToken.json"));
            var access_token = token["access_token"];

            RestClient restClient = new RestClient();
            RestRequest restRequest = new RestRequest();

            restRequest.AddHeader("Authorization", "Bearer " + access_token);

            restClient.BaseUrl = new Uri($"https://api.zoom.us/v2/meetings/{identifier}");
            var response = restClient.Get(restRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var zoomMeeting = JObject.Parse(response.Content).ToObject<ZoomMeeting>();
                DateTime utcTime = new DateTime(zoomMeeting.Start_Time.Ticks, DateTimeKind.Unspecified);
                var datetime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                var meeting = new Meeting()
                {
                    Id = zoomMeeting.Id,
                    Topic = zoomMeeting.Topic,
                    Agenda = zoomMeeting.Agenda,
                    Date = zoomMeeting.Start_Time.Date,
                    Time = double.Parse(TimeZoneInfo.ConvertTimeFromUtc(new DateTime(zoomMeeting.Start_Time.Ticks, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).ToString("H.m")),
                    Duration = zoomMeeting.Duration,
                };

                return View(meeting);
            }

            return View("Error");
        }

        [HttpPost]
        public ActionResult UpdateZoomMeeting(string identifier, Meeting meeting)
        {
            var token = JObject.Parse(System.IO.File.ReadAllText("C:/Users/Purshotam/Desktop/Zoom Meeting/Zoom Meeting/Credentials/OauthToken.json"));
            var access_token = token["access_token"];

            var meetingModel = new JObject();
            if (!string.IsNullOrWhiteSpace(meeting.Topic))
            {
                meetingModel["topic"] = meeting.Topic;
            }
            if (!string.IsNullOrWhiteSpace(meeting.Agenda))
            {
                meetingModel["agenda"] = meeting.Agenda;
            }
            if (meeting.Date > DateTime.UtcNow && meeting.Time > 0)
            {
                meetingModel["start_time"] = meeting.Date.ToString("yyyy-MM-dd") + "T" + TimeSpan.FromHours(meeting.Time).ToString("hh':'mm':'ss");
            }
            if (meeting.Duration > 0)
            {
                meetingModel["duration"] = meeting.Duration;
            }

            var model = JsonConvert.SerializeObject(meetingModel);

            RestClient restClient = new RestClient();
            RestRequest restRequest = new RestRequest();

            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Authorization", "Bearer " + access_token);
            restRequest.AddParameter("application/json", model, ParameterType.RequestBody);

            restClient.BaseUrl = new Uri($"https://api.zoom.us/v2/meetings/{identifier}");
            var response = restClient.Patch(restRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return RedirectToAction("AllMeetings", "ZoomMeeting");
            }

            return View("Error");
        }

        public ActionResult DeleteMeeting(string identifier)
        {
            var token = JObject.Parse(System.IO.File.ReadAllText("C:/Users/Purshotam/Desktop/Zoom Meeting/Zoom Meeting/Credentials/OauthToken.json"));
            var access_token = token["access_token"];

            RestClient restClient = new RestClient();
            RestRequest restRequest = new RestRequest();

            restRequest.AddHeader("Authorization", "Bearer " + access_token);

            restClient.BaseUrl = new Uri($"https://api.zoom.us/v2/meetings/{identifier}");
            var response = restClient.Delete(restRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return RedirectToAction("AllMeetings", "ZoomMeeting");
            }

            return RedirectToAction("Error");
        }
    }
}