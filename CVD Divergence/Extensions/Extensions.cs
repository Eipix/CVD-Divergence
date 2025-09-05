
using ATAS.Indicators;

namespace CVD_Divergence;

public static class Extensions
{
    public static bool IsUp(this IndicatorCandle candle) => candle.Close > candle.Open;
    public static bool IsDown(this IndicatorCandle candle) => candle.Open > candle.Close;

    public static decimal UpperShadow(this IndicatorCandle candle) => candle.High - Math.Max(candle.Open, candle.Close);

    public static decimal LowerShadow(this IndicatorCandle candle) => Math.Min(candle.Open, candle.Close) - candle.Low;

    public static decimal Body(this IndicatorCandle candle) => Math.Abs(candle.Open - candle.Close);

    public static decimal BodyPercent(this IndicatorCandle candle)
    {
        if (candle.Open is 0m)
            return 0m;

        return candle.Body() / candle.Open * 100m;
    }

    public static decimal UpperShadowPercent(this IndicatorCandle candle)
    {
        decimal body = candle.Body();

        if (body is 0m)
            return decimal.MaxValue;

        return candle.UpperShadow() / body;
    }

    public static decimal LowerShadowPercent(this IndicatorCandle candle)
    {
        decimal body = candle.Body();

        if (body is 0m)
            return decimal.MaxValue;

        return candle.LowerShadow() / body;
    }

    public static bool Rising(in decimal left, in decimal right) => left < right;
    public static bool Falling(in decimal left, in decimal right) => left > right;
    public static bool RisingOrFalling(in decimal left, in decimal right) => Rising(left, right) || Falling(left, right);

    public static bool RisingOrEqual(in decimal left, in decimal right) => left <= right;
    public static bool FallingOrEqual(in decimal left, in decimal right) => left >= right;
}
