using System;

namespace Zoom_Meeting.Models
{
    public class Meeting
    {
        public string Id { get; set; }

        public string Topic { get; set; }

        public string Agenda { get; set; }

        public DateTime Date { get; set; }

        public double Time { get; set; }

        public int Duration { get; set; }
    }
}