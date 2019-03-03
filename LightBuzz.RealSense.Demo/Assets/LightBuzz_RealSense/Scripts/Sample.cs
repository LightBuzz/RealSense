using Intel.RealSense;
using LightBuzz.RealSense;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    [SerializeField]
    private RawImage image;

    [SerializeField]
    private Text log;

    private RealSenseDevice device;
    private AlignedFrameData data;

    private DateTime fpsDate = DateTime.MinValue;
    private int fpsCount;

    private Texture2D texture;

    private int x = 640 / 2;
    private int y = 480 / 2;

    private void Start()
    {
        device = new RealSenseDevice();
        device.OnFrameDataArrived += Device_OnFrameDataArrived;
        device.Open();

        texture = new Texture2D(device.DeviceConfiguration.ColorProfile.Width, device.DeviceConfiguration.ColorProfile.Height, TextureFormat.RGB24, false);
        image.texture = texture;
    }

    private void OnDestroy()
    {
        device.OnFrameDataArrived -= Device_OnFrameDataArrived;
        device.Close();
    }

    private void Update()
    {
        if (device.IsOpen)
        {
            texture.LoadRawTextureData(data.ColorData);
            texture.Apply(false);

            //float depth = data.DepthData[y * 640 + x] / 1000f;

            //log.text = Math.Round(depth, 2).ToString("N2");
        }

        Vector3 mouse = Input.mousePosition;

        if (mouse.x >= 0f && mouse.x <= 640f && mouse.y >= 0f && mouse.y <= 480f)
        {
            x = (int)mouse.x;
            y = 480 - (int)mouse.y;
        }
    }

    private void Device_OnFrameDataArrived(AlignedFrameData obj)
    {
        data = obj;

        fpsCount++;

        if ((DateTime.Now - fpsDate).Seconds >= 1)
        {
            Debug.Log("FPS: " + fpsCount);

            fpsDate = DateTime.Now;
            fpsCount = 0;
        }
    }

    public void OnStartRecording()
    {
        //device.IsRecording = true;
    }

    public void OnStopRecording()
    {
        //device.IsRecording = false;
    }
}
