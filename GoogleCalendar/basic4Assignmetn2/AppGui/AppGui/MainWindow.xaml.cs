﻿using System;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using mmisharp;
using Newtonsoft.Json;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;

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
        private Tts t = new Tts();

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

            String startDate = start.DateTime.ToString().Split(' ')[0];
            String startTime = start.DateTime.ToString().Split(' ')[1];
            Console.WriteLine(  "Summary: " + summary +
                                "\nLocation: " + location +
                                "\nDescription: " + desc + 
                                "\nStart: " + start.DateTime.ToString() +
                                "\nEnd: " + end.Date);
            t.Speak("Evento " + translateSummary(summary) + " criado no dia " + startDate + " às " + startTime);
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
            t.Speak("Evento cancelado");
        }

        private void postponeEvent(String id, string[] date)
        {
            Console.WriteLine(id);
            Event ev = service.Events.Get("primary", id).Execute();
            DateTime s = (DateTime) ev.Start.DateTime;
            DateTime e = (DateTime)ev.End.DateTime;
            EventDateTime new_start = new EventDateTime()
            {
                DateTime = new DateTime(2020, Int32.Parse(date[1]), Int32.Parse(date[0]), s.Hour, s.Minute, 00),
                TimeZone = "Europe/Lisbon",
            };
            EventDateTime new_end = new EventDateTime()
            {
                DateTime = new DateTime(2020, Int32.Parse(date[1]), Int32.Parse(date[0]), e.Hour, e.Minute, 00),
                TimeZone = "Europe/Lisbon",
            };
            ev.Start = new_start;
            ev.End = new_end;
            service.Events.Update(ev, "primary", id).Execute();

        }

        private String translateSummary(String summary)
        {
            String str = "";
            switch(summary)
            {
                case "DINNER":
                    str = "jantar";
                    break;
                case "MEETING":
                    str = "reunião";
                    break;
                case "LUNCH":
                    str = "almoço";
                    break;
                case "PARTY":
                    str = "festa";
                    break;
            }
            return str;
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
                    string[] starthour = ((string)json.recognized[3].ToString()).Split(':');
                    EventDateTime start = new EventDateTime()
                    {
                        DateTime = new DateTime(2020, Int32.Parse(date[1]), Int32.Parse(date[0]), Int32.Parse(starthour[0]), Int32.Parse(starthour[1]), 00),
                        TimeZone = "Europe/Lisbon",
                    };
                    String endhour_s = ((string)json.recognized[4].ToString());
                    int[] endhour = new int[2];
                    if(endhour_s == "NO_HOUR")
                    {
                        endhour[0] = Int32.Parse(starthour[0]) + 1;
                        endhour[1] = Int32.Parse(starthour[1]) + 1;
                    }
                    else
                    {
                        endhour = Array.ConvertAll(endhour_s.Split(':'), int.Parse) ;
                    }
                    EventDateTime end = new EventDateTime()
                    {
                        DateTime = new DateTime(2020, Int32.Parse(date[1]), Int32.Parse(date[0]), endhour[0], endhour[1], 00),
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

                                t.Speak("Evento" + translateSummary(eventItem.Summary) + "no dia" + when.Split(' ')[0] + "às" + when.Split(' ')[1]);
                                Thread.Sleep(5000);
                            }
                            
                        }
                    }
                    else
                    {
                        Console.WriteLine("No upcoming events found.");
                        t.Speak("Não tem eventos marcados.");
                    }
                    
                    break;
                case "CANCEL_EVENT":
                    string[] date2 = ((string)json.recognized[2].ToString()).Split(' ');
                    string[] starthour2 = ((string)json.recognized[3].ToString()).Split(':');
                    DateTime start2 = new DateTime(2020, Int32.Parse(date2[1]), Int32.Parse(date2[0]), Int32.Parse(starthour2[0]), Int32.Parse(starthour2[1]), 00);
                    String eventID = getEventId((string)json.recognized[1].ToString(), start2);

                    Console.WriteLine(eventID);
                    String cancelDate = start2.ToString().Split(' ')[0];
                    if (eventID != "NO_EVENT")
                    {
                        cancelEvent(eventID);
                        t.Speak("Evento" + translateSummary(json.recognized[1].ToString()) + "no dia" + cancelDate + "cancelado.");
                    }
                    
                    break;
                case "AVAIL_DAY":
                    Events avail_events = getNextEvents(100);
                    string[] date3 = ((string)json.recognized[1].ToString()).Split(' ');
                    DateTime start3 = new DateTime(2020, Int32.Parse(date3[1]), Int32.Parse(date3[0]), 00, 00, 00);
                    DateTime end3 = new DateTime(2020, Int32.Parse(date3[1]), Int32.Parse(date3[0]), 23, 59, 59);
                    bool avail = true;
                    if (avail_events.Items != null && avail_events.Items.Count > 0)
                    {
                        foreach (var eventItem in avail_events.Items)
                        {
                            string when = eventItem.Start.DateTime.ToString();
                            if (String.IsNullOrEmpty(when))
                            {
                                when = eventItem.Start.Date;
                            }
                            if ((DateTime.Compare((DateTime)(eventItem.Start.DateTime), start3) > 0) && (DateTime.Compare((DateTime)(eventItem.Start.DateTime), end3) < 0))
                            {
                                avail = false;
                                break;
                            }

                        }
                    }
                    if (avail)
                    {
                        t.Speak("Sim, não tem nenhum evento marcado neste dia.");
                    }
                    else
                    {
                        t.Speak("Tem eventos marcados neste dia.");
                    }
                    break;
                case "POSTPONE_EVENT":
                    string[] date4 = ((string)json.recognized[2].ToString()).Split(' ');
                    string[] starthour4 = ((string)json.recognized[3].ToString()).Split(':');
                    string[] new_date4 = ((string)json.recognized[4].ToString()).Split(' ');
                    DateTime start4 = new DateTime(2020, Int32.Parse(date4[1]), Int32.Parse(date4[0]), Int32.Parse(starthour4[0]), Int32.Parse(starthour4[1]), 00);
                    
                    String eventID4 = getEventId((string)json.recognized[1].ToString(), start4);
                    if (eventID4 != "NO_EVENT")
                    {
                        postponeEvent(eventID4, new_date4);
                    }
                    break;
                case "CANCEL_EVENTS_DAY":
                    string[] date5 = ((string)json.recognized[1].ToString()).Split(' ');
                    DateTime start5 = new DateTime(2020, Int32.Parse(date5[1]), Int32.Parse(date5[0]), 00, 00, 00);
                    DateTime end5 = new DateTime(2020, Int32.Parse(date5[1]), Int32.Parse(date5[0]), 23, 59, 59);
                    Events events5 = getNextEvents(100);
                    int count = 0;
                    if (events5.Items != null && events5.Items.Count > 0)
                    {
                        foreach (var eventItem in events5.Items)
                        {
                            string when = eventItem.Start.DateTime.ToString();
                            if (String.IsNullOrEmpty(when))
                            {
                                when = eventItem.Start.Date;
                            }
                            if ((DateTime.Compare((DateTime)(eventItem.Start.DateTime), start5) > 0) && (DateTime.Compare((DateTime)(eventItem.Start.DateTime), end5) < 0))
                            {
                                cancelEvent(eventItem.Id);
                                count++;
                            }

                        }
                    }
                    if(count == 1)
                    {
                        t.Speak("Foi cancelado um evento");
                    }
                    else if(count > 1)
                    {
                        t.Speak("Foram cancelados " + count + " eventos");
                    }
                    else
                    {
                        t.Speak("Não tinha eventos nesse dia");
                    }
                    
                    break;
            }



        }
    }
}
