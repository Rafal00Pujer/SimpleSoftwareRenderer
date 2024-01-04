using System.Numerics;

namespace SimpleSoftwareRenderer;

internal class SimpleRaytracing(Scene scene, byte[,,] screenPixels)
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
        for (var x = -_screenWidth / 2; x < _screenWidth / 2; x++)
        {
            for (var y = -_screenHeight / 2; y < _screenHeight / 2; y++)
            {
                var direction = CanvasToViewport(x, y);

                var color = TraceRay(_cameraPosition, direction, 1.0f, float.PositiveInfinity, 3);
                PutPixel(x, y, color);

            }
        }
    }

    private Vector3 CanvasToViewport(int x, int y)
    {
        return new Vector3(x * ViewportWidth / _screenWidth, y * ViewportHeight / _screenHeight, CameraToViewport);
    }

    private MyColor TraceRay(Vector3 O, Vector3 D, float tMin, float tMax, int recursionDepth)
    {
        var (closestSphere, closestT) = ClosestIntersection(O, D, tMin, tMax);

        if (closestSphere is null)
        {
            return new MyColor { R = 0, G = 0, B = 0 };
        }

        var P = O + closestT * D; // Compute intersection
        var N = P - closestSphere.Center; // Compute sphere normal at intersection
        N = Vector3.Normalize(N);

        MyColor localColor = closestSphere.Color * ComputeLighting(P, N, D * -1.0f, closestSphere.Specular);

        // If we hit the recursion limit or the object is not reflective, we're done
        var r = closestSphere.Reflective;
        if (recursionDepth <= 0 || r <= 0)
        {
            return localColor;
        }

        // Compute the reflected color
        var R = ReflectRay(D * -1.0f, N);
        var reflectedColor = TraceRay(P, R, float.Epsilon, float.PositiveInfinity, recursionDepth - 1);

        return localColor * (1.0f - r) + reflectedColor * r;
    }

    private (Sphere? closestShpere, float closestT) ClosestIntersection(Vector3 origin, Vector3 direction, float tMin, float tMax)
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

        return (closestSphere, closestT);
    }

    private static (float t1, float t2) IntersectRaySphere(Vector3 origin, Vector3 direction, Sphere sphere)
    {
        double r = sphere.Radius;
        var oc = origin - sphere.Center;

        double a = Vector3.Dot(direction, direction);
        double b = 2.0 * Vector3.Dot(oc, direction);
        double c = Vector3.Dot(oc, oc) - r * r;

        var discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            return (float.PositiveInfinity, float.PositiveInfinity);
        }

        var t1 = (-b + Math.Sqrt(discriminant)) / (2.0 * a);
        var t2 = (-b - Math.Sqrt(discriminant)) / (2.0 * a);

        return ((float)t1, (float)t2);
    }

    private float ComputeLighting(Vector3 p, Vector3 n, Vector3 v, float s)
    {
        var i = 0.0f;

        foreach (var light in _scene.Lights)
        {
            if (light.Type == LightType.Ambient)
            {
                i += light.Intensity;
                continue;
            }

            Vector3 l;
            float tMax;

            if (light.Type == LightType.Point)
            {
                l = light.Position!.Value - p;
                tMax = 1.0f;
            }
            else
            {
                l = light.Direction!.Value;
                tMax = float.PositiveInfinity;
            }

            // Shadow check
            var (shadowSphere, shadowT) = ClosestIntersection(p, l, float.Epsilon, tMax);
            if (shadowSphere is not null)
            {
                continue;
            }

            // Diffuse
            var nDotL = Vector3.Dot(n, l);
            if (nDotL > 0)
            {
                i += light.Intensity * nDotL / (n.Length() * l.Length());
            }

            // Specular
            if (s != -1.0f)
            {
                var r = ReflectRay(l, n);
                var rDotV = Vector3.Dot(r, v);

                if (rDotV > 0)
                {
                    var powBase = rDotV / (r.Length() * v.Length());
                    i += light.Intensity * (float)Math.Pow(powBase, s);
                }
            }
        }

        return i;
    }

    private static Vector3 ReflectRay(Vector3 r, Vector3 n)
    {
        return 2 * n * Vector3.Dot(n, r) - r;
    }

    private void PutPixel(int x, int y, MyColor color)
    {
        var halfHeight = _screenHeight / 2;
        var halfWidth = _screenWidth / 2;

        var sX = halfWidth + x;
        var sY = halfHeight + y;

        if (sX < 0 || sX > _screenWidth
            || sY < 0 || sY > _screenHeight)
        {
            return;
        }

        PopulatePixel(color, sX, sY, _screenPixels);
    }

    private static void PopulatePixel(MyColor color, int x, int y, byte[,,] pixels)
    {
        pixels[y, x, 0] = (byte)Math.Clamp(color.R, 0.0, 255.0);
        pixels[y, x, 1] = (byte)Math.Clamp(color.G, 0.0, 255.0);
        pixels[y, x, 2] = (byte)Math.Clamp(color.B, 0.0, 255.0);
    }
}
