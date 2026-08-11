/// @brief Generates a uniformly distributed pseudo-random float in the range [0, 1) 
///        from a float input using Thomas Wang's integer hash / "Hash without Sine" (Dave Hoskins variant).
/// @details This function takes a float input, converts it to an unsigned integer, and applies a series of 
/// bitwise operations and multiplications to produce a pseudo-random output. The final result is normalized 
/// to the range [0, 1) by multiplying with a constant factor. (see: https://www.shadertoy.com/view/4djSRW)
/// @param p The input value to hash.
/// @return A pseudo-random float between 0 and 1.
float Hash01(float p)
{
    uint x = asuint(p);
    x = x * 747796405u + 2891336453u;
    uint word = ((x >> ((x >> 28u) + 4u)) ^ x) * 277803737u;
    uint result = (word >> 22u) ^ word;
    return float(result) * 2.3283064365386963e-10;
}