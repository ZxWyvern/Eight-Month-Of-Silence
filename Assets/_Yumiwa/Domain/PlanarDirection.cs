namespace Yumiwa.Domain
{
    /// <summary>Engine-agnostic 2D direction value object (keeps UnityEngine out of upper layers).</summary>
    public readonly struct PlanarDirection
    {
        public readonly float X;
        public readonly float Y;

        public PlanarDirection(float x, float y)
        {
            float length = (float)System.Math.Sqrt(x * x + y * y);
            X = length > 1e-5f ? x / length : 0f;
            Y = length > 1e-5f ? y / length : 0f;
        }

        public bool HasDirection => X != 0f || Y != 0f;
    }
}
