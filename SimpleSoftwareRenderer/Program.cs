// See https://aka.ms/new-console-template for more information

using SimpleSoftwareRenderer;

Console.WriteLine("Hello, World!");

using var window = new Window(nameof(SimpleSoftwareRenderer), 600, 400, 100, 100);

var (width, height) = window.FrameSize;

var pixels = new byte[height, width, 3];

while (!window.Quit)
{
    window.Run();

    window.Draw(pixels);
}