using System;
using System.Threading;
using Intel.RealSense;
using System.Linq;
using System.Collections.Generic;

namespace LightBuzz.RealSense
{
    /// <summary>
    /// Manages streaming using a RealSense Device
    /// </summary>
    public class RealSenseDevice
    {
        #region Constants

        private readonly AutoResetEvent _streamingEvent = new AutoResetEvent(false);

        #endregion

        #region Members
        
        private Pipeline _pipeline;
        private Align _aligner;
        private Thread _streamingThread;
        private AlignedFrameData _frameData;

        #endregion

        #region Events

        /// <summary>
        /// Raised when updated color and depth data are available.
        /// </summary>
        public event Action<AlignedFrameData> OnFrameDataArrived;

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether the device is open and streaming frames.
        /// </summary>
        public bool IsOpen { get; protected set; }

        /// <summary>
        /// The active profile of the RealSense camera.
        /// </summary>
        public PipelineProfile ActiveProfile { get; protected set; }

        /// <summary>
        /// The current device configuration.
        /// </summary>
        public DeviceConfiguration DeviceConfiguration { get; set; }

        /// <summary>
        /// The coordinate mapper of the current device.
        /// </summary>
        public CoordinateMapper CoordinateMapper { get; protected set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Opens the RealSense device.
        /// </summary>
        public void Open()
        {
            _pipeline = new Pipeline();

            if (DeviceConfiguration == null)
            {
                DeviceConfiguration = DeviceConfiguration.Default();
            }

            using (Config cfg = DeviceConfiguration.ToPipelineConfig())
            {
                ActiveProfile = _pipeline.Start(cfg);
            }

            _aligner = new Align(Stream.Depth);

            _frameData = new AlignedFrameData
            {
                Timestamp = DateTime.Now,
                ColorData = new byte[DeviceConfiguration.ColorProfile.Width * DeviceConfiguration.ColorProfile.Height * 3],
                DepthData = new ushort[DeviceConfiguration.DepthProfile.Width * DeviceConfiguration.DepthProfile.Height]
            };
                        
            CoordinateMapper = CoordinateMapper.Create(this);

            _streamingEvent.Reset();
            _streamingThread = new Thread(WaitForFrames)
            {
                IsBackground = true
            };
            _streamingThread.Start();

            IsOpen = true;
        }

        /// <summary>
        /// Closes the sensor and disposes any resources.
        /// </summary>
        public void Close()
        {
            OnFrameDataArrived = null;

            if (_streamingThread != null)
            {
                _streamingEvent.Set();
                _streamingThread.Join();
            }

            if (_pipeline != null)
            {
                if (IsOpen)
                {
                    _pipeline.Stop();
                }

                _pipeline.Release();
                _pipeline = null;
            }

            if (ActiveProfile != null)
            {
                ActiveProfile.Dispose();
                ActiveProfile = null;
            }

            if (_pipeline != null)
            {
                _pipeline.Release();
                _pipeline = null;
            }

            IsOpen = false;
        }

        #endregion

        #region Private methods

        private void WaitForFrames()
        {
            while (!_streamingEvent.WaitOne(0))
            {
                using (FrameSet set = _pipeline.WaitForFrames())
                {
                    _frameData.Timestamp = DateTime.Now;

                    using (VideoFrame colorFrame = set.ColorFrame)
                    {
                        colorFrame.CopyTo(_frameData.ColorData);
                    }
                    using (FrameSet processed = _aligner.Process(set))
                    using (DepthFrame depthFrame = processed.DepthFrame)
                    {
                        depthFrame.CopyTo(_frameData.DepthData);
                    }

                    OnFrameDataArrived?.Invoke(_frameData);
                }
            }
        }

        #endregion
    }
}
