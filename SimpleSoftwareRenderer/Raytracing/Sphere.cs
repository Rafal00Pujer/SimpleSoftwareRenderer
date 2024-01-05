using System.Numerics;

namespace SimpleSoftwareRenderer.Raytracing;

class Sphere()
{
    public Vector3 Center { get; set; }

    public float Radius { get; set; }

    public Color Color { get; set; }

    public float Specular { get; set; }

    public float Reflective { get; set; }
}