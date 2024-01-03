using System.Numerics;

namespace SimpleSoftwareRenderer;

struct MyColor()
{
    public byte R { get; set; }

    public byte G { get; set; }

    public byte B { get; set; }

    public static MyColor operator *(MyColor color, byte intensity)
    {
        color.R = (byte)Math.Clamp(color.R * intensity, 0, 255);
        color.G = (byte)Math.Clamp(color.G * intensity, 0, 255);
        color.B = (byte)Math.Clamp(color.B * intensity, 0, 255);

        return color;
    }

    public static MyColor operator *(byte intensity, MyColor color) => color * intensity;

    public static MyColor operator *(MyColor color, float intensity)
    {
        color.R = (byte)Math.Clamp(color.R * intensity, 0, 255);
        color.G = (byte)Math.Clamp(color.G * intensity, 0, 255);
        color.B = (byte)Math.Clamp(color.B * intensity, 0, 255);

        return color;
    }

    public static MyColor operator *(float intensity, MyColor color) => color * intensity;

    public static MyColor operator +(MyColor colorA, MyColor colorB)
    {
        var color = new MyColor
        {
            R = (byte)Math.Clamp(colorA.R + colorB.R, 0, 255),
            G = (byte)Math.Clamp(colorA.G + colorB.G, 0, 255),
            B = (byte)Math.Clamp(colorA.B + colorB.B, 0, 255)
        };

        return color;
    }
}

class Sphere()
{
    public Vector3 Center { get; set; }

    public float Radius { get; set; }

    public MyColor Color { get; set; }
}