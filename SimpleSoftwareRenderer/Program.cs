// See https://aka.ms/new-console-template for more information

using SimpleSoftwareRenderer;
using System.Diagnostics;
using System.Numerics;

Console.WriteLine("Hello, World!");

using var window = new Window(nameof(SimpleSoftwareRenderer), 600, 400, 854, 480);

var (canvasWidth, canvasHeight) = window.FrameSize;

var pixels = new byte[canvasHeight, canvasWidth, 3];

var scene = new List<Sphere>
{
    new()
    {
        Center = new Vector3(0.0f, -1.0f, 3.0f),
        Radius = 1.0f,
        Color = new MyColor{R = 255}
    },
    new()
    {
        Center = new Vector3(2.0f, 0.0f, 4.0f),
        Radius = 1.0f,
        Color = new MyColor{B = 255}
    },
    new()
    {
        Center = new Vector3(-2.0f, 0.0f, 4.0f),
        Radius = 1.0f,
        Color = new MyColor{G = 255}
    }
};

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