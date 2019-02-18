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
        private Thread _worker;

        #endregion

        #region Events

        /// <summary>
        /// Reaised when streaming starts.
        /// </summary>
        public event Action<PipelineProfile> OnStart;

        /// <summary>
        /// Raised when streaming stops.
        /// </summary>
        public event Action OnStop;

        /// <summary>
        /// Raised when a new frame set is available
        /// </summary>
        public event Action<FrameSet> OnFrameSetArrived;

        /// <summary>
        /// Raised when a color frame is available.
        /// </summary>
        public event Action<VideoFrame> OnColorFrameArrived;

        /// <summary>
        /// Raised when a depth frame is available.
        /// </summary>
        public event Action<DepthFrame> OnDepthFrameArrived;

        /// <summary>
        /// Raised when an infrared frame is available.
        /// </summary>
        public event Action<VideoFrame> OnInfraredFrameArrived;

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
                        
            CoordinateMapper = CoordinateMapper.Create(this);

            _stopEvent.Reset();

            _worker = new Thread(WaitForFrames)
            {
                IsBackground = true
            };
            _worker.Start();

            Streaming = true;

            OnStart?.Invoke(ActiveProfile);
        }

        /// <summary>
        /// Closes the sensor and disposes any resources.
        /// </summary>
        public void Close()
        {
            OnFrameSetArrived = null;
            OnColorFrameArrived = null;
            OnDepthFrameArrived = null;
            OnInfraredFrameArrived = null;

            if (_worker != null)
            {
                _stopEvent.Set();
                _worker.Join();
            }

            if (Streaming && OnStop != null)
            {
                OnStop();
            }

            if (_pipeline != null)
            {
                if (Streaming)
                    _pipeline.Stop();
                _pipeline.Release();
                _pipeline = null;
            }

            Streaming = false;

            if (ActiveProfile != null)
            {
                ActiveProfile.Dispose();
                ActiveProfile = null;
            }

            OnStop = null;

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
                    OnFrameSetArrived?.Invoke(set);
                }
            }
        }

        #endregion
    }
}
