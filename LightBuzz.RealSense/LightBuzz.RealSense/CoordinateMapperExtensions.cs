using Intel.RealSense;

namespace LightBuzz.RealSense
{
    /// <summary>
    /// Provides coordinate mapping extension methods.
    /// </summary>
    public static class CoordinateMapperExtensions
    {
        /// <summary>
        /// Gets the coordinate mapper of the current pipline profile.
        /// </summary>
        /// <param name="pipeline">The current pipeline.</param>
        /// <param name="colorWidth">The desired color frame width.</param>
        /// <param name="colorHeight">The desired color frame height.</param>
        /// <param name="depthWidth">The desired depth frame width.</param>
        /// <param name="depthHeight">The desired depth frame height.</param>
        /// <returns>The color/depth coordinate mapper of the current pipline, if all of the supported streams were found. Null otherwise.</returns>
        public static CoordinateMapper GetCoordinateMapper(this PipelineProfile pipeline, int colorWidth, int colorHeight, int depthWidth, int depthHeight)
        {
            return CoordinateMapper.Create(pipeline, colorWidth, colorHeight, depthWidth, depthHeight);
        }

        /// <summary>
        /// Gets the coordinate mapper of the current pipline profile.
        /// </summary>
        /// <param name="pipeline">The current pipeline.</param>
        /// <returns>The color/depth coordinate mapper of the current pipline, if all of the supported streams were found. Null otherwise.</returns>
        public static CoordinateMapper GetCoordinateMapper(this PipelineProfile pipeline)
        {
            return CoordinateMapper.Create(pipeline);
        }
    }
}
