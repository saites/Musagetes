// (c) Copyright Jacob Johnston.
// This source is subject to Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using NAudio.Dsp;

namespace Musagetes
{
    public class SampleAggregator
    {
        public float LeftMaxVolume { get; private set; }
        public float LeftMinVolume { get; private set; }
        public float RightMaxVolume { get; private set; }
        public float RightMinVolume { get; private set; }

        private readonly Complex[] _channelData;
        private readonly int _bufferSize;
        private readonly int _binaryExponentitation;
        private int _channelDataPosition;

        public SampleAggregator(int bufferSize)
        {
            _bufferSize = bufferSize;
            _binaryExponentitation = (int)Math.Log(bufferSize, 2);
            _channelData = new Complex[bufferSize];
        }

        public void Clear()
        {
            LeftMaxVolume = float.MinValue;
            RightMaxVolume = float.MinValue;
            LeftMinVolume = float.MaxValue;
            RightMinVolume = float.MaxValue;
            _channelDataPosition = 0;
        }

        /// <summary>
        /// Add a sample value to the aggregator.
        /// </summary>
        /// <param name="leftValue"></param>
        /// <param name="rightValue"></param>
        public void Add(float leftValue, float rightValue)
        {
            if (_channelDataPosition == 0)
            {
                LeftMaxVolume = float.MinValue;
                RightMaxVolume = float.MinValue;
                LeftMinVolume = float.MaxValue;
                RightMinVolume = float.MaxValue;
            }

            // Make stored channel data stereo by averaging left and right values.
            _channelData[_channelDataPosition].X = (leftValue + rightValue) / 2.0f;
            _channelData[_channelDataPosition].Y = 0;
            _channelDataPosition++;

            LeftMaxVolume = Math.Max(LeftMaxVolume, leftValue);
            LeftMinVolume = Math.Min(LeftMinVolume, leftValue);
            RightMaxVolume = Math.Max(RightMaxVolume, rightValue);
            RightMinVolume = Math.Min(RightMinVolume, rightValue);

            if (_channelDataPosition >= _channelData.Length)
            {
                _channelDataPosition = 0;
            }
        }

        /// <summary>
        /// Performs an FFT calculation on the channel data upon request.
        /// </summary>
        /// <param name="fftBuffer">A buffer where the FFT data will be stored.</param>
        public void GetFftResults(float[] fftBuffer)
        {
            var channelDataClone = new Complex[_bufferSize];
            _channelData.CopyTo(channelDataClone, 0);
            FastFourierTransform.FFT(true, _binaryExponentitation, channelDataClone);
            for (var i = 0; i < channelDataClone.Length / 2; i++)
            {
                // Calculate actual intensities for the FFT results.
                fftBuffer[i] = (float)Math.Sqrt(
                    channelDataClone[i].X * channelDataClone[i].X
                    + channelDataClone[i].Y * channelDataClone[i].Y);
            }
        }
    }
}