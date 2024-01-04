namespace SimpleSoftwareRenderer;

struct MyColor()
{
    public float R { get; set; }

    public float G { get; set; }

    public float B { get; set; }

    public static MyColor operator *(MyColor color, float intensity)
    {
        color.R *= intensity;
        color.G *= intensity;
        color.B *= intensity;

        return color;
    }

    public static MyColor operator *(float intensity, MyColor color) => color * intensity;

    public static MyColor operator +(MyColor colorA, MyColor colorB)
    {
        var color = new MyColor
        {
            R = colorA.R + colorB.R,
            G = colorA.G + colorB.G,
            B = colorA.B + colorB.B
        };

        return color;
    }
}