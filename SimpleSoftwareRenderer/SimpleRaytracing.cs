using System.Numerics;

namespace SimpleSoftwareRenderer;

internal class SimpleRaytracing(Scene scene, byte[,,] screenPixels)
{
    private const float ViewportWidth = 1.0f;
    private const float ViewportHeight = 1.0f;
    private const float CameraToViewport = 1.0f;

    private readonly Scene _scene = scene;
    private readonly byte[,,] _screenPixels = screenPixels;

    private readonly int ScreenWidth = screenPixels.GetLength(1);
    private readonly int ScreenHeight = screenPixels.GetLength(0);

    private readonly Vector3 CameraPosition = Vector3.Zero;

    public void RenderScene()
    {
        for (var x = -ScreenWidth / 2; x < ScreenWidth / 2; x++)
        {
            for (var y = -ScreenHeight / 2; y < ScreenHeight / 2; y++)
            {
                var direction = CanvasToViewport(x, y);

                var color = TraceRay(CameraPosition, direction, 1.0f, float.PositiveInfinity);
                PutPixel(x, y, color);

            }
        }
    }

    private Vector3 CanvasToViewport(int x, int y)
    {
        return new Vector3(x * ViewportWidth / ScreenWidth, y * ViewportHeight / ScreenHeight, CameraToViewport);
    }

    private MyColor TraceRay(Vector3 origin, Vector3 direction, float tMin, float tMax)
    {
        var closestT = float.PositiveInfinity;
        Sphere? closestSphere = null;

        foreach (var sphere in _scene.Spheres)
        {
            var (t1, t2) = IntersectRaySphere(origin, direction, sphere);

            if (tMin < t1 && t1 < tMax && t1 < closestT)
            {
                closestT = t1;
                closestSphere = sphere;
            }

            if (tMin < t2 && t2 < tMax && t2 < closestT)
            {
                closestT = t2;
                closestSphere = sphere;
            }
        }

        if (closestSphere is null)
        {
            return new MyColor { R = 255, G = 255, B = 255 };
        }

        var intersection = origin + closestT * direction; // Compute intersection

        var surfaceNormal = intersection - closestSphere.Center; // Compute sphere normal at intersection
        surfaceNormal = Vector3.Normalize(surfaceNormal);

        return closestSphere.Color * ComputeLighting(intersection, surfaceNormal);
    }

    private static (float t1, float t2) IntersectRaySphere(Vector3 origin, Vector3 direction, Sphere sphere)
    {
        var r = sphere.Radius;
        var co = origin - sphere.Center;

        var a = Vector3.Dot(direction, direction);
        var b = 2 * Vector3.Dot(co, direction);
        var c = Vector3.Dot(co, co) - r * r;

        var discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            return (float.PositiveInfinity, float.PositiveInfinity);
        }

        float t1 = (-b + (float)Math.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - (float)Math.Sqrt(discriminant)) / (2 * a);

        return (t1, t2);
    }

    private float ComputeLighting(Vector3 intersection, Vector3 surfaceNormal)
    {
        var intensity = 0.0f;

        foreach (var light in _scene.Lights)
        {
            if (light.Type == LightType.Ambient)
            {
                intensity += light.Intensity;
                continue;
            }

            Vector3 l;

            if (light.Type == LightType.Point)
            {
                l = light.Position!.Value - intersection;
            }
            else
            {
                l = light.Direction!.Value;
            }

            var nDotL = Vector3.Dot(surfaceNormal, l);

            if (nDotL > 0)
            {
                intensity += light.Intensity * nDotL / (surfaceNormal.Length() * intersection.Length());
            }
        }

        return intensity;
    }

    private void PutPixel(int x, int y, MyColor color)
    {
        var halfHeight = ScreenHeight / 2;
        var halfWidth = ScreenWidth / 2;

        var sX = halfWidth + x;
        var sY = halfHeight + y;

        if (sX < 0 || sX > ScreenWidth
            || sY < 0 || sY > ScreenHeight)
        {
            return;
        }

        PopulatePixel(color, sX, sY, _screenPixels);
    }

    private static void PopulatePixel(MyColor color, int x, int y, byte[,,] pixels)
    {
        pixels[y, x, 0] = color.R;
        pixels[y, x, 1] = color.G;
        pixels[y, x, 2] = color.B;
    }
}
