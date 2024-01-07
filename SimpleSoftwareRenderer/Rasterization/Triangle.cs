using System.Numerics;

namespace SimpleSoftwareRenderer.Rasterization;

internal struct Triangle(int vertexAIndex, int vertexBIndex, int vertexCIndex, Color color)
{
    public int VertexAIndex { get; set; } = vertexAIndex;

    public int VertexBIndex { get; set; } = vertexBIndex;

    public int VertexCIndex { get; set; } = vertexCIndex;

    public Color Color { get; set; } = color;

    public List<Vector3> Normals;

    public List<Vector2> Uvs;
}
