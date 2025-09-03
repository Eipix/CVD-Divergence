namespace CVD_Divergence.Models;

public readonly record struct PriceExtremum
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
