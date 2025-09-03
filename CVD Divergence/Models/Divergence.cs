using CVD_Divergence.Models;
using System.Drawing;

using static CVD_Divergence.Extensions;

namespace ATAS.Indicators.Technical;

public readonly record struct Divergence
{
    public readonly PriceExtremum Left;
    public readonly PriceExtremum Right;
    public readonly Color Color;

    public readonly DivergenceType Type;

    public readonly bool IsAbsorption;
    public readonly bool IsExhaustion;

    public Divergence(in PriceExtremum left, in PriceExtremum right, in Color color, in DivergenceType type)
    {
        Left = left;
        Right = right;

        Color = color;
        Type = type;

        IsAbsorption = Type switch
        {
            DivergenceType.Bullish => Falling(left.Delta, right.Delta),
            DivergenceType.Bearish => Rising(left.Delta, right.Delta),
            _ => throw new IndexOutOfRangeException($"Unexpected divergence type {nameof(Type)}"),
        };

        IsExhaustion = !IsAbsorption;
    }
}
