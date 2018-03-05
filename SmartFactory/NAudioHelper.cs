using NAudio.Mixer;
using NAudio.Wave.SampleProviders;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Gui;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SmartFactory
{
    class NAudioHelper
    {
        private WaveIn waveSource = null;

        private List<byte> pcmData = null;

        public Action<byte[]> PcmDataAvailable;
        /// <summary>
        /// 开始录音
        /// </summary>
        public void StartRec()
        {
            waveSource = new WaveIn();
            pcmData = new List<byte>();
            waveSource.WaveFormat = new WaveFormat(16000, 16, 1); // 16bit,16KHz,Mono的录音格式
            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);
            waveSource.StartRecording();
           
        }

        /// <summary>
        /// 停止录音
        /// </summary>
        public void StopRec()
        {
            waveSource?.StopRecording();
            // Close Wave(Not needed under synchronous situation)
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }
        }

        /// <summary>
        /// 开始录音回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            PcmDataAvailable?.Invoke(e.Buffer);
        }

        /// <summary>
        /// 录音结束回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }
        }

        /// <summary>
        /// 播放PCM音频
        /// </summary>
        /// <param name="pcmData"></param>
        public void Play(byte[] pcmData)
        {
            var waveOut = new WaveOut();
            waveOut.Init(new RawSourceWaveStream(pcmData, 0, pcmData.Length, new WaveFormat(16000, 16, 1)));
            waveOut.Play();
        }
    }
}
