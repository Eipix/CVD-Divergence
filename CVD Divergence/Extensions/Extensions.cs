
namespace CVD_Divergence;

public static class Extensions
{
    public static bool Rising(in decimal left, in decimal right) => left < right;
    public static bool Falling(in decimal left, in decimal right) => left > right;
    public static bool RisingOrFalling(in decimal left, in decimal right) => Rising(left, right) || Falling(left, right);

    public static bool RisingOrEqual(in decimal left, in decimal right) => left <= right;
    public static bool FallingOrEqual(in decimal left, in decimal right) => left >= right;
}
