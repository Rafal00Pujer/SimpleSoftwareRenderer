using System.Numerics;

namespace SimpleSoftwareRenderer.Rasterization;

internal class Model
{
    public List<Vector3> Vertices { get; set; }

    public List<Triangle> Triangles { get; set; }
}
