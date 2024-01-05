using System.Numerics;

namespace SimpleSoftwareRenderer.Raytracing;

internal class SimpleRaytracing(Scene scene, byte[,,] screenPixels)
{
    // I need bigger epsilon to prevent rendering artifacts
    private const float Epsilon = 0.1f;
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

    private Color TraceRay(Vector3 origin, Vector3 direction, float minT, float MaxT, int depth)
    {
        var (closestSphere, closestT) = ClosestIntersection(origin, direction, minT, MaxT);

        if (closestSphere is null)
        {
            return new Color { R = 0.0f, G = 0.0f, B = 0.0f };
        }

        var point = origin + closestT!.Value * direction; // Compute intersection
        var normal = point - closestSphere.Center; // Compute sphere normal at intersection
        normal = Vector3.Normalize(normal);

        var view = direction * -1.0f;
        var lighting = ComputeLighting(point, normal, view, closestSphere.Specular);
        var localColor = closestSphere.Color * lighting;

        // If we hit the recursion limit or the object is not reflective, we're done
        if (closestSphere.Reflective <= 0.0f || depth <= 0)
        {
            return localColor;
        }

        // Compute the reflected color
        var reflectedRay = ReflectRay(view, normal);
        var reflectedColor = TraceRay(point, reflectedRay, Epsilon, float.PositiveInfinity, depth - 1);

        return localColor * (1.0f - closestSphere.Reflective) + reflectedColor * closestSphere.Reflective;
    }

    private (Sphere? closestShpere, float? closestT) ClosestIntersection(Vector3 origin, Vector3 direction, float minT, float maxT)
    {
        var closestT = float.PositiveInfinity;
        Sphere? closestSphere = null;

        foreach (var sphere in _scene.Spheres)
        {
            var (t1, t2) = IntersectRaySphere(origin, direction, sphere);

            if (minT < t1 && t1 < maxT && t1 < closestT)
            {
                closestT = t1;
                closestSphere = sphere;
            }

            if (minT < t2 && t2 < maxT && t2 < closestT)
            {
                closestT = t2;
                closestSphere = sphere;
            }
        }

        if (closestSphere is null)
        {
            return (null, null);
        }

        return (closestSphere, closestT);
    }

    private static (float t1, float t2) IntersectRaySphere(Vector3 origin, Vector3 direction, Sphere sphere)
    {
        var oc = origin - sphere.Center;

        var k1 = Vector3.Dot(direction, direction);
        var k2 = 2.0f * Vector3.Dot(oc, direction);
        var k3 = Vector3.Dot(oc, oc) - sphere.Radius * sphere.Radius;

        var discriminant = k2 * k2 - 4 * k1 * k3;

        if (discriminant < 0)
        {
            return (float.PositiveInfinity, float.PositiveInfinity);
        }

        var t1 = (-k2 + (float)Math.Sqrt(discriminant)) / (2.0f * k1);
        var t2 = (-k2 - (float)Math.Sqrt(discriminant)) / (2.0f * k1);

        return (t1, t2);
    }

    private float ComputeLighting(Vector3 point, Vector3 normal, Vector3 view, float specular)
    {
        var intensity = 0.0f;
        var lengthN = normal.Length();
        var lengthV = view.Length();

        foreach (var light in _scene.Lights)
        {
            if (light.Type == LightType.Ambient)
            {
                intensity += light.Intensity;
            }
            else
            {
                Vector3 vecl;
                float tMax;

                if (light.Type == LightType.Point)
                {
                    vecl = light.Position!.Value - point;
                    tMax = 1.0f;
                }
                else
                {
                    vecl = light.Direction!.Value;
                    tMax = float.PositiveInfinity;
                }

                // Shadow check
                var (shadowSphere, shadowT) = ClosestIntersection(point, vecl, Epsilon, tMax);
                if (shadowSphere is not null)
                {
                    continue;
                }

                // Diffuse
                var nDotL = Vector3.Dot(normal, vecl);
                if (nDotL > 0)
                {
                    intensity += light.Intensity * nDotL / (lengthN * vecl.Length());
                }

                // Specular
                if (specular != -1.0f)
                {
                    var vecR = ReflectRay(vecl, normal);
                    var rDotV = Vector3.Dot(vecR, view);

                    if (rDotV > 0)
                    {
                        var powBase = rDotV / (vecR.Length() * lengthV);
                        intensity += light.Intensity * (float)Math.Pow(powBase, specular);
                    }
                }
            }
        }

        return intensity;
    }

    private static Vector3 ReflectRay(Vector3 v1, Vector3 v2)
    {
        return v2 * (2.0f * Vector3.Dot(v1, v2)) - v1;
    }

    private void PutPixel(int x, int y, Color color)
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

    private static void PopulatePixel(Color color, int x, int y, byte[,,] pixels)
    {
        pixels[y, x, 0] = (byte)Math.Clamp(color.R, 0.0, 255.0);
        pixels[y, x, 1] = (byte)Math.Clamp(color.G, 0.0, 255.0);
        pixels[y, x, 2] = (byte)Math.Clamp(color.B, 0.0, 255.0);
    }
}
