namespace Content.Shared._OH;

public readonly record struct DoubleVec(double X, double Y)
{
    public double Length() => Math.Sqrt(X * X + Y * Y);
}
