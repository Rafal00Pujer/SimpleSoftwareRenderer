// See https://aka.ms/new-console-template for more information

using SimpleSoftwareRenderer;
using System.Diagnostics;
using System.Numerics;

Console.WriteLine("Hello, World!");

using var window = new Window(nameof(SimpleSoftwareRenderer), 600, 400, 854, 480);

var (canvasWidth, canvasHeight) = window.FrameSize;

var pixels = new byte[canvasHeight, canvasWidth, 3];

var scene = CreateScene();

var raytracing = new SimpleRaytracing(scene, pixels);

var oneSecond = TimeSpan.FromSeconds(1);
var stopwatch = Stopwatch.StartNew();

while (!window.Quit)
{
    window.Run();

    raytracing.RenderScene();

    window.Draw(pixels);

    Console.Clear();
    Console.WriteLine($"FPS: {oneSecond / stopwatch.Elapsed}");
    stopwatch.Restart();
}

static Scene CreateScene()
{
    var scene = new Scene()
    {
        Spheres =
        [
            new()
            {
                Center = new Vector3(0.0f, -1.0f, 3.0f),
                Radius = 1.0f,
                Color = new MyColor { R = 255 },
                Specular = 500.0f
            },
            new()
            {
                Center = new Vector3(2.0f, 0.0f, 4.0f),
                Radius = 1.0f,
                Color = new MyColor { B = 255 },
                Specular = 500.0f
            },
            new()
            {
                Center = new Vector3(-2.0f, 0.0f, 4.0f),
                Radius = 1.0f,
                Color = new MyColor { G = 255 },
                Specular = 10.0f
            },
            new()
            {
                Center = new Vector3(0.0f, -5001.0f, 0.0f),
                Radius = 5000.0f,
                Color = new MyColor { R = 255, G = 255 },
                Specular = 1000.0f
            }
        ],
        Lights =
        [
            new()
            {
                Type = LightType.Ambient,
                Intensity = 0.2f
            },
            new()
            {
                Type = LightType.Point,
                Intensity = 0.6f,
                Position = new Vector3(2.0f, 1.0f, 0.0f)
            },
            new()
            {
                Type = LightType.Directional,
                Intensity = 0.2f,
                Direction = new Vector3(1.0f, 4.0f, 4.0f)
            }
        ]
    };

    return scene;
}