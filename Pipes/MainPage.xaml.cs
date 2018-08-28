using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.AudioRecorder;
using System.Diagnostics;

namespace Pipes
{
    public partial class MainPage : ContentPage
    {
        AudioRecorderService audioRecorder = new AudioRecorderService()
        {
            StopRecordingAfterTimeout = true,
            TotalAudioTimeout = new TimeSpan(0, 0, 1)
        };


        public MainPage()
        {
            InitializeComponent();
            AutoCorrelationFunction.DetectedFrequencyHandler += DisplayFrequency;
        }

        public async void RecordAsync_Click(object sender, EventArgs e)
        {
            if (!audioRecorder.IsRecording) {
                Debug.WriteLine("Record button clicked");
                await audioRecorder.StartRecording();
                Debug.WriteLine(audioRecorder.AudioStreamDetails.SampleRate);
                Debug.WriteLine(audioRecorder.AudioStreamDetails.ChannelCount);
                Debug.WriteLine(audioRecorder.AudioStreamDetails.BitsPerSample);
                await Task.Run(() => AutoCorrelationFunction.AnalyseAudio(audioRecorder)); 
            }
        }

        public void DisplayFrequency(object sender, DetectedFrequencyEventArgs e) {
            freqLabel.Text = e.Frequency.ToString() + "Hz";
        }

    }
}
