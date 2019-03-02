using System;

namespace LightBuzz.RealSense
{
    public class AlignedFrameData
    {
        public DateTime Timestamp { get; internal set; }

        public byte[] ColorData { get; internal set; }

        public ushort[] DepthData { get; internal set; }
    }
}
