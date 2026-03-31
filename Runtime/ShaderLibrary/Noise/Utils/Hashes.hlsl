float Hash01(float p)
{
    uint x = asuint(p);
    x = x * 747796405u + 2891336453u;
    uint word = ((x >> ((x >> 28u) + 4u)) ^ x) * 277803737u;
    uint result = (word >> 22u) ^ word;
    return float(result) * 2.3283064365386963e-10;
}