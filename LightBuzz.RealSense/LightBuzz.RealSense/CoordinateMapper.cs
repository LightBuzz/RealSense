using Intel.RealSense;

namespace LightBuzz.RealSense
{
    /// <summary>
    /// Converts between 2D and 3D RealSense coordinates.
    /// </summary>
    public sealed class CoordinateMapper
    {
        #region Constants

        /// <summary>
        /// The default color frame width.
        /// </summary>
        public static readonly int DefaultColorWidth = 640;

        /// <summary>
        /// The default color frame height.
        /// </summary>
        public static readonly int DefaultColorHeight = 480;

        /// <summary>
        /// The default depth frame width.
        /// </summary>
        public static readonly int DefaultDepthWidth = 640;

        /// <summary>
        /// The default depth frame height.
        /// </summary>
        public static readonly int DefaultDepthHeight = 480;

        #endregion

        #region Properties

        /// <summary>
        /// The Color intrinsics of the camera.
        /// </summary>
        public Intrinsics ColorIntrinsics { get; set; }

        /// <summary>
        /// The Color extrinsics of the camera.
        /// </summary>
        public Extrinsics ColorExtrinsics { get; set; }

        /// <summary>
        /// The Depth intrinsics of the camera.
        /// </summary>
        public Intrinsics DepthIntrinsics { get; set; }

        /// <summary>
        /// The Depth extrinsics of the camera.
        /// </summary>
        public Extrinsics DepthExtrinsics { get; set; }

        #endregion

        #region Members

        private bool colorAndDepthMatch = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty Coordinate Mapper.
        /// </summary>
        private CoordinateMapper()
        {
        }

        /// <summary>
        /// Creates a new Coordinate Mapper with the specified intrinsics and extrinsics parameters.
        /// </summary>
        /// <param name="colorIntrinsics">The color intrinsics.</param>
        /// <param name="colorExtrinsics">The color extrinsics.</param>
        /// <param name="depthIntrinsics">The depth intrinsics.</param>
        /// <param name="depthExtrinsics">The depth extrinsics.</param>
        private CoordinateMapper(Intrinsics colorIntrinsics, Extrinsics colorExtrinsics, Intrinsics depthIntrinsics, Extrinsics depthExtrinsics)
        {
            ColorIntrinsics = colorIntrinsics;
            ColorExtrinsics = colorExtrinsics;
            DepthIntrinsics = depthIntrinsics;
            DepthExtrinsics = depthExtrinsics;

            if (ColorIntrinsics.width == DepthIntrinsics.width ||
                ColorIntrinsics.height == DepthIntrinsics.height ||
                ColorIntrinsics.ppx == DepthIntrinsics.ppx ||
                ColorIntrinsics.ppy == DepthIntrinsics.ppy ||
                ColorIntrinsics.fx == DepthIntrinsics.fx ||
                ColorIntrinsics.fy == DepthIntrinsics.fy ||
                ColorIntrinsics.model == DepthIntrinsics.model)
            {
                colorAndDepthMatch = true;
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Instantiates a new coordinate mapper for the specified pipeline.
        /// </summary>
        /// <param name="pipeline">The specified pipeline.</param>
        /// <param name="colorWidth">The desired color frame width.</param>
        /// <param name="colorHeight">The desired color frame height.</param>
        /// <param name="depthWidth">The desired depth frame width.</param>
        /// <param name="depthHeight">The desired depth frame height.</param>
        /// <returns>The color/depth coordinate mapper of the current pipline, if all of the supported streams were found. Null otherwise.</returns>
        public static CoordinateMapper Create(PipelineProfile pipeline, int colorWidth, int colorHeight, int depthWidth, int depthHeight)
        {
            if (pipeline == null) return null;
            if (pipeline.Streams == null) return null;
            if (pipeline.Streams.Count == 0) return null;

            StreamProfile colorProfile = null;
            StreamProfile depthProfile = null;

            foreach (StreamProfile profile in pipeline.Streams)
            {
                VideoStreamProfile videoProfile = profile as VideoStreamProfile;

                if (profile.Stream == Stream.Color && videoProfile.Width == colorWidth && videoProfile.Height == colorHeight)
                {
                    colorProfile = profile;
                }
                else if (profile.Stream == Stream.Depth && videoProfile.Width == depthWidth && videoProfile.Height == depthHeight)
                {
                    depthProfile = profile;
                }
            }

            if (colorProfile == null) return null;
            if (depthProfile == null) return null;
                        
            Intrinsics colorIntrinsics = (colorProfile as VideoStreamProfile).GetIntrinsics();
            Extrinsics colorExtrinsics = colorProfile.GetExtrinsicsTo(depthProfile);
            Intrinsics depthIntrinsics = (depthProfile as VideoStreamProfile).GetIntrinsics();
            Extrinsics depthExtrinsics = depthProfile.GetExtrinsicsTo(colorProfile);

            return Create(colorIntrinsics, colorExtrinsics, depthIntrinsics, depthExtrinsics);
        }

        public static CoordinateMapper Create(PipelineProfile pipeline, DeviceConfiguration configuration)
        {
            if (configuration == null)
            {
                return Create(pipeline);
            }

            int colorWidth = DefaultColorWidth;
            int colorHeight = DefaultColorHeight;
            int depthWidth = DefaultDepthWidth;
            int depthHeight = DefaultDepthHeight;

            foreach (var profile in configuration.Profiles)
            {
                if (profile.Stream == Stream.Color)
                {
                    colorWidth = profile.Width;
                    colorHeight = profile.Height;
                }
                if (profile.Stream == Stream.Depth)
                {
                    depthWidth = profile.Width;
                    depthHeight = profile.Height;
                }
            }

            return Create(pipeline, colorWidth, colorHeight, depthWidth, depthHeight);
        }

        /// <summary>
        /// Instantiates a new coordinate mapper for the specified pipeline.
        /// </summary>
        /// <param name="pipeline">The specified pipeline.</param>
        /// <returns>The color/depth coordinate mapper of the current pipline, if all of the supported streams were found. Null otherwise.</returns>
        public static CoordinateMapper Create(PipelineProfile pipeline)
        {
            return Create(pipeline, DefaultColorWidth, DefaultColorHeight, DefaultDepthWidth, DefaultDepthHeight);
        }

        public static CoordinateMapper Create(RealSenseDevice device)
        {
            if (device == null) return null;

            return Create(device.ActiveProfile, device.DeviceConfiguration);
        }

        /// <summary>
        /// Instantiates a new coordinate mapper with the specified intrinsics and extrinsics parameters.
        /// </summary>
        /// <param name="colorIntrinsics">The color intrinsics.</param>
        /// <param name="colorExtrinsics">The color extrinsics.</param>
        /// <param name="depthIntrinsics">The depth intrinsics.</param>
        /// <param name="depthExtrinsics">The depth extrinsics.</param>
        /// <returns></returns>
        public static CoordinateMapper Create(Intrinsics colorIntrinsics, Extrinsics colorExtrinsics, Intrinsics depthIntrinsics, Extrinsics depthExtrinsics)
        {
            return new CoordinateMapper
            (
                colorIntrinsics,
                colorExtrinsics,
                depthIntrinsics,
                depthExtrinsics
            );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Maps the specified 3D point to the 2D color space.
        /// </summary>
        /// <param name="point">The 3D point to map.</param>
        /// <returns>The corresponding 2D color point.</returns>
        public Vector2D MapWorldToColor(Vector3D point)
        {
            return Map3DTo2D(ColorIntrinsics, point);
        }

        /// <summary>
        /// Maps the specified 3D point to the 2D depth space.
        /// </summary>
        /// <param name="point">The 3D point to map.</param>
        /// <returns>The corresponding 2D depth point.</returns>
        public Vector2D MapWorldToDepth(Vector3D point)
        {
            return Map3DTo2D(DepthIntrinsics, point);
        }

        /// <summary>
        /// Maps the specified 2D color point to the 3D space.
        /// </summary>
        /// <param name="point">The 2D color point to map.</param>
        /// <param name="depth">The depth of the 2D point to map.</param>
        /// <returns>The corresponding 3D point.</returns>
        public Vector3D MapColorToWorld(Vector2D point, float depth)
        {
            return Map2DTo3D(ColorIntrinsics, point, depth);
        }

        /// <summary>
        /// Maps the specified 2D depth point to the 3D space.
        /// </summary>
        /// <param name="point">The 2D depth point to map.</param>
        /// <param name="depth">The depth of the 2D point to map.</param>
        /// <returns>The corresponding 3D point.</returns>
        public Vector3D MapDepthToWorld(Vector2D point, float depth)
        {
            return Map2DTo3D(DepthIntrinsics, point, depth);
        }

        /// <summary>
        /// Maps the specified color pixel to the depth space.
        /// </summary>
        /// <param name="colorPixel">The color point to map.</param>
        /// <param name="depthData">The depth array data.</param>
        /// <param name="depthScale">The depth scale. Use the <code>Sensor.DepthScale</code> proepty to find the depth scale.</param>
        /// <param name="depthMin">The minimum depth distance (defaults to 0.1 meters).</param>
        /// <param name="depthMax">The maxiumum depth distance (defaults to 10 meters).</param>
        /// <returns>The equivalent depth space point.</returns>
        public Vector2D MapColorToDepth(Vector2D colorPixel, float[] depthData, float depthScale, float depthMin = 0.1f, float depthMax = 10.0f)
        {
            if (colorAndDepthMatch)
            {
                return colorPixel;
            }

            Vector2D depthPixel = new Vector2D();

            // Find line start pixel
            Vector3D min_point = MapColorToWorld(colorPixel, depthMin);
            Vector3D min_transformed_point = MapWorldToWorld(min_point, ColorExtrinsics);
            Vector2D start_pixel = MapWorldToDepth(min_transformed_point);

            start_pixel = AdjustPointToBoundary(start_pixel, DepthIntrinsics.width, DepthIntrinsics.height);

            // Find line end depth pixel
            Vector3D max_point = MapColorToWorld(colorPixel, depthMax);
            Vector3D max_transformed_point = MapWorldToWorld(max_point, ColorExtrinsics);
            Vector2D end_pixel = MapWorldToDepth(max_transformed_point);

            end_pixel = AdjustPointToBoundary(end_pixel, DepthIntrinsics.width, DepthIntrinsics.height);

            // Search along line for the depth pixel that it's projected pixel is the closest to the input pixel
            float min_dist = -1;

            for (Vector2D p = start_pixel; IsPixelInLine(p, start_pixel, end_pixel); p = NextPointInLine(p, start_pixel, end_pixel))
            {
                float depth = depthScale * depthData[(int)p[1] * DepthIntrinsics.width + (int)p[0]];

                if (depth == 0)
                    continue;

                Vector3D point = MapDepthToWorld(p, depth);
                Vector3D transformed_point = MapWorldToWorld(point, DepthExtrinsics);
                Vector2D projected_pixel = MapWorldToColor(transformed_point);

                float new_dist = (float)(System.Math.Pow((projected_pixel[1] - colorPixel[1]), 2) + System.Math.Pow((projected_pixel[0] - colorPixel[0]), 2));

                if (new_dist < min_dist || min_dist < 0)
                {
                    min_dist = new_dist;

                    depthPixel[0] = p[0];
                    depthPixel[1] = p[1];
                }
            }

            return depthPixel;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Maps the specified 3D point to the 2D space.
        /// </summary>
        /// <param name="intrinsics">The camera intrinsics to use.</param>
        /// <param name="point">The 3D point to map.</param>
        /// <returns>The corresponding 2D point.</returns>
        private Vector2D Map3DTo2D(Intrinsics intrinsics, Vector3D point)
        {
            Vector2D pixel = new Vector2D();

            float x = point.X / point.Z;
            float y = point.Y / point.Z;

            if (intrinsics.model == Distortion.ModifiedBrownConrady)
            {
                float r2 = x * x + y * y;
                float f = 1f + intrinsics.coeffs[0] * r2 + intrinsics.coeffs[1] * r2 * r2 + intrinsics.coeffs[4] * r2 * r2 * r2;

                x *= f;
                y *= f;

                float dx = x + 2f * intrinsics.coeffs[2] * x * y + intrinsics.coeffs[3] * (r2 + 2 * x * x);
                float dy = y + 2f * intrinsics.coeffs[3] * x * y + intrinsics.coeffs[2] * (r2 + 2 * y * y);

                x = dx;
                y = dy;
            }

            if (intrinsics.model == Distortion.Ftheta)
            {
                float r = (float)System.Math.Sqrt(x * x + y * y);
                float rd = (1f / intrinsics.coeffs[0] * (float)System.Math.Atan(2f * r * (float)System.Math.Tan(intrinsics.coeffs[0] / 2f)));

                x *= rd / r;
                y *= rd / r;
            }

            pixel.X = x * intrinsics.fx + intrinsics.ppx;
            pixel.Y = y * intrinsics.fy + intrinsics.ppy;

            return pixel;
        }

        /// <summary>
        /// Maps the specified 2D point to the 3D space.
        /// </summary>
        /// <param name="intrinsics">The camera intrinsics to use.</param>
        /// <param name="pixel">The 2D point to map.</param>
        /// <param name="depth">The depth of the 2D point to map.</param>
        /// <returns>The corresponding 3D point.</returns>
        private Vector3D Map2DTo3D(Intrinsics intrinsics, Vector2D pixel, float depth)
        {
            Vector3D point = new Vector3D();

            float x = (pixel.X - intrinsics.ppx) / intrinsics.fx;
            float y = (pixel.Y - intrinsics.ppy) / intrinsics.fy;

            if (intrinsics.model == Distortion.InverseBrownConrady)
            {
                float r2 = x * x + y * y;
                float f = 1 + intrinsics.coeffs[0] * r2 + intrinsics.coeffs[1] * r2 * r2 + intrinsics.coeffs[4] * r2 * r2 * r2;
                float ux = x * f + 2 * intrinsics.coeffs[2] * x * y + intrinsics.coeffs[3] * (r2 + 2 * x * x);
                float uy = y * f + 2 * intrinsics.coeffs[3] * x * y + intrinsics.coeffs[2] * (r2 + 2 * y * y);

                x = ux;
                y = uy;
            }

            point.X = depth * x;
            point.Y = depth * y;
            point.Z = depth;

            return point;
        }

        private Vector3D MapWorldToWorld(Vector3D original, Extrinsics extrinsics)
        {
            return new Vector3D
            (
                extrinsics.rotation[0] * original[0] + extrinsics.rotation[3] * original[1] + extrinsics.rotation[6] * original[2] + extrinsics.translation[0],
                extrinsics.rotation[1] * original[0] + extrinsics.rotation[4] * original[1] + extrinsics.rotation[7] * original[2] + extrinsics.translation[1],
                extrinsics.rotation[2] * original[0] + extrinsics.rotation[5] * original[1] + extrinsics.rotation[8] * original[2] + extrinsics.translation[2]
            );
        }

        private Vector2D AdjustPointToBoundary(Vector2D point, int width, int height)
        {
            if (point.X < 0) point.X = 0;
            if (point.X > width) point.X = width;
            if (point.Y < 0) point.Y = 0;
            if (point.Y > height) point.Y = height;

            return point;
        }

        private Vector3D AdjustPointToBoundary(Vector3D point, int width, int height, int depth)
        {
            if (point.X < 0) point.X = 0;
            if (point.X > width) point.X = width;
            if (point.Y < 0) point.Y = 0;
            if (point.Y > height) point.Y = height;
            if (point.Z < 0) point.Z = 0;
            if (point.Z > depth) point.Z = depth;

            return point;
        }

        private Vector2D NextPointInLine(Vector2D current, Vector2D start, Vector2D end)
        {
            if (end.X != start.X)
            {
                float lineSlope = (end.Y - start.Y) / (end.X - start.X);

                if (System.Math.Abs(end.X - current.X) > System.Math.Abs(end.Y - current.Y))
                {
                    current.X = end.X > current.X ? current.X + 1 : current.X - 1;
                    current.Y = end.Y - lineSlope * (end.X - current.X);
                }
                else
                {
                    current.Y = end.Y > current.Y ? current.Y + 1 : current.Y - 1;
                    current.X = end.X - ((end.Y + current.Y) / lineSlope);
                }
            }

            return current;
        }

        private bool IsPixelInLine(Vector2D current, Vector2D start, Vector2D end)
        {
            return
                ((end.X >= start.X && end.X >= current.X && current.X >= start.X) || (end.X <= start.X && end.X <= current.X && current.X <= start.X)) &&
                ((end.Y >= start.Y && end.Y >= current.Y && current.Y >= start.Y) || (end.Y <= start.Y && end.Y <= current.Y && current.Y <= start.Y));
        }

        #endregion
    }
}