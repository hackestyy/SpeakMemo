﻿using System;
using System.ComponentModel;
using System.Windows;

namespace SpeakMemo
{
    public class SpaceTime : INotifyPropertyChanged
    {
        static readonly string[] sizeUnits = { "bytes", "K", "M", "G", "T" };

        long space;
        TimeSpan time;

        public event PropertyChangedEventHandler PropertyChanged;

        public long Space
        {
            set
            {
                if (space != value)
                {
                    space = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Space"));
                    OnPropertyChanged(new PropertyChangedEventArgs("FormattedSpace"));
                }
            }
            get
            {
                return space;
            }
        }

        public string FormattedSpace
        {
            get { return GetFormattedFileSize(this.Space); }
        }

        public TimeSpan Time
        {
            set
            {
                if (time != value)
                {
                    time = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Time"));
                    OnPropertyChanged(new PropertyChangedEventArgs("FormattedTime"));
                }
            }
            get
            {
                return time;
            }
        }

        public string FormattedTime
        {
            get { return GetFormattedDuration(this.Time); }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, args);
        }

        public static string GetFormattedFileSize(long filesize)
        {
            int magnitude = 0;
            double display = 0;
            string format = "G";

            if (filesize > 0)
            {
                int base2log = (int)(Math.Log(filesize) / Math.Log(2));
                magnitude = base2log / 10;
                display = filesize / Math.Pow(2, 10 * magnitude);
                int sigDigits = 1 + (int)Math.Log10(display);
                format = "N" + Math.Max(0, 3 - sigDigits);
            }
            return display.ToString(format) + ' ' + sizeUnits[magnitude];
        }

        public static string GetFormattedDuration(TimeSpan Duration)
        {
            if (Duration.Days > 0)
            {
                int hours = Duration.Hours + (Duration.Minutes + Duration.Seconds / 30) / 30;

                return String.Format("{0} day{1} {2}hr{3}",
                                     Duration.Days, Duration.Days == 1 ? "" : "s",
                                     hours, hours == 1 ? "" : "s");
            }

            else if (Duration.Hours > 0)
            {
                int minutes = Duration.Minutes + Duration.Seconds / 30;

                return String.Format("{0} hr{1} {2} min{3}",
                                     Duration.Hours, Duration.Hours == 1 ? "" : "s",
                                     minutes, minutes == 1 ? "" : "s");
            }

            else if (Duration.Minutes > 0)
            {
                int seconds = (int)Math.Round(Duration.Seconds + Duration.Milliseconds / 1000.0);

                return String.Format("{0} min{1} {2} sec{3}",
                                     Duration.Minutes, Duration.Minutes == 1 ? "" : "s",
                                     seconds, seconds == 1 ? "" : "s");
            }

            else if (Duration.Seconds > 0)
            {
                double seconds = Math.Round(Duration.Seconds + Duration.Milliseconds / 1000.0,
                                            Duration.Seconds < 10 ? 2 : 1);

                return String.Format("{0} second{1}",
                                     seconds, seconds == 1 ? "" : "s");
            }

            return String.Format("{0} msecs", Duration.Milliseconds);
        }
    }
}
