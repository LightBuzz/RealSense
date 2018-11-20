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
        /// <summary>
        /// Notifies upon streaming start
        /// </summary>
        public event Action<PipelineProfile> OnStart;

        /// <summary>
        /// Notifies when streaming has stopped
        /// </summary>
        public event Action OnStop;

        /// <summary>
        /// Fired when a new frame is available
        /// </summary>
        public event Action<Frame> OnFrameArrive;

        /// <summary>
        /// Fired when a new frame set is available
        /// </summary>
        public event Action<FrameSet> OnFrameSetArrive;

        /// <summary>
        /// User configuration
        /// </summary>
        public DeviceConfiguration DeviceConfiguration = new DeviceConfiguration
        {
            mode = DeviceConfiguration.Mode.Live,
            RequestedSerialNumber = string.Empty,
            Profiles = new VideoStreamRequest[]
            {
                new VideoStreamRequest {Stream = Stream.Depth, StreamIndex = -1, Width = 640, Height = 480, Format = Format.Z16 , Framerate = 0 },
                new VideoStreamRequest {Stream = Stream.Infrared, StreamIndex = -1, Width = 640, Height = 480, Format = Format.Y8 , Framerate = 0 },
                new VideoStreamRequest {Stream = Stream.Color, StreamIndex = -1, Width = 640, Height = 480, Format = Format.Rgb8 , Framerate = 0 }
            }
        };

        public bool Streaming { get; protected set; }
        public PipelineProfile ActiveProfile { get; protected set; }

        private Pipeline _pipeline;
        private Thread _worker;
        private readonly AutoResetEvent _stopEvent = new AutoResetEvent(false);

        public void Open()
        {
            _pipeline = new Pipeline();

            using (var cfg = DeviceConfiguration.ToPipelineConfig())
            {
                ActiveProfile = _pipeline.Start(cfg);
            }

            using (var activeStreams = ActiveProfile.Streams)
            {
                DeviceConfiguration.Profiles = activeStreams.Select(VideoStreamRequest.FromProfile).ToArray();
            }

            _stopEvent.Reset();

            _worker = new Thread(WaitForFrames)
            {
                IsBackground = true
            };
            _worker.Start();

            Streaming = true;

            OnStart?.Invoke(ActiveProfile);
        }

        public void Close()
        {
            OnFrameSetArrive = null;
            OnFrameArrive = null;

            if (_worker != null)
            {
                _stopEvent.Set();
                _worker.Join();
            }

            if (Streaming && OnStop != null)
                OnStop();

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

        /// <summary>
        /// Worker Thread for multithreaded operations
        /// </summary>
        private void WaitForFrames()
        {
            while (!_stopEvent.WaitOne(0))
            {
                //using (var frames = _pipeline.WaitForFrames())
                //using (var frame = frames.AsFrame())
                //    OnFrameArrive?.Invoke(frame);

                using (var frames = _pipeline.WaitForFrames())
                {
                    OnFrameSetArrive?.Invoke(frames);
                }
            }
        }
    }
}
