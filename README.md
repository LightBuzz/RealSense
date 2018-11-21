# LightBuzz RealSense SDK for C#

An easy-to-use C# wrapper for the [Intel RealSense D415/435 SDK](https://github.com/IntelRealSense/librealsense). Supports .NET 3.5+ and Unity3D for Windows.

## Download

The latest binaries are available in the project [releases](https://github.com/LightBuzz/RealSense/releases/latest).

## Features

### Color, Depth, and Infrared streams @ 90 FPS

```
private RealSenseDevice device;

private void Start()
{
    device = new RealSenseDevice();
    device.OnColorFrameArrived += Device_OnColorFrameArrived;
    device.OnDepthFrameArrived += Device_OnDepthFrameArrived;
    device.OnInfraredFrameArrived += Device_OnInfraredFrameArrived;
    device.Open();
}

private void OnDestroy()
{
    device.OnColorFrameArrived -= Device_OnColorFrameArrived;
    device.OnDepthFrameArrived -= Device_OnDepthFrameArrived;
    device.OnInfraredFrameArrived -= Device_OnInfraredFrameArrived;
    device.Close();
}

private void Device_OnColorFrameArrived(VideoFrame frame)
{
    // Do something with the frame...
}

private void Device_OnDepthFrameArrived(DepthFrame frame)
{
    // Do something with the frame...
}

private void Device_OnInfraredFrameArrived(VideoFrame frame)
{
    // Do something with the frame...
}
```

### Coordinate mapping

```
CoordinateMapper mapper = mapper = device.ActiveProfile.GetCoordinateMapper();

mapper.MapDepthToWorld(point, depth);
mapper.MapWorldToColor(point);
mapper.MapWorldToDepth(point);
```

## Support

The project is developed and maintained by [LightBuzz Inc](https://lightbuzz.com).

## License

[Apache License 2.0](https://github.com/LightBuzz/RealSense/blob/master/LICENSE)
