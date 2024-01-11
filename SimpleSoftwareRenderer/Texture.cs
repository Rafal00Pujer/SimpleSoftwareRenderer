using System.Drawing;

namespace SimpleSoftwareRenderer;

internal class Texture
{
    private readonly SimpleColor[,] _colors;

    public int Width => _colors.GetLength(0);

    public int Height => _colors.GetLength(1);

    public SimpleColor this[int x, int y] => _colors[x, y];

    private Texture(int width, int height)
    {
        _colors = new SimpleColor[width, height];
    }

    public static async Task<Texture> LoadTexture(string filePath)
    {
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);
        await fileStream.DisposeAsync();

        var bitmap = new Bitmap(memoryStream);
        var texture = new Texture(bitmap.Width, bitmap.Height);

        for (int y = 0; y < texture.Height; y++)
        {
            for (int x = 0; x < texture.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);

                var simpleColor = new SimpleColor
                {
                    R = color.R,
                    G = color.G,
                    B = color.B,
                };

                texture._colors[x, y] = simpleColor;
            }
        }

        await memoryStream.DisposeAsync();

        return texture;
    }
}
