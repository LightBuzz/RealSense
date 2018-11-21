using System;
using Intel.RealSense;

namespace LightBuzz.RealSense
{
    [Serializable]
    public class DeviceConfiguration
    {
        public VideoStreamMode Mode { get; set; }

        public VideoStreamRequest[] Profiles { get; set; }

        public string RequestedSerialNumber { get; set; }

        public string PlaybackFile { get; set; }

        public string RecordPath { get; set; }

        public DeviceConfiguration()
        {
        }

        public DeviceConfiguration(VideoStreamRequest[] profiles)
        {
            Mode = VideoStreamMode.Live;
            RequestedSerialNumber = string.Empty;
            Profiles = profiles;
        }

        public DeviceConfiguration(VideoStreamRequest[] profiles, VideoStreamMode mode, string id)
        {
            Mode = mode;
            RequestedSerialNumber = id;
            Profiles = profiles;
        }

        public Config ToPipelineConfig()
        {
            Config cfg = new Config();

            switch (Mode)
            {
                case VideoStreamMode.Live:
                    cfg.EnableDevice(RequestedSerialNumber);
                    foreach (var p in Profiles)
                        cfg.EnableStream(p.Stream, p.StreamIndex, p.Width, p.Height, p.Format, p.Framerate);
                    break;

                case VideoStreamMode.Playback:
                    if (string.IsNullOrEmpty(PlaybackFile))
                    {
                        Mode = VideoStreamMode.Live;
                    }
                    else
                    {
                        cfg.EnableDeviceFromFile(PlaybackFile);
                    }
                    break;

                case VideoStreamMode.Record:
                    foreach (var p in Profiles)
                        cfg.EnableStream(p.Stream, p.StreamIndex, p.Width, p.Height, p.Format, p.Framerate);
                    if (!string.IsNullOrEmpty(RecordPath))
                        cfg.EnableRecordToFile(RecordPath);
                    break;

            }

            return cfg;
        }

        public static DeviceConfiguration Default()
        {
            return new DeviceConfiguration
            {
                Mode = VideoStreamMode.Live,
                RequestedSerialNumber = string.Empty,
                Profiles = new VideoStreamRequest[]
                {
                    new VideoStreamRequest
                    {
                        Stream = Stream.Depth,
                        StreamIndex = -1,
                        Width = 640,
                        Height = 480,
                        Format = Format.Z16,
                        Framerate = 0
                    },
                    new VideoStreamRequest
                    {
                        Stream = Stream.Infrared,
                        StreamIndex = -1,
                        Width = 640,
                        Height = 480,
                        Format = Format.Y8,
                        Framerate = 0
                    },
                    new VideoStreamRequest
                    {
                        Stream = Stream.Color,
                        StreamIndex = -1,
                        Width = 640,
                        Height = 480,
                        Format = Format.Rgb8,
                        Framerate = 0
                    }
                }
            };
        }
    }
}
