using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Plugin.AudioRecorder;
using Xamarin.Forms;

namespace Pipes
{
    public static class AutoCorrelationFunction
    {
        public static EventHandler<DetectedFrequencyEventArgs> DetectedFrequencyHandler;

        static double GetFrequency(double[] data, int minLag, int maxLag)
        {
            double mean = Sum(data, 0, maxLag) / maxLag;
            double sum = Sum(data, minLag, maxLag); // sum used for rolling average
            double[] autoCorrelate = new double[maxLag - minLag]; // no need to calculate values we don't need
            double maxFreq = -1;
            int maxFreqLag = 0;
            for (int lag = minLag, nextLag = minLag + maxLag; lag < maxLag; sum += data[nextLag++] - data[lag++])
            {
                double covariance = 0, stdDevX = 0, stdDevY = 0;
                for (int i = 0; i < maxLag; i++)
                {
                    double x = (data[i] - mean);
                    double y = (data[i + lag] - sum / maxLag);
                    covariance += x * y;
                    stdDevX += x * x;
                    stdDevY += y * y;
                }
                autoCorrelate[lag - minLag] = covariance / (Math.Sqrt(stdDevX) * Math.Sqrt(stdDevY));
                // We're looking for the highest correlation
                if (autoCorrelate[lag - minLag] > maxFreq)
                {
                    maxFreq = autoCorrelate[lag - minLag];
                    maxFreqLag = lag;
                }
            }
            return 1.0 / maxFreqLag;
        }

        static double Sum(double[] data, int o, int n)
        {
            double result = 0;
            for (int i = o; i < n + o; i++)
            {
                result += data[i];
            }
            return result;
        }

        public static async Task AnalyseAudio(AudioRecorderService audioRecorder)
        {
            int sampleRate = audioRecorder.AudioStreamDetails.SampleRate;
            int channelCount = audioRecorder.AudioStreamDetails.ChannelCount;
            int bytesPerSample = audioRecorder.AudioStreamDetails.BitsPerSample / 8;
            int n = sampleRate / 8; // lowest organ note
            int min = sampleRate / 8192; // highest organ note?
            using (Stream s = audioRecorder.GetAudioFileStream())
            {
                int nBytes = 2 * n * channelCount * bytesPerSample; // number of bytes to extract
                byte[] bytes = new byte[nBytes];
                double[] data = new double[n * 2];
                while (audioRecorder.IsRecording)
                {
                    // If we haven't gotten a full sample and we're still recording, try again. Otherwise ignore incomplete sample.
                    for (int read = await s.ReadAsync(bytes, 0, nBytes); read < nBytes; read += await s.ReadAsync(bytes, read, nBytes - read))
                    {
                        if (!audioRecorder.IsRecording) { return; }
                    }

                    // assuming little endian for now
                    for (int i = 0; i < n * 2; i++)
                    {
                        data[i] = BitConverter.ToInt16(bytes, i * channelCount * bytesPerSample);
                    }
                    double freq = GetFrequency(data, min, n) * sampleRate;
                    Device.BeginInvokeOnMainThread(() => { DetectedFrequencyHandler?.Invoke(null, new DetectedFrequencyEventArgs(freq)); }); // This is probably terrible plz don't judge me

                    s.Seek(-nBytes / 2, SeekOrigin.Current);
                }
            }
        }
    }
    public class DetectedFrequencyEventArgs : EventArgs
    {
        public DetectedFrequencyEventArgs(double _freq)
        {
            freq = _freq;
        }
        private double freq;
        public double Frequency
        {
            get { return freq; }
        }
    }
}
