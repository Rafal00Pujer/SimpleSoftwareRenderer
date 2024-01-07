// See https://aka.ms/new-console-template for more information
using SimpleSoftwareRenderer;
using System.Diagnostics;

Console.WriteLine("Hello, World!");

using var window = new Window(nameof(SimpleSoftwareRenderer), 600, 400, 600, 600);

var (screenWidth, screenHeight) = window.FrameSize;

var pixels = new byte[screenHeight, screenWidth, 3];

var oneSecond = TimeSpan.FromSeconds(1);
var stopwatch = Stopwatch.StartNew();

while (!window.Quit)
{
    window.Run();

    window.Draw(pixels);

    Console.Clear();
    Console.WriteLine($"FPS: {oneSecond / stopwatch.Elapsed}");
    stopwatch.Restart();
}

static float ToRadians(float angleInDegress)
{
    return (float)(Math.PI / 180.0) * angleInDegress;
}