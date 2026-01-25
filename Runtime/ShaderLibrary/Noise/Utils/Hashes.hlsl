float Hash01(float x)
{
    x = frac(x * 12345.6789);
    x += dot(x, x + 45.32);
    return frac(x);
}