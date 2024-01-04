using System.Numerics;

namespace SimpleSoftwareRenderer;

class Sphere()
{
    public Vector3 Center { get; set; }

    public float Radius { get; set; }

    public MyColor Color { get; set; }

    public float Specular { get; set; }

    public float Reflective { get; set; }
}