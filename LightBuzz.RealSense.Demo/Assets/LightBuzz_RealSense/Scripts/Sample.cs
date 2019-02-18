using Intel.RealSense;
using LightBuzz.RealSense;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    [SerializeField]
    private RawImage image;

    [SerializeField]
    private Text log;

    private RealSenseDevice device;
    private Align aligner;

    private DateTime dateColor= DateTime.MinValue;
    private DateTime dateDepth = DateTime.MinValue;

    private int countColor;
    private int countDepth;

    private byte[] colorData;
    private Texture2D texture;

    private bool frameArrived;
    private float depth;

    private void Start()
    {
        device = new RealSenseDevice();
        device.OnFrameSetArrived += Device_OnFrameSetArrived;
        device.Open();

        aligner = new Align(Stream.Color);

        colorData = new byte[640 * 480 * 3];
        texture = new Texture2D(640, 480, TextureFormat.RGB24, false);
        image.texture = texture;
    }

    private void OnDestroy()
    {
        if (device != null)
        {
            device.Close();
        }
    }

    private void Update()
    {
        if (frameArrived)
        {
            texture.LoadRawTextureData(colorData);
            texture.Apply(false);

            log.text = depth.ToString();
        }
    }

    private void Device_OnFrameSetArrived(FrameSet obj)
    {
        using (FrameSet frames = aligner.Process(obj))
        using (VideoFrame colorFrame = frames.ColorFrame)
        using (DepthFrame depthFrame = frames.DepthFrame)
        {
            colorFrame.CopyTo(colorData);

            depth = depthFrame.GetDistance(colorFrame.Width / 2, colorFrame.Height / 2);

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
}
