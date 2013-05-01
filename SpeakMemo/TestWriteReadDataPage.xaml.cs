using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace SpeakMemo
{
    public partial class TestWriteReadDataPage : PhoneApplicationPage
    {
        PlayBufferProcess play = new PlayBufferProcess();
        RecordBufferProcess record = new RecordBufferProcess();
        public TestWriteReadDataPage()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            
            string text = textWriteData.Text;
            //play.Play(null, 2);
            string[] s = text.Split(';');
            byte[] b = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                b[i] = Convert.ToByte(s[i]);
            }
            play.Play(b, (uint)b.Length);
            
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if ("StopReadData".Equals(button2.Content))
            {
                record.StopRecord();
                byte[] finalData = record.GetFinalData();
                textReadData.Text = Common.ByteArrayToString(finalData);
                button2.Content = "StartReadData";
            }
            else
            {
                record.StartRecord();
                button2.Content = "StopReadData";

            }
            
        }
    }
}