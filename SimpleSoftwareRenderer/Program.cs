// See https://aka.ms/new-console-template for more information

using SimpleSoftwareRenderer;

Console.WriteLine("Hello, World!");

using var window = new Window(640, 300, 640, 480, "Test window");

window.Run();