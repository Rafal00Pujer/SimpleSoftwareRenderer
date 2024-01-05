namespace SimpleSoftwareRenderer;

struct Color()
{
    public float R { get; set; }

    public float G { get; set; }

    public float B { get; set; }

    public static Color operator *(Color color, float intensity)
    {
        color.R *= intensity;
        color.G *= intensity;
        color.B *= intensity;

        return color;
    }

    public static Color operator *(float intensity, Color color) => color * intensity;

    public static Color operator +(Color colorA, Color colorB)
    {
        var color = new Color
        {
            R = colorA.R + colorB.R,
            G = colorA.G + colorB.G,
            B = colorA.B + colorB.B
        };

        return color;
    }
}