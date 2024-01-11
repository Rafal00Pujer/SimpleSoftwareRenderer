namespace SimpleSoftwareRenderer;

struct SimpleColor()
{
    public float R { get; set; }

    public float G { get; set; }

    public float B { get; set; }

    public static SimpleColor operator *(SimpleColor color, float intensity)
    {
        color.R *= intensity;
        color.G *= intensity;
        color.B *= intensity;

        return color;
    }

    public static SimpleColor operator *(float intensity, SimpleColor color) => color * intensity;

    public static SimpleColor operator *(SimpleColor left, SimpleColor right)
    {
        var result = new SimpleColor
        {
            R = left.R * right.R,
            G = left.G * right.G,
            B = left.B * right.B,
        };

        return result;
    }

    public static SimpleColor operator +(SimpleColor colorA, SimpleColor colorB)
    {
        var color = new SimpleColor
        {
            R = colorA.R + colorB.R,
            G = colorA.G + colorB.G,
            B = colorA.B + colorB.B
        };

        return color;
    }

    public override readonly string ToString()
    {
        return $"(R:{R}, G:{G}, B:{B})";
    }
}