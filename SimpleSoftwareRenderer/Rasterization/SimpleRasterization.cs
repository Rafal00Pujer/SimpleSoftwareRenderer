using System.Numerics;

namespace SimpleSoftwareRenderer.Rasterization;

internal class SimpleRasterization(Scene scene, byte[,,] screenPixels)
{
    private const float ViewportWidth = 1.0f;
    private const float ViewportHeight = 1.0f;
    private const float CameraToViewport = 1.0f;

    private readonly Scene _scene = scene;
    private readonly byte[,,] _screenPixels = screenPixels;

    private readonly int _screenWidth = screenPixels.GetLength(1);
    private readonly int _screenHeight = screenPixels.GetLength(0);

    private readonly Vector3 _cameraPosition = Vector3.Zero;

    public void RenderScene()
    {
        foreach (var instance in _scene.Instances)
        {
            RenderModel(instance);
        }
    }

    private void RenderModel(ModelInstance instance)
    {
        var model = instance.Model;

        var projected = new List<Vector2>();

        foreach (var vertex in model.Vertices)
        {
            var translatedVertex = Matrix4x4.CreateTranslation(vertex) * instance.Transform;
            var projectedVertex = ProjectVertex(new Vector3(translatedVertex.M41, translatedVertex.M42, translatedVertex.M43));

            projected.Add(projectedVertex);
        }

        foreach (var triangle in model.Triangles)
        {
            RenderTriangle(triangle, projected);
        }
    }

    private void RenderTriangle(Triangle triangle, List<Vector2> projected)
    {
        DrawWireFrameTriangle(
            new Vector3(projected[triangle.VertexAIndex], 0.0f),
            new Vector3(projected[triangle.VertexBIndex], 0.0f),
            new Vector3(projected[triangle.VertexCIndex], 0.0f),
            triangle.Color);
    }

    private Vector2 ProjectVertex(Vector3 vertex)
    {
        var viewportX = vertex.X * CameraToViewport / vertex.Z;
        var viewportY = vertex.Y * CameraToViewport / vertex.Z;

        var (canvasX, canvasY) = ViewportToCanvas(viewportX, viewportY);

        return new Vector2(canvasX, canvasY);
    }

    private (int canvasX, int canvasY) ViewportToCanvas(float viewportX, float viewportY)
    {
        var canvasX = viewportX * _screenWidth / ViewportWidth;
        var canvasY = viewportY * _screenHeight / ViewportHeight;

        return ((int)canvasX, (int)canvasY);
    }

    private void DrawWireFrameTriangle(Vector3 p0, Vector3 p1, Vector3 p2, Color color)
    {
        DrawLine(p0, p1, color);
        DrawLine(p1, p2, color);
        DrawLine(p2, p0, color);
    }

    private void DrawShadedTriangle(Vector3 p0, Vector3 p1, Vector3 p2, Color color)
    {
        SortTriangleVertices(ref p0, ref p1, ref p2);

        // Compute the x coordinates of the triangle edges
        var x01 = Interpolate(p0.Y, p0.X, p1.Y, p1.X);
        var h01 = Interpolate(p0.Y, p0.Z, p1.Y, p1.Z);

        var x12 = Interpolate(p1.Y, p1.X, p2.Y, p2.X);
        var h12 = Interpolate(p1.Y, p1.Z, p2.Y, p2.Z);

        var x02 = Interpolate(p0.Y, p0.X, p2.Y, p2.X);
        var h02 = Interpolate(p0.Y, p0.Z, p2.Y, p2.Z);


        // Concatenate the short sides
        x01.RemoveAt(x01.Count - 1);
        var x012 = x01.Concat(x12).ToList();

        h01.RemoveAt(h01.Count - 1);
        var h012 = h01.Concat(h12).ToList();

        List<float> xLeft;
        List<float> xRight;

        List<float> hLeft;
        List<float> hRight;

        // Determine which is left and which is right
        var m = x012.Count / 2;
        if (x02[m] < x012[m])
        {
            xLeft = x02;
            hLeft = h02;

            xRight = x012;
            hRight = h012;
        }
        else
        {
            xLeft = x012;
            hLeft = h012;

            xRight = x02;
            hRight = h02;
        }

        // Draw the horizontal segments
        for (var y = p0.Y; y <= p2.Y; y++)
        {
            var xL = xLeft[(int)(y - p0.Y)];
            var xR = xRight[(int)(y - p0.Y)];

            var hSegment = Interpolate(xL, hLeft[(int)(y - p0.Y)], xR, hRight[(int)(y - p0.Y)]);

            for (var x = xL; x <= xR; x++)
            {
                var shadedColor = color * hSegment[(int)(x - xL)];

                PutPixel((int)x, (int)y, shadedColor);
            }
        }
    }

    private static void SortTriangleVertices(ref Vector3 p0, ref Vector3 p1, ref Vector3 p2)
    {
        if (p1.Y < p0.Y)
        {
            (p1, p0) = (p0, p1);
        }

        if (p2.Y < p0.Y)
        {
            (p2, p0) = (p0, p2);
        }

        if (p2.Y < p1.Y)
        {
            (p2, p1) = (p1, p2);
        }
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
        var sY = halfHeight + y;

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
