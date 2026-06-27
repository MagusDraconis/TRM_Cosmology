namespace TRM.Core
{
    public sealed class ThetaFieldProfile
    {
        public List<ThetaFieldPoint> Points { get; } = new();

        public ThetaFieldPoint? FindNearest(double radiusKpc)
        {
            if (Points.Count == 0)
                return null;

            return Points
                .OrderBy(p => Math.Abs(p.RadiusKpc - radiusKpc))
                .FirstOrDefault();
        }
    }
}
