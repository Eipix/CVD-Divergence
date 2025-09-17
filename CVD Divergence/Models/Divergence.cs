using CVD_Divergence.Models;
using System.Drawing;

using static CVD_Divergence.Extensions;

namespace ATAS.Indicators.Technical;

public record Divergence
{
    public readonly PriceExtremum Left;
    public readonly PriceExtremum Right;

    public readonly Color Color;

    public readonly DivergenceType Type;

    public readonly bool IsAbsorption;
    public readonly bool IsExhaustion;

    public Divergence(PriceExtremum last, PriceExtremum previous, in Color color, in DivergenceType type)
    {
        Right = last;
        Left = previous;

        Color = color;
        Type = type;

        IsAbsorption = Type switch
        {
            DivergenceType.Bullish => Falling(Left.Delta, Right.Delta),
            DivergenceType.Bearish => Rising(Left.Delta, Right.Delta),
            _ => throw new IndexOutOfRangeException($"Unexpected divergence type {nameof(Type)}"),
        };

        IsExhaustion = !IsAbsorption;
    }
}
