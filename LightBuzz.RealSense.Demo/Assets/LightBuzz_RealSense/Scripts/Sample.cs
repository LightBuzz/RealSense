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

    [SerializeField]
    [Range(1, 20)]
    private int radius = 4;

    private RealSenseDevice device;
    private Align aligner;

    private DateTime dateColor = DateTime.MinValue;
    private DateTime dateDepth = DateTime.MinValue;

    private int countColor;
    private int countDepth;

    private byte[] colorData;
    private ushort[] depthData;
    private Texture2D texture;

    private bool frameArrived;
    private float depth;
    private float depthMedian;

    private int x = 640 / 2;
    private int y = 480 / 2;

    private void Start()
    {
        device = new RealSenseDevice();
        device.OnFrameSetArrived += Device_OnFrameSetArrived;
        device.Open();

        aligner = new Align(Stream.Depth);

        colorData = new byte[640 * 480 * 3];
        depthData = new ushort[640 * 480];
        texture = new Texture2D(640, 480, TextureFormat.RGB24, false);
        image.texture = texture;
    }

    private void OnDestroy()
    {
        if (device != null)
        {
            device.OnFrameSetArrived -= Device_OnFrameSetArrived;
            device.Close();
        }
    }

    private void Update()
    {
        if (frameArrived)
        {
            texture.LoadRawTextureData(colorData);
            texture.Apply(false);

            log.text = 
                Math.Round(depth, 1).ToString("N1") + 
                Environment.NewLine +
                Math.Round(depthMedian, 1).ToString("N1");
        }

        Vector3 mouse = Input.mousePosition;

        if (mouse.x >= 0f && mouse.x <= 640f && mouse.y >= 0f && mouse.y <= 480f)
        {
            x = (int)mouse.x;
            y = 480 - (int)mouse.y;
        }
    }

    private void Device_OnFrameSetArrived(FrameSet obj)
    {
        using (VideoFrame colorFrame = obj.ColorFrame)
        {
            colorFrame.CopyTo(colorData);
        }
        using (FrameSet frames = aligner.Process(obj))
        using (DepthFrame depthFrame = frames.DepthFrame)
        {
            depthFrame.CopyTo(depthData);

            depth = depthFrame.GetDistance(x, y);
            depthMedian = MedianDepth();

            frameArrived = true;
        }

        //using (VideoFrame frame = obj.ColorFrame)
        //{
        //    countColor++;

        //    if ((DateTime.Now - dateColor).Seconds >= 1)
        //    {
        //        Debug.Log("Color: " + countColor);

        //        dateColor = DateTime.Now;
        //        countColor = 0;
        //    }
        //}

        //using (DepthFrame frame = obj.DepthFrame)
        //{
        //    countDepth++;

        //    if ((DateTime.Now - dateDepth).Seconds >= 1)
        //    {
        //        Debug.Log("Depth: " + countDepth);

        //        dateDepth = DateTime.Now;
        //        countDepth = 0;
        //    }
        //}
    }

    private float MedianDepth()
    {
        const int minimum = 0;
        const int maximum = 5000;

        float result = depth;

        List<ushort> values = new List<ushort>();

        int xMin = x - radius >= 0 ? x - radius : 0;
        int xMax = x + radius <= 640 ? x + radius : 640;
        int yMin = y - radius >= 0 ? y - radius : 0;
        int yMax = y + radius <= 480 ? y + radius : 480;

        for (int iX = xMin; iX < xMax; iX++)
        {
            for (int iY = yMin; iY < yMax; iY++)
            {
                ushort current = depthData[iY * 640 + iX];

                if (current > minimum && current < maximum)
                {
                    values.Add(current);
                }
            }
        }

        if (values.Count > 0)
        {
            foreach (ushort value in values)
            {
                result += value;
            }

            result /= values.Count;
            result /= 1000f;
        }

        return result;
    }
}
