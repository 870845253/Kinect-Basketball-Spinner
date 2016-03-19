using UnityEngine;
using System.Linq;
using Windows.Kinect;

public class BasketballSpinnerSample : MonoBehaviour
{
    private KinectSensor sensor;
    private ColorFrameReader colorReader;
    private BodyFrameReader bodyReader;
    private Body[] bodies;
    
    private Texture2D texture;
    private byte[] pixels;
    private int width;
    private int height;

    public GameObject quad;
    public GameObject ball;
    public float scale = 2f;
    public float speed = 10f;

    void Start()
    {
        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            bodyReader = sensor.BodyFrameSource.OpenReader();
            colorReader = sensor.ColorFrameSource.OpenReader();

            bodies = new Body[sensor.BodyFrameSource.BodyCount];

            width = sensor.ColorFrameSource.FrameDescription.Width;
            height = sensor.ColorFrameSource.FrameDescription.Height;
            pixels = new byte[width * height * 4];
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            quad.GetComponent<Renderer>().material.mainTexture = texture;
            quad.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));

            sensor.Open();
        }
    }

    void Update()
    {
        if (colorReader != null)
        {
            using (var frame = colorReader.AcquireLatestFrame())
            {
                if (frame != null)
                {
                    frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Rgba);
                    texture.LoadRawTextureData(pixels);
                    texture.Apply();
                }
            }
        }

        if (bodyReader != null)
        {
            using (var frame = bodyReader.AcquireLatestFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(bodies);

                    var body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (body != null)
                    {
                        var handTipRight = body.Joints[JointType.HandTipRight].Position;
                        var handTipLeft = body.Joints[JointType.HandTipLeft].Position;

                        var closer = handTipRight.Z < handTipLeft.Z ? handTipRight : handTipLeft;
                        var point = sensor.CoordinateMapper.MapCameraPointToColorSpace(closer);
                        var position = new Vector2(0f, 0f);

                        if (!float.IsInfinity(point.X) && !float.IsInfinity(point.Y))
                        {
                            position.x = point.X;
                            position.y = point.Y;
                        }

                        var world = Camera.main.ViewportToWorldPoint(new Vector3(position.x / width, position.y / height, 0f));
                        var center = quad.GetComponent<Renderer>().bounds.center;

                        ball.transform.localScale = new Vector3(scale, scale, scale) / closer.Z;
                        ball.transform.position = new Vector3(world.x - 0.5f - center.x, -world.y + 0.5f, -1f);
                        ball.transform.Rotate(0f, speed, 0f);
                    }
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        if (bodyReader != null)
        {
            bodyReader.Dispose();
        }

        if (colorReader != null)
        {
            colorReader.Dispose();
        }

        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
        }
    }
}