namespace CVD_Divergence.Models;

public record PriceExtremum
{
    public readonly int Bar;
    public readonly decimal Price;
    public readonly decimal Delta;

    public PriceExtremum(in int bar, in decimal price, in decimal delta)
    {
        Bar = bar;
        Price = price;
        Delta = delta;
    }
}
