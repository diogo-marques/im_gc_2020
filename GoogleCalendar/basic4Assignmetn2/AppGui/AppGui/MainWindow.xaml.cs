using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using mmisharp;
using Newtonsoft.Json;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MmiCommunication mmiC;

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/calendar-dotnet-quickstart.json
        static string[] Scopes = { CalendarService.Scope.Calendar };
        static string ApplicationName = "Google Calendar API .NET Quickstart";
        CalendarService service;

        public MainWindow()
        {
            
            UserCredential credentials;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Calendar API service.
            service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = ApplicationName,
            });
            
            InitializeComponent();


            mmiC = new MmiCommunication("localhost",8000, "User1", "GUI");
            mmiC.Message += MmiC_Message;
            mmiC.Start();

        }

        private void createEvent(CalendarService service, String summary, String location, EventDateTime start, EventDateTime end, String desc)
        {
            // Refer to the .NET quickstart on how to setup the environment:
            // https://developers.google.com/calendar/quickstart/dotnet
            // Change the scope to CalendarService.Scope.Calendar and delete any stored
            // credentials.
            Event newEvent = new Event()
            {
                Summary = summary,
                Location = location,
                Description = desc,
                Start = start,
                End = end,
            };
            String calendarId = "primary";
            EventsResource.InsertRequest request = service.Events.Insert(newEvent, calendarId);
            Event createdEvent = request.Execute();
            Console.WriteLine("Event created: {0}", createdEvent.HtmlLink);
        }

        private Events getNextEvents(int maxResults)
        {
            // Define parameters of request.
            
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = maxResults;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = request.Execute();

            return events;
        }

        private String getEventId(String summary, DateTime start)
        {
            Events events = getNextEvents(100);
            if (events.Items != null && events.Items.Count > 0)
            {
                Console.WriteLine("Upcoming events:");
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    if ((DateTime.Compare((DateTime)(eventItem.Start.DateTime), start) == 0) && eventItem.Summary == summary)
                    {
                        return eventItem.Id;
                    }

                }
            }
            return "NO_EVENT";
        }

        private void cancelEvent(String id)
        {
            service.Events.Delete("primary", id).Execute();
        }

        private void MmiC_Message(object sender, MmiEventArgs e )
        {
            Console.WriteLine(e.Message);
            var doc = XDocument.Parse(e.Message);
            var com = doc.Descendants("command").FirstOrDefault().Value;
            dynamic json = JsonConvert.DeserializeObject(com);

            Console.WriteLine(json);
            
            switch ((string)json.recognized[0].ToString())
            {
                case "CREATE_EVENT":
                    string[] date = ((string)json.recognized[2].ToString()).Split(' ');
                    EventDateTime start = new EventDateTime()
                    {
                        DateTime = new DateTime(2020, Int32.Parse(date[1]), Int32.Parse(date[0]), 10, 00, 00),
                        TimeZone = "Europe/Lisbon",
                    };
                    EventDateTime end = new EventDateTime()
                    {
                        DateTime = new DateTime(2020, Int32.Parse(date[1]), Int32.Parse(date[0]), 11, 00, 00),
                        TimeZone = "Europe/Lisbon",
                    };
                    createEvent(service, (string)json.recognized[1].ToString(), "Aveiro", start, end, " ");
                    break;
                case "LIST_EVENTS":
                    string[] date1 = ((string)json.recognized[1].ToString()).Split(' ');
                    DateTime start1 = new DateTime(2020, Int32.Parse(date1[1]), Int32.Parse(date1[0]), 00, 00, 00);
                    DateTime end1 = new DateTime(2020, Int32.Parse(date1[1]), Int32.Parse(date1[0]), 23, 59, 59);
                    Events events = getNextEvents(100);
                    if (events.Items != null && events.Items.Count > 0)
                    {
                        Console.WriteLine("Upcoming events:");
                        foreach (var eventItem in events.Items)
                        {
                            string when = eventItem.Start.DateTime.ToString();
                            if (String.IsNullOrEmpty(when))
                            {
                                when = eventItem.Start.Date;
                            }
                            if((DateTime.Compare( (DateTime)(eventItem.Start.DateTime), start1)>0) && (DateTime.Compare((DateTime)(eventItem.Start.DateTime), end1) < 0))
                            {
                                Console.WriteLine("{0} ({1})", eventItem.Summary, when);
                            }
                            
                        }
                    }
                    else
                    {
                        Console.WriteLine("No upcoming events found.");
                    }
                    
                    break;
                case "CANCEL_EVENT":
                    string[] date2 = ((string)json.recognized[2].ToString()).Split(' ');
                    DateTime start2 = new DateTime(2020, Int32.Parse(date2[1]), Int32.Parse(date2[0]), 10, 00, 00);
                    String eventID = getEventId((string)json.recognized[1].ToString(), start2);
                    if(eventID != "NO_EVENT")
                    {
                        cancelEvent(eventID);
                    }
                    break;
            }



        }
    }
}
