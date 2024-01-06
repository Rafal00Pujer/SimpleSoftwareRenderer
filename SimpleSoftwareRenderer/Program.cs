// See https://aka.ms/new-console-template for more information

using SimpleSoftwareRenderer;
using SimpleSoftwareRenderer.Rasterization;
using System.Diagnostics;
using System.Numerics;

Console.WriteLine("Hello, World!");

using var window = new Window(nameof(SimpleSoftwareRenderer), 600, 400, 600, 600);

var (canvasWidth, canvasHeight) = window.FrameSize;

var pixels = new byte[canvasHeight, canvasWidth, 3];

Scene scene = CreateScene();

var rasterization = new SimpleRasterization(scene, pixels);

var cameraPosition = new Vector3(0.0f, 0.0f, 0.0f);
var cameraPositionMatrix = Matrix4x4.CreateTranslation(cameraPosition);
Matrix4x4.Invert(cameraPositionMatrix, out cameraPositionMatrix);

var cameraRotation = new Vector3(ToRadians(20.0f), ToRadians(-25.0f), ToRadians(0.0f));
var cameraRotationMatrix = Matrix4x4.CreateFromYawPitchRoll(cameraRotation.Y, cameraRotation.X, cameraRotation.Z);
//Matrix4x4.Invert(cameraRotationMatrix, out cameraRotationMatrix);
cameraRotationMatrix = Matrix4x4.Transpose(cameraRotationMatrix);

var cameraTransform = cameraRotationMatrix * cameraPositionMatrix;

var oneSecond = TimeSpan.FromSeconds(1);
var stopwatch = Stopwatch.StartNew();

while (!window.Quit)
{
    window.Run();

    window.Draw(pixels);

    rasterization.RenderScene(cameraTransform, cameraPosition, 90.0f);

    Console.Clear();
    Console.WriteLine($"FPS: {oneSecond / stopwatch.Elapsed}");
    stopwatch.Restart();
}

static Scene CreateScene()
{
    var cube = new Model
    {
        Vertices =
        [
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(-1.0f, 1.0f, 1.0f),
            new Vector3(-1.0f, -1.0f, 1.0f),
            new Vector3(1.0f, -1.0f, 1.0f),
            new Vector3(1.0f, 1.0f, -1.0f),
            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f)
        ],
        Triangles =
        [
            new Triangle(0, 1, 2, new Color { R = 255.0f }),
            new Triangle(0, 2, 3, new Color { R = 255.0f }),
            new Triangle(4, 0, 3, new Color { G = 255.0f }),
            new Triangle(4, 3, 7, new Color { G = 255.0f }),
            new Triangle(5, 4, 7, new Color { B = 255.0f }),
            new Triangle(5, 7, 6, new Color { B = 255.0f }),
            new Triangle(1, 5, 6, new Color { R = 255.0f, G = 255.0f }),
            new Triangle(1, 6, 2, new Color { R = 255.0f, G = 255.0f }),
            new Triangle(4, 5, 1, new Color { R = 255.0f, B = 255.0f }),
            new Triangle(4, 1, 0, new Color { R = 255.0f, B = 255.0f }),
            new Triangle(2, 6, 7, new Color { G = 255.0f, B = 255.0f }),
            new Triangle(2, 7, 3, new Color { G = 255.0f, B = 255.0f })
        ]
    };

    var scene = new Scene()
    {
        Instances =
        [
            new ModelInstance
            {
                Model = cube,
                Position = new Vector3(-1.5f, 0.0f, 7.0f)
            },
            new ModelInstance
            {
                Model = cube,
                Position = new Vector3(1.25f, 2.0f, 7.5f)
            }
        ]
    };

    return scene;
}

static float ToRadians(float angleInDegress)
{
    return (float)(Math.PI / 180.0) * angleInDegress;
}