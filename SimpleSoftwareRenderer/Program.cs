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

    //LinesTest();

    window.Draw(pixels);

    Console.Clear();
    Console.WriteLine($"FPS: {oneSecond / stopwatch.Elapsed}");
    stopwatch.Restart();
}

void ProjectionTest()
{

}

void LinesTest()
{
    // naive
    // shallow, + slope
    DrawLineNaive(new Color { G = 255.0f }, 20, 420, 32, 128);

    // steep, + slope
    DrawLineNaive(new Color { G = 255.0f }, 20, 420, 32, 599);

    // shallow, - slope
    DrawLineNaive(new Color { G = 255.0f }, 420, 20, 32, 128);

    // steep, - slope
    DrawLineNaive(new Color { G = 255.0f }, 420, 20, 32, 599);

    // bressenham
    // shallow, + slope
    DrawLineBresenham(new Color { B = 255.0f }, 220, 620, 32, 128);

    // steep, + slope
    DrawLineBresenham(new Color { B = 255.0f }, 220, 620, 32, 599);

    // shallow, - slope
    DrawLineBresenham(new Color { B = 255.0f }, 620, 220, 32, 128);

    // steep, - slope
    DrawLineBresenham(new Color { B = 255.0f }, 620, 220, 32, 599);
}

void DrawLineBresenham(Color color, int x1, int x2, int y1, int y2)
{
    if (x1 == x2)
    {
        if (y1 < y2)
        {
            DrawVerticalLine(color, x1, y1, y2);
        }
        else
        {
            DrawVerticalLine(color, x1, y2, y1);
        }

        return;
    }

    if (y1 == y2)
    {
        if (x1 < x2)
        {
            DrawHorizontalLine(color, x1, x2, y1);
        }
        else
        {
            DrawHorizontalLine(color, x2, x1, y1);
        }

        return;
    }

    if (Math.Abs(y2 - y1) < Math.Abs(x2 - x1))
    {
        if (x1 < x2)
        {
            DrawShallowLineBresenham(color, x1, x2, y1, y2);
        }
        else
        {
            DrawShallowLineBresenham(color, x2, x1, y2, y1);
        }

        return;
    }

    if (y1 < y2)
    {
        DrawSteepLineBresenham(color, x1, x2, y1, y2);
    }
    else
    {
        DrawSteepLineBresenham(color, x2, x1, y2, y1);
    }
}

void DrawShallowLineBresenham(Color color, int x1, int x2, int y1, int y2)
{
    x1 = Math.Clamp(x1, 0, screenWidth - 1);
    x2 = Math.Clamp(x2, 0, screenWidth - 1);
    y1 = Math.Clamp(y1, 0, screenHeight - 1);
    y2 = Math.Clamp(y2, 0, screenHeight - 1);

    var dx = x2 - x1;
    var dy = y2 - y1;
    var yInc = 1;

    if (dy < 0)
    {
        yInc = -1;
        dy *= -1;
    }

    var d = 2 * dy - dx;
    var dInc = 2 * (dy - dx);
    var dNoInc = 2 * dy;

    var y = y1;

    for (var x = x1; x < x2; x++)
    {
        PopulatePixel(color, x, y);

        if (d > 0)
        {
            y += yInc;
            d += dInc;
        }
        else
        {
            d += dNoInc;
        }
    }
}

void DrawSteepLineBresenham(Color color, int x1, int x2, int y1, int y2)
{
    x1 = Math.Clamp(x1, 0, screenWidth - 1);
    x2 = Math.Clamp(x2, 0, screenWidth - 1);
    y1 = Math.Clamp(y1, 0, screenHeight - 1);
    y2 = Math.Clamp(y2, 0, screenHeight - 1);

    var dx = x2 - x1;
    var dy = y2 - y1;
    var xInc = 1;

    if (dx < 0)
    {
        xInc = -1;
        dx *= -1;
    }

    var d = 2 * dx - dy;
    var dInc = 2 * (dx - dy);
    var dNoInc = 2 * dx;

    var x = x1;

    for (var y = y1; y < y2; y++)
    {
        PopulatePixel(color, x, y);

        if (d > 0)
        {
            x += xInc;
            d += dInc;
        }
        else
        {
            d += dNoInc;
        }
    }
}

void DrawLineNaive(Color color, int x1, int x2, int y1, int y2)
{
    if (x1 == x2)
    {
        if (y1 < y2)
        {
            DrawVerticalLine(color, x1, y1, y2);
        }
        else
        {
            DrawVerticalLine(color, x1, y2, y1);
        }

        return;
    }

    if (y1 == y2)
    {
        if (x1 < x2)
        {
            DrawHorizontalLine(color, x1, x2, y1);
        }
        else
        {
            DrawHorizontalLine(color, x2, x1, y1);
        }

        return;
    }

    if (Math.Abs(y2 - y1) < Math.Abs(x2 - x1))
    {
        if (x1 < x2)
        {
            DrawShallowLineNaive(color, x1, x2, y1, y2);
        }
        else
        {
            DrawShallowLineNaive(color, x2, x1, y2, y1);
        }

        return;
    }

    if (y1 < y2)
    {
        DrawSteepLineNaive(color, x1, x2, y1, y2);
    }
    else
    {
        DrawSteepLineNaive(color, x2, x1, y2, y1);
    }
}

void DrawShallowLineNaive(Color color, int x1, int x2, int y1, int y2)
{
    var dYdX = (float)(y2 - y1) / (x2 - x1);

    x1 = Math.Clamp(x1, 0, screenWidth - 1);
    x2 = Math.Clamp(x2, 0, screenWidth - 1);
    y1 = Math.Clamp(y1, 0, screenHeight - 1);

    var y = (float)y1;

    for (var x = x1; x < x2; x++)
    {
        PopulatePixel(color, x, (int)y);

        y += dYdX;
    }
}

void DrawSteepLineNaive(Color color, int x1, int x2, int y1, int y2)
{
    var dXdY = (float)(x2 - x1) / (y2 - y1);

    x1 = Math.Clamp(x1, 0, screenWidth - 1);
    y1 = Math.Clamp(y1, 0, screenHeight - 1);
    y2 = Math.Clamp(y2, 0, screenHeight - 1);

    var x = (float)x1;

    for (var y = y1; y < y2; y++)
    {
        PopulatePixel(color, (int)x, y);

        x += dXdY;
    }
}

void DrawHorizontalLine(Color color, int x1, int x2, int y)
{
    x1 = Math.Clamp(x1, 0, screenWidth - 1);
    x2 = Math.Clamp(x2, 0, screenWidth - 1);
    y = Math.Clamp(y, 0, screenHeight - 1);

    for (var x = x1; x < x2; x++)
    {
        PopulatePixel(color, x, y);
    }
}

void DrawVerticalLine(Color color, int x, int y1, int y2)
{
    y1 = Math.Clamp(y1, 0, screenWidth - 1);
    y2 = Math.Clamp(y2, 0, screenHeight - 1);
    x = Math.Clamp(x, 0, screenHeight - 1);

    for (var y = y1; x < y2; y++)
    {
        PopulatePixel(color, x, y);
    }
}

static float ToRadians(float angleInDegress)
{
    return (float)(Math.PI / 180.0) * angleInDegress;
}

void PopulatePixel(Color color, int x, int y)
{
    y = screenHeight - y - 1;

    pixels![y, x, 0] = (byte)Math.Clamp(color.R, 0.0, 255.0);
    pixels[y, x, 1] = (byte)Math.Clamp(color.G, 0.0, 255.0);
    pixels[y, x, 2] = (byte)Math.Clamp(color.B, 0.0, 255.0);
}