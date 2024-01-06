using System.Numerics;

namespace SimpleSoftwareRenderer.Rasterization;

internal class SimpleRasterization(Scene scene, byte[,,] screenPixels)
{
    private const float ViewportWidth = 1.0f;
    private const float ViewportHeight = 1.0f;

    private readonly Scene _scene = scene;
    private readonly byte[,,] _screenPixels = screenPixels;

    private readonly int _screenWidth = screenPixels.GetLength(1);
    private readonly int _screenHeight = screenPixels.GetLength(0);
    private readonly int[] _depthBuffer = new int[screenPixels.GetLength(1) * screenPixels.GetLength(0)];

    private float _cameraToViewport;
    private Matrix4x4 _cameraTransform;
    private Vector3 _cameraPosition;

    public void RenderScene(Matrix4x4 cameraTransform, Vector3 cameraPosition, float cameraFovInDegress)
    {
        _cameraToViewport = ViewportWidth / 2.0f / (float)Math.Tan(Math.PI / 180.0 * (cameraFovInDegress / 2.0f));
        _cameraTransform = cameraTransform;
        _cameraPosition = cameraPosition;

        for (var i = 0; i < _depthBuffer.Length; i++)
        {
            _depthBuffer[i] = -1;
        }

        var planes = new List<Plane>
        {
            new (Vector3.Normalize(new Vector3(0.0f, 0.0f, 1.0f)), _cameraToViewport),
            new (Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)), 0.0f),
            new (Vector3.Normalize(new Vector3(-1.0f, 0.0f, 1.0f)), 0.0f),
            new (Vector3.Normalize(new Vector3(0.0f, 1.0f, 1.0f)), 0.0f),
            new (Vector3.Normalize(new Vector3(0.0f, -1.0f, 1.0f)), 0.0f)
        };

        foreach (var instance in _scene.Instances)
        {
            var finalTransform = instance.Transform * _cameraTransform;

            var clipedModel = TransformAndClip(planes, instance.Model, finalTransform);

            if (clipedModel is null)
            {
                continue;
            }

            RenderModel(clipedModel);
        }
    }

    private bool UpdateDepthBufferIfCloser(int x, int y, int invZ)
    {
        var halfHeight = _screenHeight / 2;
        var halfWidth = _screenWidth / 2;

        x += halfWidth;
        y += halfHeight - 1;

        if (x < 0 || x >= _screenWidth
            || y < 0 || y >= _screenHeight)
        {
            return false;
        }

        var offset = x + _screenWidth * y;
        if (_depthBuffer[offset] == -1
            || _depthBuffer[offset] < invZ)
        {
            _depthBuffer[offset] = invZ;
            return true;
        }

        return false;
    }

    private static Model? TransformAndClip(List<Plane> planes, Model model, Matrix4x4 transform)
    {
        var vertices = new List<Vector3>();

        // Apply modelview transform.
        foreach (var vertex in model.Vertices)
        {
            vertices.Add(Vector3.Transform(vertex, transform));
        }

        // Clip the entire model against each successive plane.
        var triangles = model.Triangles.ToList();

        foreach (var plane in planes)
        {
            triangles = triangles.SelectMany(triangle =>
                ClipTriangle(triangle, plane, vertices))
                    .ToList();
        }

        if (triangles.Count == 0)
        {
            return null;
        }

        return new Model
        {
            Vertices = vertices,
            Triangles = triangles
        };
    }

    private static IEnumerable<Triangle> ClipTriangle(Triangle triangle, Plane plane, List<Vector3> vertices)
    {
        var inV = new List<(Vector3 vertex, int index)>();
        var outV = new List<(Vector3 vertex, int index)>();

        ClipVertex(plane, vertices[triangle.VertexAIndex], triangle.VertexAIndex);
        ClipVertex(plane, vertices[triangle.VertexBIndex], triangle.VertexBIndex);
        ClipVertex(plane, vertices[triangle.VertexCIndex], triangle.VertexCIndex);

        switch (inV.Count)
        {
            // Nothing to do - the triangle is fully clipped out.
            //case 0:
            //    break;

            // The triangle has one vertex in. Output is one clipped triangle.
            case 1:
                {
                    var newB = Intersect(inV[0].vertex, outV[0].vertex);
                    var newBIndex = vertices.Count;
                    vertices.Add(newB);

                    var newC = Intersect(inV[0].vertex, outV[1].vertex);
                    var newCIndex = vertices.Count;
                    vertices.Add(newC);

                    yield return new Triangle(inV[0].index, newBIndex, newCIndex, triangle.Color);

                    break;
                }

            // The triangle has two vertices in. Output is two clipped triangles.
            case 2:
                {
                    var newA = Intersect(inV[0].vertex, outV[0].vertex);
                    var newAIndex = vertices.Count;
                    vertices.Add(newA);

                    var newB = Intersect(inV[1].vertex, outV[0].vertex);
                    var newBIndex = vertices.Count;
                    vertices.Add(newB);

                    yield return new Triangle(inV[0].index, inV[1].index, newAIndex, triangle.Color);
                    yield return new Triangle(newAIndex, inV[1].index, newBIndex, triangle.Color);

                    break;
                }

            // The triangle is fully in front of the plane.
            case 3:
                yield return triangle;
                break;
        }

        void ClipVertex(Plane plane, Vector3 vertex, int index)
        {
            if (Plane.DotCoordinate(plane, vertex) > 0)
            {
                inV.Add((vertex, index));
            }
            else
            {
                outV.Add((vertex, index));
            }
        }

        Vector3 Intersect(Vector3 a, Vector3 b)
        {
            var t = ((plane.D * -1.0f) - Plane.DotNormal(plane, a))
            / Plane.DotNormal(plane, b - a);

            return Vector3.Lerp(a, b, t);
        }
    }

    private void RenderModel(Model model)
    {
        var projected = new List<Vector2>();

        foreach (var vertex in model.Vertices)
        {
            var projectedVertex = ProjectVertex(vertex);

            projected.Add(projectedVertex);
        }

        foreach (var triangle in model.Triangles)
        {
            RenderTriangle(triangle, model.Vertices, projected);
        }
    }

    private void RenderTriangle(Triangle triangle, List<Vector3> vertices, List<Vector2> projected)
    {
        // Sort by projected point Y.
        int i0 = triangle.VertexAIndex;
        int i1 = triangle.VertexBIndex;
        int i2 = triangle.VertexCIndex;

        if (projected[i1].Y < projected[i0].Y)
        {
            (i0, i1) = (i1, i0);
        }

        if (projected[i2].Y < projected[i0].Y)
        {
            (i0, i2) = (i2, i0);
        }

        if (projected[i2].Y < projected[i1].Y)
        {
            (i1, i2) = (i2, i1);
        }

        var v0 = vertices[i0];
        var v1 = vertices[i1];
        var v2 = vertices[i2];

        // Compute triangle normal. Use the unsorted vertices, otherwise the winding of the points may change.
        var normal = ComputeTriangleNormal(
            vertices[triangle.VertexAIndex],
            vertices[triangle.VertexBIndex],
            vertices[triangle.VertexCIndex]);

        // Backface culling.
        var vertexToCamera = vertices[triangle.VertexAIndex] * -1.0f;

        if (Vector3.Dot(vertexToCamera, normal) <= 0)
        {
            return;
        }

        // Get attribute values (X, 1/Z) at the vertices.
        var p0 = projected[i0];
        var p1 = projected[i1];
        var p2 = projected[i2];

        // Compute attribute values at the edges.
        var (x02, x012) = EdgeInterpolate(p0.Y, p0.X, p1.Y, p1.X, p2.Y, p2.X);
        var (iz02, iz012) = EdgeInterpolate(p0.Y, 1.0f / v0.Z, p1.Y, 1.0f / v1.Z, p2.Y, 1.0f / v2.Z);

        // Determine which is left and which is right.
        var m = x02.Count / 2;
        if (x02[m] >= x012[m])
        {
            (x012, x02) = (x02, x012);
            (iz012, iz02) = (iz02, iz012);
        }

        // Draw horizontal segments.
        for (var y = p0.Y; y <= p2.Y; y++)
        {
            var xL = x02[(int)(y - p0.Y)];
            var xR = x012[(int)(y - p0.Y)];

            // Interpolate attributes for this scanline.
            var zL = iz02[(int)(y - p0.Y)];
            var zR = iz012[(int)(y - p0.Y)];

            var zscan = Interpolate(xL, zL, xR, zR);

            for (var x = xL; x <= xR; x++)
            {
                if (UpdateDepthBufferIfCloser((int)x, (int)y, (int)zscan[(int)(x - xL)]))
                {
                    PutPixel((int)x, (int)y, triangle.Color);
                }
            }
        }
    }

    private static Vector3 ComputeTriangleNormal(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        var v0v1 = v1 + (v0 * -1.0f);
        var v0v2 = v2 + (v0 * -1.0f);

        return Vector3.Cross(v0v1, v0v2);
    }

    private Vector2 ProjectVertex(Vector3 vertex)
    {
        var viewportX = vertex.X * _cameraToViewport / vertex.Z;
        var viewportY = vertex.Y * _cameraToViewport / vertex.Z;

        var (canvasX, canvasY) = ViewportToCanvas(viewportX, viewportY);

        return new Vector2(canvasX, canvasY);
    }

    private (int canvasX, int canvasY) ViewportToCanvas(float viewportX, float viewportY)
    {
        var canvasX = viewportX * _screenWidth / ViewportWidth;
        var canvasY = viewportY * _screenHeight / ViewportHeight;

        return ((int)canvasX, (int)canvasY);
    }

    private void DrawLine(Vector3 p0, Vector3 p1, Color color)
    {
        // Line is horizontal-ish
        if (Math.Abs(p1.X - p0.X) > Math.Abs(p1.Y - p0.Y))
        {
            // Make sure x0 < x1
            if (p0.X > p1.X)
            {
                (p0, p1) = (p1, p0);
            }

            var ys = Interpolate(p0.X, p0.Y, p1.X, p1.Y);

            for (var x = p0.X; x <= p1.X; x++)
            {
                PutPixel((int)x, (int)ys[(int)x - (int)p0.X], color);
            }
        }
        // Line is vertical-ish
        else
        {
            // Make sure y0 < y1
            if (p0.Y > p1.Y)
            {
                (p0, p1) = (p1, p0);
            }

            var xs = Interpolate(p0.Y, p0.X, p1.Y, p1.X);

            for (var y = p0.Y; y <= p1.Y; y++)
            {
                PutPixel((int)xs[(int)y - (int)p0.Y], (int)y, color);
            }
        }
    }

    private static (List<float> v02, List<float> v012) EdgeInterpolate(float y0, float v0, float y1, float v1, float y2, float v2)
    {
        var v01 = Interpolate(y0, v0, y1, v1);
        var v12 = Interpolate(y1, v1, y2, v2);
        var v02 = Interpolate(y0, v0, y2, v2);

        v01.RemoveAt(v01.Count - 1);

        var v012 = v01.Concat(v12).ToList();

        return (v02, v012);
    }

    private static List<float> Interpolate(float i0, float d0, float i1, float d1)
    {
        if (i0 == i1)
        {
            return [d0];
        }

        var values = new List<float>();
        var a = (d1 - d0) / (i1 - i0);
        var d = d0;

        for (var i = i0; i <= i1; i++)
        {
            values.Add(d);
            d += a;
        }

        return values;
    }

    private void PutPixel(int x, int y, Color color)
    {
        var halfHeight = _screenHeight / 2;
        var halfWidth = _screenWidth / 2;

        var sX = halfWidth + x;
        var sY = halfHeight + y - 1;

        if (sX < 0 || sX >= _screenWidth
            || sY < 0 || sY >= _screenHeight)
        {
            return;
        }

        PopulatePixel(color, sX, sY, _screenPixels);
    }

    private static void PopulatePixel(Color color, int x, int y, byte[,,] pixels)
    {
        pixels[y, x, 0] = (byte)Math.Clamp(color.R, 0.0, 255.0);
        pixels[y, x, 1] = (byte)Math.Clamp(color.G, 0.0, 255.0);
        pixels[y, x, 2] = (byte)Math.Clamp(color.B, 0.0, 255.0);
    }
}
