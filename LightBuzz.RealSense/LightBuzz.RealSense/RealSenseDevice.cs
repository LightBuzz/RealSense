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

        private readonly AutoResetEvent _recordingEvent = new AutoResetEvent(false);

        private readonly AutoResetEvent _savingEvent = new AutoResetEvent(false);

        #endregion

        #region Members
        
        private Pipeline _pipeline;
        private Align _aligner;
        private Thread _streamingThread;
        private Thread _recordingThread;
        private Thread _savingThread;

        private AlignedFrameData _frameData;

        private bool _isRecording;

        #endregion

        #region Events

        /// <summary>
        /// Raised when updated color and depth data are available.
        /// </summary>
        public event Action<AlignedFrameData> OnFrameDataArrived;

        /// <summary>
        /// Raised when all of the frames have been recorded and saved to disk.
        /// </summary>
        public event Action OnRecordingCompleted;

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether the device is streaming frames.
        /// </summary>
        public bool Streaming { get; protected set; }

        /// <summary>
        /// Determines whether a new frame set has arrived.
        /// </summary>
        public bool FrameDataArrived { get; protected set; }

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

        /// <summary>
        /// Specifies whether the device will record color and depth data in the background.
        /// </summary>
        public bool IsRecording
        {
            get
            {
                return _isRecording;
            }
            set
            {
                _isRecording = value;

                if (_isRecording)
                {
                    _recordingIndex = 0;

                    //_recordingEvent.Reset();
                    //_recordingThread = new Thread(RecordFrames)
                    //{
                    //    IsBackground = true
                    //};
                    //_recordingThread.Start();

                    _savingEvent.Reset();
                    _savingThread = new Thread(SaveFrames)
                    {
                        IsBackground = true
                    };
                    _savingThread.Start();
                }
            }
        }

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

            _frameData = new AlignedFrameData
            {
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

            Streaming = true;
        }

        /// <summary>
        /// Closes the sensor and disposes any resources.
        /// </summary>
        public void Close()
        {
            OnFrameDataArrived = null;

            if (_recordingThread != null)
            {
                _recordingThread.Abort();
                _recordingThread = null;
            }
            if (_streamingThread != null)
            {
                _streamingEvent.Set();
                _streamingThread.Join();
            }

            //if (_savingThread != null)
            //{
            //    _savingEvent.Set();
            //    _savingThread.Join();
            //}

            //if (_recordingThread != null)
            //{
            //    _recordingEvent.Set();
            //    _recordingThread.Join();
            //}

            //if (_streamingThread != null)
            //{
            //    _streamingEvent.Set();
            //    _streamingThread.Join();
            //}

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
                _pipeline = null;
            }
        }

        public void Dispose()
        {
            _queue.Clear();
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

                    if (_isRecording)
                    {
                        _queue.Enqueue(_frameData);
                    }

                    OnFrameDataArrived?.Invoke(_frameData);

                    FrameDataArrived = true;
                }
            }
        }

        private Queue<AlignedFrameData> _queue = new Queue<AlignedFrameData>();
        private int _recordingIndex;
        private DateTime _lastRecordedDate;

        private void RecordFrames()
        {
            if (!_recordingEvent.WaitOne(0))
            {
                if (_isRecording)
                {
                    if (_frameData != null/* && _frameData.Timestamp != _lastRecordedDate*/)
                    {
                        //byte[] colorCopy = new byte[_frameData.ColorData.Length];
                        //ushort[] depthCopy = new ushort[_frameData.DepthData.Length];

                        //Array.Copy(_frameData.ColorData, colorCopy, colorCopy.Length);
                        //Array.Copy(_frameData.DepthData, depthCopy, depthCopy.Length);

                        //_queue.Enqueue(new AlignedFrameData
                        //{
                        //    Timestamp = _frameData.Timestamp,
                        //    ColorData = colorCopy,
                        //    DepthData = depthCopy
                        //});

                        AlignedFrameData d = new AlignedFrameData
                        {
                            Timestamp = _frameData.Timestamp,
                            ColorData = _frameData.ColorData,
                            DepthData = _frameData.DepthData
                        };

                        _queue.Enqueue(d);
                        //_lastRecordedDate = _frameData.Timestamp;
                    }
                }
                else
                {
                    if (_recordingThread != null)
                    {
                        _recordingEvent.Set();
                        _recordingThread.Join();
                    }
                }
            }
        }

        private void SaveFrames()
        {
            while (!_savingEvent.WaitOne(0))
            {
                if (_queue.Count > 0)
                {
                    AlignedFrameData data = _queue.Dequeue();

                    string pathColor = System.IO.Path.Combine(@"C:\Users\Galini\Desktop\Video", _recordingIndex + "_color.bin");
                    string pathDepth = System.IO.Path.Combine(@"C:\Users\Galini\Desktop\Video", _recordingIndex + "_depth.bin");

                    System.IO.File.WriteAllBytes(pathColor, data.ColorData);

                    using (System.IO.FileStream fs = new System.IO.FileStream(pathDepth, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                    {
                        using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs))
                        {
                            foreach (ushort value in data.DepthData)
                            {
                                bw.Write(value);
                            }
                        }
                    }

                    _recordingIndex++;
                }
                else
                {
                    if (_savingThread != null)
                    {
                        OnRecordingCompleted?.Invoke();

                        _savingEvent.Set();
                        _savingThread.Join();
                    }
                }
            }
        }

        #endregion
    }
}
