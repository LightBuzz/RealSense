using System;
using System.Threading;
using Intel.RealSense;
using System.Linq;

namespace LightBuzz.RealSense
{
    /// <summary>
    /// Manages streaming using a RealSense Device
    /// </summary>
    public class RealSenseDevice
    {
        #region Constants

        private readonly AutoResetEvent _stopEvent = new AutoResetEvent(false);

        #endregion

        #region Members
        
        private Pipeline _pipeline;
        private Align _aligner;
        private Thread _worker;

        private FrameData _frameData;

        #endregion

        #region Events

        /// <summary>
        /// Raised when updated color and depth data are available.
        /// </summary>
        public event Action<FrameData> OnFrameDataArrived;

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether the device is streaming frames.
        /// </summary>
        public bool Streaming { get; protected set; }

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

            using (var cfg = DeviceConfiguration.ToPipelineConfig())
            {
                ActiveProfile = _pipeline.Start(cfg);
            }

            using (var activeStreams = ActiveProfile.Streams)
            {
                DeviceConfiguration.Profiles = activeStreams.Select(VideoStreamRequest.FromProfile).ToArray();
            }

            _aligner = new Align(Stream.Depth);

            _frameData = new FrameData
            {
                ColorData = new byte[DeviceConfiguration.ColorProfile.Width * DeviceConfiguration.ColorProfile.Height * 3],
                DepthData = new ushort[DeviceConfiguration.DepthProfile.Width * DeviceConfiguration.DepthProfile.Height]
            };
                        
            CoordinateMapper = CoordinateMapper.Create(this);

            _stopEvent.Reset();

            _worker = new Thread(WaitForFrames)
            {
                IsBackground = true
            };
            _worker.Start();

            Streaming = true;
        }

        /// <summary>
        /// Closes the sensor and disposes any resources.
        /// </summary>
        public void Close()
        {
            OnFrameDataArrived = null;

            if (_worker != null)
            {
                _stopEvent.Set();
                _worker.Join();
            }

            if (_pipeline != null)
            {
                if (Streaming)
                {
                    _pipeline.Stop();
                }

                _pipeline.Release();
                _pipeline = null;
            }

            Streaming = false;

            if (ActiveProfile != null)
            {
                ActiveProfile.Dispose();
                ActiveProfile = null;
            }

            if (_pipeline != null)
            {
                _pipeline.Release();
            }

            _pipeline = null;
        }

        #endregion

        #region Private methods

        private void WaitForFrames()
        {
            while (!_stopEvent.WaitOne(0))
            {
                using (FrameSet set = _pipeline.WaitForFrames())
                {
                    //OnFrameSetArrived?.Invoke(set);

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

    public class FrameData
    {
        public byte[] ColorData { get; set; }

        public ushort[] DepthData { get; set; }
    }
}
