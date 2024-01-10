// See https://aka.ms/new-console-template for more information
using SimpleSoftwareRenderer;
using System.Diagnostics;
using System.Numerics;

Console.WriteLine("Hello, World!");

using var window = new Window(nameof(SimpleSoftwareRenderer), 600, 400, 640, 480);

var (screenWidth, screenHeight) = window.FrameSize;

byte[,,] pixels;

var theta = 0.0f;
var deltaTime = 0.0f;

var oneSecond = TimeSpan.FromSeconds(1);
var stopwatch = Stopwatch.StartNew();

while (!window.Quit)
{
    window.Run();

    pixels = new byte[screenHeight, screenWidth, 3];

    //LinesTest();
    //ProjectionTest();
    //BackFaceTest();
    //ClipingTest();
    //FlatShadingTest();
    ColorBlendTest();

    window.Draw(pixels);

    Console.Clear();
    Console.WriteLine($"FPS: {oneSecond / stopwatch.Elapsed}");
    deltaTime = stopwatch.ElapsedMilliseconds;
    stopwatch.Restart();
}

void ColorBlendTest()
{
    const int pointCount = 8;

    var vertices = new Vector4[pointCount]
    {
        new( 0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, -0.75f, 0.75f, 1.0f),
        new( 0.75f, -0.75f, 0.75f, 1.0f),

        new( -0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, -0.75f, -0.75f, 1.0f),
        new( -0.75f, -0.75f, -0.75f, 1.0f)
    };

    var attributes = new Payload[pointCount]
    {
        new() {Data = [0.5f,0.5f,0.5f,0.0f,0.0f,0.0f,0.0f,0.0f]},
        new() {Data = [1.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f]},
        new() {Data = [0.0f,1.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f]},
        new() {Data = [1.0f,1.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f]},

        new() {Data = [0.0f,0.0f,1.0f,0.0f,0.0f,0.0f,0.0f,0.0f]},
        new() {Data = [1.0f,0.0f,1.0f,0.0f,0.0f,0.0f,0.0f,0.0f]},
        new() {Data = [0.0f,1.0f,1.0f,0.0f,0.0f,0.0f,0.0f,0.0f]},
        new() {Data = [1.0f,1.0f,1.0f,0.0f,0.0f,0.0f,0.0f,0.0f]}
    };

    const int planeCount = 6;
    const int planeVerticesCount = 4;

    var planeVertices = new int[planeCount, planeVerticesCount]
    {
        { 0, 1, 2, 3 }, // front
        { 1, 0, 5, 4 }, // top
        { 3, 6, 5, 0 }, // right
        { 7, 6, 3, 2 }, // bottom
        { 1, 4, 7, 2 }, // left
        { 4, 5, 6, 7 } // back
    };

    theta += 0.1f * deltaTime / 16.6f;
    if (theta > 360)
    {
        theta -= 360;
    }

    var transformedVertices = new Vector4[pointCount];

    var rotation = Matrix4x4.CreateFromYawPitchRoll(ToRadians(-theta * 3.0f), ToRadians(theta * 2.0f), ToRadians(-theta));
    var translation = Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, -5.0f));

    const float fovY = 45.0f;
    var aspect = (float)screenWidth / screenHeight;
    const float near = 0.1f;
    const float far = 10.0f;

    var projection = Matrix4x4.CreatePerspectiveFieldOfView(ToRadians(fovY), aspect, near, far);
    var viewFrustum = MakeViewFrustum(fovY, aspect, -near, -far);

    var finalTransform = rotation * translation;

    for (var i = 0; i < pointCount; i++)
    {
        transformedVertices[i] = Vector4.Transform(vertices[i], finalTransform);
    }

    for (var i = 0; i < planeCount; i++)
    {
        var vertexA = transformedVertices[planeVertices[i, 0]];
        var vertexB = transformedVertices[planeVertices[i, 1]];
        var vertexC = transformedVertices[planeVertices[i, 2]];

        var tangent = vertexB - vertexA;
        var biTangent = vertexC - vertexA;

        var normal = Vector3.Normalize(
            Vector3.Cross(new Vector3(tangent.X, tangent.Y, tangent.Z),
            new Vector3(biTangent.X, biTangent.Y, biTangent.Z)));

        var fragmentToViewer = new Vector3(-vertexA.X, -vertexA.Y, -vertexA.Z);

        if (Vector3.Dot(normal, fragmentToViewer) < 0)
        {
            continue;
        }

        var edges = new EdgeTable();

        for (var j = 0; j < planeVerticesCount; j++)
        {
            edges.Vertices.Add(transformedVertices[planeVertices[i, j]]);

            var payload = new Payload();
            Array.Copy(attributes[planeVertices[i, j]].Data, payload.Data, Payload.PayloadCount);

            var torch = new Vector3(0.0f, 0.0f, 1.0f);
            var diffuseColor = new Vector3(payload.Data[0], payload.Data[1], payload.Data[2]);
            diffuseColor *= Vector3.Dot(normal, torch);

            payload.Data[0] = diffuseColor.X;
            payload.Data[1] = diffuseColor.Y;
            payload.Data[2] = diffuseColor.Z;

            edges.Attributes.Add(payload);
        }

        edges = FrustumClip(edges, viewFrustum);

        for (var j = 0; j < edges.Vertices.Count; j++)
        {
            var point = Vector4.Transform(edges.Vertices[j], projection);

            point.X /= point.W;
            point.Y /= point.W;

            point.X = (screenWidth / 2) + (screenWidth / 2) * point.X;
            point.Y = (screenHeight / 2) + (screenHeight / 2) * point.Y;

            edges.Vertices[j] = point;
        }

        DrawPolygonBlended(edges);
    }
}

void FlatShadingTest()
{
    const int pointCount = 8;

    var vertices = new Vector4[pointCount]
    {
        new( 0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, -0.75f, 0.75f, 1.0f),
        new( 0.75f, -0.75f, 0.75f, 1.0f),

        new( -0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, -0.75f, -0.75f, 1.0f),
        new( -0.75f, -0.75f, -0.75f, 1.0f)
    };

    const int planeCount = 6;

    var planeVertices = new int[planeCount, 4]
    {
        { 0, 1, 2, 3 }, // front
        { 1, 0, 5, 4 }, // top
        { 3, 6, 5, 0 }, // right
        { 7, 6, 3, 2 }, // bottom
        { 1, 4, 7, 2 }, // left
        { 4, 5, 6, 7 } // back
    };

    theta += 0.1f * deltaTime / 16.6f;
    if (theta > 360)
    {
        theta -= 360;
    }

    var transformedVertices = new Vector4[pointCount];

    var rotation = Matrix4x4.CreateFromYawPitchRoll(ToRadians(-theta * 3.0f), ToRadians(theta * 2.0f), ToRadians(-theta));
    var translation = Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, -5.0f));

    const float fovY = 45.0f;
    var aspect = (float)screenWidth / screenHeight;
    const float near = 0.1f;
    const float far = 10.0f;

    var projection = Matrix4x4.CreatePerspectiveFieldOfView(ToRadians(fovY), aspect, near, far);
    var viewFrustum = MakeViewFrustum(fovY, aspect, -near, -far);

    var finalTransform = rotation * translation;

    for (var i = 0; i < pointCount; i++)
    {
        transformedVertices[i] = Vector4.Transform(vertices[i], finalTransform);
    }

    for (var i = 0; i < planeCount; i++)
    {
        var vertexA = transformedVertices[planeVertices[i, 0]];
        var vertexB = transformedVertices[planeVertices[i, 1]];
        var vertexC = transformedVertices[planeVertices[i, 2]];

        var tangent = vertexB - vertexA;
        var biTangent = vertexC - vertexA;

        var normal = Vector3.Normalize(
            Vector3.Cross(new Vector3(tangent.X, tangent.Y, tangent.Z),
            new Vector3(biTangent.X, biTangent.Y, biTangent.Z)));

        var fragmentToViewer = new Vector3(-vertexA.X, -vertexA.Y, -vertexA.Z);

        if (Vector3.Dot(normal, fragmentToViewer) < 0)
        {
            continue;
        }

        var edges = new EdgeTable();

        for (var j = 0; j < 4; j++)
        {
            edges.Vertices.Add(transformedVertices[planeVertices[i, j]]);
        }

        edges = FrustumClipSimple(edges, viewFrustum);

        for (var j = 0; j < edges.Vertices.Count; j++)
        {
            var point = Vector4.Transform(edges.Vertices[j], projection);

            point.X /= point.W;
            point.Y /= point.W;

            point.X = MathF.Floor((screenWidth / 2) + (screenWidth / 2) * point.X);
            point.Y = MathF.Floor((screenHeight / 2) + (screenHeight / 2) * point.Y);

            edges.Vertices[j] = point;
        }

        var torch = new Vector3(0.0f, 0.0f, 1.0f);
        var diffuseColor = new Color { R = 255.0f, G = 255.0f, B = 255.0f };
        diffuseColor *= Vector3.Dot(normal, torch);

        DrawPolygonFlat(diffuseColor, edges);
    }
}

void ClipingTest()
{
    const int pointCount = 8;

    var vertices = new Vector4[pointCount]
    {
        new( 0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, -0.75f, 0.75f, 1.0f),
        new( 0.75f, -0.75f, 0.75f, 1.0f),

        new( -0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, -0.75f, -0.75f, 1.0f),
        new( -0.75f, -0.75f, -0.75f, 1.0f)
    };

    const int planeCount = 6;

    var planeVertices = new int[planeCount, 4]
    {
        { 0, 1, 2, 3 }, // front
        { 1, 0, 5, 4 }, // top
        { 3, 6, 5, 0 }, // right
        { 7, 6, 3, 2 }, // bottom
        { 1, 4, 7, 2 }, // left
        { 4, 5, 6, 7 } // back
    };

    theta += 0.1f * deltaTime / 16.6f;
    if (theta > 360)
    {
        theta -= 360;
    }

    var transformedVertices = new Vector4[pointCount];

    var rotation = Matrix4x4.CreateFromYawPitchRoll(ToRadians(-theta * 3.0f), ToRadians(theta * 2.0f), ToRadians(-theta));
    var translation = Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, -5.0f));

    const float fovY = 45.0f;
    var aspect = (float)screenWidth / screenHeight;
    const float near = 0.1f;
    const float far = 10.0f;

    var projection = Matrix4x4.CreatePerspectiveFieldOfView(ToRadians(fovY), aspect, near, far);
    var viewFrustum = MakeViewFrustum(fovY, aspect, -near, -far);

    var finalTransform = rotation * translation;

    for (var i = 0; i < pointCount; i++)
    {
        transformedVertices[i] = Vector4.Transform(vertices[i], finalTransform);
    }

    for (var i = 0; i < planeCount; i++)
    {
        var vertexA = transformedVertices[planeVertices[i, 0]];
        var vertexB = transformedVertices[planeVertices[i, 1]];
        var vertexC = transformedVertices[planeVertices[i, 2]];

        var tangent = vertexB - vertexA;
        var biTangent = vertexC - vertexA;

        var normal = Vector3.Cross(new Vector3(tangent.X, tangent.Y, tangent.Z), new Vector3(biTangent.X, biTangent.Y, biTangent.Z));
        var fragmentToViewer = new Vector3(-vertexA.X, -vertexA.Y, -vertexA.Z);

        if (Vector3.Dot(normal, fragmentToViewer) < 0)
        {
            continue;
        }

        var edges = new EdgeTable();

        for (var j = 0; j < 4; j++)
        {
            edges.Vertices.Add(transformedVertices[planeVertices[i, j]]);
        }

        edges = FrustumClipSimple(edges, viewFrustum);

        for (var j = 0; j < edges.Vertices.Count; j++)
        {
            var pointA = Vector4.Transform(edges.Vertices[j], projection);
            pointA.X /= pointA.W;
            pointA.Y /= pointA.W;

            pointA.X = (screenWidth / 2) + (screenWidth / 2) * pointA.X;
            pointA.Y = (screenHeight / 2) + (screenHeight / 2) * pointA.Y;

            var pointB = Vector4.Transform(edges.Vertices[(j + 1) % edges.Vertices.Count], projection);
            pointB.X /= pointB.W;
            pointB.Y /= pointB.W;

            pointB.X = (screenWidth / 2) + (screenWidth / 2) * pointB.X;
            pointB.Y = (screenHeight / 2) + (screenHeight / 2) * pointB.Y;

            DrawLineBresenham(new Color { R = 255.0f, G = 255.0f, B = 255.0f },
            (int)pointA.X, (int)pointB.X,
            (int)pointA.Y, (int)pointB.Y);
        }
    }
}

void BackFaceTest()
{
    const int pointCount = 8;

    var vertices = new Vector4[pointCount]
    {
        new( 0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, 0.75f, 0.75f, 1.0f),
        new( -0.75f, -0.75f, 0.75f, 1.0f),
        new( 0.75f, -0.75f, 0.75f, 1.0f),

        new( -0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, 0.75f, -0.75f, 1.0f),
        new( 0.75f, -0.75f, -0.75f, 1.0f),
        new( -0.75f, -0.75f, -0.75f, 1.0f)
    };

    const int planeCount = 6;

    var planeVertices = new int[planeCount, 4]
    {
        { 0, 1, 2, 3 }, // front
        { 1, 0, 5, 4 }, // top
        { 3, 6, 5, 0 }, // right
        { 7, 6, 3, 2 }, // bottom
        { 1, 4, 7, 2 }, // left
        { 4, 5, 6, 7 } // back
    };

    theta += 0.1f * deltaTime / 16.6f;
    if (theta > 360)
    {
        theta -= 360;
    }

    var transformedVertices = new Vector4[pointCount];

    var rotation = Matrix4x4.CreateFromYawPitchRoll(ToRadians(-theta * 3.0f), ToRadians(theta * 2.0f), ToRadians(-theta));
    var translation = Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, -5.0f));
    var projection = Matrix4x4.CreatePerspectiveFieldOfView(ToRadians(45.0f), (float)screenWidth / screenHeight, 0.1f, 10.0f);

    var finalTransform = rotation * translation * projection;

    for (var i = 0; i < pointCount; i++)
    {
        transformedVertices[i] = Vector4.Transform(vertices[i], finalTransform);

        transformedVertices[i].X /= transformedVertices[i].W;
        transformedVertices[i].Y /= transformedVertices[i].W;
        transformedVertices[i].Z /= transformedVertices[i].W;

        transformedVertices[i].X = (screenWidth / 2) + (screenWidth / 2) * transformedVertices[i].X;
        transformedVertices[i].Y = (screenHeight / 2) - (screenHeight / 2) * transformedVertices[i].Y;
    }

    for (var i = 0; i < planeCount; i++)
    {
        var vertexA = transformedVertices[planeVertices[i, 0]];
        var vertexB = transformedVertices[planeVertices[i, 1]];
        var vertexC = transformedVertices[planeVertices[i, 2]];

        var tangent = vertexB - vertexA;
        var biTangent = vertexC - vertexA;

        var normal = Vector3.Cross(new Vector3(tangent.X, tangent.Y, tangent.Z), new Vector3(biTangent.X, biTangent.Y, biTangent.Z));

        if (normal.Z > 0)
        {
            continue;
        }

        for (var j = 0; j < 4; j++)
        {
            var xa = (int)transformedVertices[planeVertices[i, j]].X;
            var ya = (int)transformedVertices[planeVertices[i, j]].Y;

            var xb = (int)transformedVertices[planeVertices[i, (j + 1) % 4]].X;
            var yb = (int)transformedVertices[planeVertices[i, (j + 1) % 4]].Y;

            DrawLineBresenham(new Color { R = 255.0f, G = 255.0f, B = 255.0f },
            xa, xb,
            ya, yb);
        }
    }
}

void ProjectionTest()
{
    const int pointCount = 8;

    var vertices = new Vector4[pointCount]
    {
        new( 0.75f, 0.75f, -2.0f, 1.0f),
        new( -0.75f, 0.75f, -2.0f, 1.0f),
        new( -0.75f, -0.75f, -2.0f, 1.0f),
        new( 0.75f, -0.75f, -2.0f, 1.0f),

        new( -0.75f, 0.75f, -3.5f, 1.0f),
        new( 0.75f, 0.75f, -3.5f, 1.0f),
        new( 0.75f, -0.75f, -3.5f, 1.0f),
        new( -0.75f, -0.75f, -3.5f, 1.0f)
    };

    const int edgeCount = 12;

    var edgeA = new int[edgeCount]
    {
        0, 1, 2, 3,
        4, 5, 6, 7,
        1, 5, 3, 7
    };

    var edgeB = new int[edgeCount]
    {
        1, 2, 3, 0,
        5, 6, 7, 4,
        4, 0, 6, 2
    };

    var transformedVertices = new Vector4[pointCount];

    var matrix = MakePerspectiveProjection(45.0f, (float)screenWidth / screenHeight, 0.1f, 10.0f);

    for (var i = 0; i < pointCount; i++)
    {
        transformedVertices[i] = Vector4.Transform(vertices[i], matrix);

        transformedVertices[i].X /= transformedVertices[i].W;
        transformedVertices[i].Y /= transformedVertices[i].W;
        transformedVertices[i].Z /= transformedVertices[i].W;

        transformedVertices[i].X = (screenWidth / 2) + (screenWidth / 2) * transformedVertices[i].X;
        transformedVertices[i].Y = (screenHeight / 2) - (screenHeight / 2) * transformedVertices[i].Y;

        //Console.WriteLine(transformedVertices[i]);
    }

    for (var i = 0; i < edgeCount; i++)
    {
        DrawLineBresenham(new Color { R = 255.0f, G = 255.0f, B = 255.0f },
            (int)transformedVertices[edgeA[i]].X, (int)transformedVertices[edgeB[i]].X,
            (int)transformedVertices[edgeA[i]].Y, (int)transformedVertices[edgeB[i]].Y);
    }

    Matrix4x4 MakePerspectiveProjection(float fovy, float aspect, float near, float far)
    {
        var yMax = near * (float)Math.Tan(ToRadians(fovy / 2.0f));
        var xMax = yMax * aspect;

        var c = -(far + near) / (far - near);
        var d = -2.0f * far * near / (far - near);
        var e = near / xMax;
        var f = near / yMax;

        var matrix = new Matrix4x4
        {
            M11 = e,
            M22 = f,
            M33 = c,
            M34 = -1.0f,
            M43 = d
        };

        return matrix;
    }
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

void DrawPolygonBlended(EdgeTable polygon)
{
    var xStart = new Vertex[screenWidth];
    var xEnd = new Vertex[screenWidth];

    for (var i = 0; i < xStart.Length; i++)
    {
        xStart[i] = new Vertex();
        xEnd[i] = new Vertex();
    }

    var yMin = 480;
    var yMax = 0;

    foreach (var vertex in polygon.Vertices)
    {
        if (vertex.Y < yMin)
        {
            yMin = (int)vertex.Y;
        }

        if (vertex.Y > yMax)
        {
            yMax = (int)vertex.Y;
        }
    }

    for (var y = yMin; y <= yMax; y++)
    {
        xStart[y].X = screenWidth;
        xEnd[y].X = 0;
    }

    for (var i = 0; i < polygon.Vertices.Count; i++)
    {
        var v1 = new Vertex
        {
            X = (int)polygon.Vertices[i].X,
            Y = (int)polygon.Vertices[i].Y
        };

        Array.Copy(polygon.Attributes[i].Data, v1.Attributes.Data, Payload.PayloadCount);

        var v2 = new Vertex
        {
            X = (int)polygon.Vertices[(i + 1) % polygon.Vertices.Count].X,
            Y = (int)polygon.Vertices[(i + 1) % polygon.Vertices.Count].Y
        };

        Array.Copy(polygon.Attributes[(i + 1) % polygon.Vertices.Count].Data, v2.Attributes.Data, Payload.PayloadCount);

        if (Math.Abs(v2.X - v1.X) < Math.Abs(v2.Y - v1.Y))
        {
            if (v1.Y < v2.Y)
            {
                BlendSteepEdge(v1, v2, xStart, xEnd);
            }
            else
            {
                BlendSteepEdge(v2, v1, xStart, xEnd);
            }
        }
        else
        {
            if (v1.X < v2.X)
            {
                BlendShallowEdge(v1, v2, xStart, xEnd);
            }
            else
            {
                BlendShallowEdge(v2, v1, xStart, xEnd);
            }
        }
    }

    for (var y = yMin; y <= yMax; y++)
    {
        var v1 = new Vertex
        {
            X = xStart[y].X,
            Y = xStart[y].Y
        };

        Array.Copy(xStart[y].Attributes.Data, v1.Attributes.Data, Payload.PayloadCount);

        var v2 = new Vertex
        {
            X = xEnd[y].X,
            Y = xEnd[y].Y
        };

        Array.Copy(xEnd[y].Attributes.Data, v2.Attributes.Data, Payload.PayloadCount);

        DrawBlendedHorizontalLine(v1, v2, y);
    }
}

void BlendShallowEdge(Vertex v1, Vertex v2, Vertex[] xStart, Vertex[] xEnd)
{
    var dx = v2.X - v1.X;
    var dy = v2.Y - v1.Y;
    var yInc = 1;

    if (dy < 0)
    {
        yInc = -1;
        dy *= -1;
    }

    var d = 2 * dy - dx;
    var dInc = 2 * (dy - dx);
    var dNoInc = 2 * dy;

    var dPdx = new Payload { Data = new float[Payload.PayloadCount] };

    for (var i = 0; i < Payload.PayloadCount; i++)
    {
        dPdx.Data[i] = (v2.Attributes.Data[i] - v1.Attributes.Data[i]) / dx;
    }

    var frag = new Vertex
    {
        X = v1.X,
        Y = v1.Y,
    };
    Array.Copy(v1.Attributes.Data, frag.Attributes.Data, Payload.PayloadCount);

    var y = v1.Y;

    for (var x = v1.X; x <= v2.X; x++)
    {
        if (x < xStart[y].X)
        {
            xStart[y].X = x;
            //xStart[y].Attributes = frag.Attributes;

            Array.Copy(frag.Attributes.Data, xStart[y].Attributes.Data, Payload.PayloadCount);
        }

        if (x > xEnd[y].X)
        {
            xEnd[y].X = x;
            //xEnd[y].Attributes = frag.Attributes;

            Array.Copy(frag.Attributes.Data, xEnd[y].Attributes.Data, Payload.PayloadCount);
        }

        if (d > 0)
        {
            y += yInc;
            d += dInc;
        }
        else
        {
            d += dNoInc;
        }

        for (var i = 0; i < Payload.PayloadCount; i++)
        {
            frag.Attributes.Data[i] += dPdx.Data[i];
        }
    }
}

void BlendSteepEdge(Vertex v1, Vertex v2, Vertex[] xStart, Vertex[] xEnd)
{
    var dx = v2.X - v1.X;
    var dy = v2.Y - v1.Y;
    var xInc = 1;

    if (dx < 0)
    {
        xInc = -1;
        dx *= -1;
    }

    var d = 2 * dx - dy;
    var dInc = 2 * (dx - dy);
    var dNoInc = 2 * dx;

    var dPdy = new Payload { Data = new float[Payload.PayloadCount] };

    for (var i = 0; i < Payload.PayloadCount; i++)
    {
        dPdy.Data[i] = (v2.Attributes.Data[i] - v1.Attributes.Data[i]) / dy;
    }

    var frag = new Vertex
    {
        X = v1.X,
        Y = v1.Y,
    };
    Array.Copy(v1.Attributes.Data, frag.Attributes.Data, Payload.PayloadCount);

    var x = v1.X;

    for (var y = v1.Y; y <= v2.Y; y++)
    {
        if (x < xStart[y].X)
        {
            xStart[y].X = x;
            //xStart[y].Attributes = frag.Attributes;

            Array.Copy(frag.Attributes.Data, xStart[y].Attributes.Data, Payload.PayloadCount);
        }

        if (x > xEnd[y].X)
        {
            xEnd[y].X = x;
            //xEnd[y].Attributes = frag.Attributes;

            Array.Copy(frag.Attributes.Data, xEnd[y].Attributes.Data, Payload.PayloadCount);
        }

        if (d > 0)
        {
            x += xInc;
            d += dInc;
        }
        else
        {
            d += dNoInc;
        }

        for (var i = 0; i < Payload.PayloadCount; i++)
        {
            frag.Attributes.Data[i] += dPdy.Data[i];
        }
    }
}

void DrawBlendedHorizontalLine(Vertex v1, Vertex v2, int y)
{
    var x1 = Math.Clamp(v1.X, 0, screenWidth - 1);
    var x2 = Math.Clamp(v2.X, 0, screenWidth - 1);
    y = Math.Clamp(y, 0, screenHeight - 1);

    //var frag = v1.Attributes;
    var frag = new Payload();
    Array.Copy(v1.Attributes.Data, frag.Data, Payload.PayloadCount);

    var dx = x2 - x1;

    var dPdx = new Payload { Data = new float[Payload.PayloadCount] };

    for (var i = 0; i < Payload.PayloadCount; i++)
    {
        dPdx.Data[i] = (v2.Attributes.Data[i] - v1.Attributes.Data[i]) / dx;
    }

    for (var x = x1; x < x2; x++)
    {
        //var color = new Color { R = frag.Data[0], G = frag.Data[1], B = frag.Data[2] };
        var color = new Color { R = ScaleColor(frag.Data[0]), G = ScaleColor(frag.Data[1]), B = ScaleColor(frag.Data[2]) };

        PopulatePixel(color, x, y);

        for (var i = 0; i < Payload.PayloadCount; i++)
        {
            frag.Data[i] += dPdx.Data[i];
        }
    }

    float ScaleColor(float colorToScale)
    {
        return 255.0f * colorToScale;
    }
}

EdgeTable FrustumClip(EdgeTable input, Frustum f)
{
    for (var i = 0; i < Frustum.PlaneCount; i++)
    {
        input = ClipAgainstBoundryWithAttributes(input, f[i]);
    }

    return input;
}

EdgeTable ClipAgainstBoundryWithAttributes(EdgeTable input, Plane p)
{
    var output = new EdgeTable();
    var vertexCount = input.Vertices.Count;

    for (var i = 0; i < vertexCount; i++)
    {
        var a = input.Vertices[i];
        var b = input.Vertices[(i + 1) % vertexCount];

        var t = PlaneIntersectionPoint(a, b, p);
        var c = Vector4.Lerp(a, b, t);

        var payloadA = input.Attributes[i];
        var payloadB = input.Attributes[(i + 1) % vertexCount];
        var payloadC = new Payload { Data = new float[Payload.PayloadCount] };

        for (var j = 0; j < Payload.PayloadCount; j++)
        {
            payloadC.Data[j] = (payloadA.Data[j] * (1.0f - t)) + (payloadB.Data[j] * t);
        }

        // b inside of boundary
        if (!PointBehindPlane(b, p))
        {
            // a outside of boundary
            if (PointBehindPlane(a, p))
            {
                output.Vertices.Add(c);
                output.Attributes.Add(payloadC);
            }

            output.Vertices.Add(b);
            output.Attributes.Add(payloadB);
        }
        // a is visible
        else if (!PointBehindPlane(a, p))
        {
            output.Vertices.Add(c);
            output.Attributes.Add(payloadC);
        }
    }

    return output;
}

void DrawPolygonFlat(Color color, EdgeTable edgeTable)
{
    var xStart = new int[screenWidth];
    var xEnd = new int[screenWidth];

    var yMin = int.MaxValue;
    var yMax = 0;

    foreach (var vertex in edgeTable.Vertices)
    {
        if (vertex.Y < yMin)
        {
            yMin = (int)vertex.Y;
        }

        if (vertex.Y > yMax)
        {
            yMax = (int)vertex.Y;
        }
    }

    for (var y = yMin; y <= yMax; y++)
    {
        xStart[y] = int.MaxValue;
        xEnd[y] = 0;
    }

    for (var i = 0; i < edgeTable.Vertices.Count; i++)
    {
        var x1 = edgeTable.Vertices[i].X;
        var y1 = edgeTable.Vertices[i].Y;

        var x2 = edgeTable.Vertices[(i + 1) % edgeTable.Vertices.Count].X;
        var y2 = edgeTable.Vertices[(i + 1) % edgeTable.Vertices.Count].Y;

        if (Math.Abs(x2 - x1) < Math.Abs(y2 - y1))
        {
            if (y1 < y2)
            {
                TraceSteepEdge((int)x1, (int)x2, (int)y1, (int)y2, xStart, xEnd);
            }
            else
            {
                TraceSteepEdge((int)x2, (int)x1, (int)y2, (int)y1, xStart, xEnd);
            }

            continue;
        }

        if (x1 < x2)
        {
            TraceShallowEdge((int)x1, (int)x2, (int)y1, (int)y2, xStart, xEnd);
        }
        else
        {
            TraceShallowEdge((int)x2, (int)x1, (int)y2, (int)y1, xStart, xEnd);
        }
    }

    for (var y = yMin; y <= yMax; y++)
    {
        DrawHorizontalLine(color, xStart[y], xEnd[y], y);
    }
}

void TraceShallowEdge(int x1, int x2, int y1, int y2, int[] xStart, int[] xEnd)
{
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

    for (var x = x1; x <= x2; x++)
    {
        if (x < xStart[y])
        {
            xStart[y] = x;
        }

        if (x > xEnd[y])
        {
            xEnd[y] = x;
        }

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

void TraceSteepEdge(int x1, int x2, int y1, int y2, int[] xStart, int[] xEnd)
{
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

    for (var y = y1; y <= y2; y++)
    {
        if (x < xStart[y])
        {
            xStart[y] = x;
        }

        if (x > xEnd[y])
        {
            xEnd[y] = x;
        }

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

EdgeTable ClipAgainstBoundry(EdgeTable input, Plane p)
{
    var output = new EdgeTable();
    var vertexCount = input.Vertices.Count;

    for (var i = 0; i < vertexCount; i++)
    {
        var a = input.Vertices[i];
        var b = input.Vertices[(i + 1) % vertexCount];

        var t = PlaneIntersectionPoint(a, b, p);
        var c = Vector4.Lerp(a, b, t);

        // b inside of boundary
        if (!PointBehindPlane(b, p))
        {
            // a outside of boundary
            if (PointBehindPlane(a, p))
            {
                output.Vertices.Add(c);
            }

            output.Vertices.Add(b);
        }
        // a is visible
        else if (!PointBehindPlane(a, p))
        {
            output.Vertices.Add(c);
        }
    }

    return output;
}

EdgeTable FrustumClipSimple(EdgeTable input, Frustum f)
{
    for (var i = 0; i < Frustum.PlaneCount; i++)
    {
        input = ClipAgainstBoundry(input, f[i]);
    }

    return input;
}

Frustum MakeViewFrustum(float fovY, float aspect, float near, float far)
{
    var result = new Frustum();

    float yTop = MathF.Abs(near) * MathF.Tan(ToRadians(fovY / 2.0f));
    float xRight = yTop * aspect;

    // near
    var p = new Vector3(0.0f, 0.0f, near);
    var normal = new Vector3(0.0f, 0.0f, -1.0f);

    result[0] = MakePlane(p, normal);

    // far
    p = new Vector3(0.0f, 0.0f, far);
    normal = new Vector3(0.0f, 0.0f, 1.0f);

    result[1] = MakePlane(p, normal);

    // top
    p = new Vector3(0.0f, yTop, near);
    normal = new Vector3(0.0f, near / yTop, -1.0f);

    result[2] = MakePlane(p, normal);

    // bottom
    p = new Vector3(0.0f, -yTop, near);
    normal = new Vector3(0.0f, -near / yTop, -1.0f);

    result[3] = MakePlane(p, normal);

    // left
    p = new Vector3(-xRight, 0.0f, near);
    normal = new Vector3(-near / xRight, 0.0f, -1.0f);

    result[4] = MakePlane(p, normal);

    // right
    p = new Vector3(xRight, 0.0f, near);
    normal = new Vector3(near / xRight, 0.0f, -1.0f);

    result[5] = MakePlane(p, normal);

    return result;
}

Plane MakePlane(Vector3 p, Vector3 normal)
{
    normal = Vector3.Normalize(normal);
    var d = Vector3.Dot(p, normal) * -1.0f;

    return new Plane(normal, d);
}

float PlaneIntersectionPoint(Vector4 a, Vector4 b, Plane p)
{
    var aToB = b - a;
    var dividend = Plane.DotCoordinate(p, new Vector3(a.X, a.Y, a.Z)) * -1.0f;
    var divisor = Vector3.Dot(p.Normal, new Vector3(aToB.X, aToB.Y, aToB.Z));

    return dividend / divisor;
}

bool PointBehindPlane(Vector4 v, Plane p)
{
    return Plane.DotCoordinate(p, new Vector3(v.X, v.Y, v.Z)) < 0;
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
    y1 = Math.Clamp(y1, 0, screenHeight - 1);
    y2 = Math.Clamp(y2, 0, screenHeight - 1);
    x = Math.Clamp(x, 0, screenWidth - 1);

    for (var y = y1; y < y2; y++)
    {
        PopulatePixel(color, x, y);
    }
}

static float ToRadians(float angleInDegress)
{
    return MathF.PI / 180.0f * angleInDegress;
}

void PopulatePixel(Color color, int x, int y)
{
    //y = screenHeight - y;

    pixels![y, x, 0] = (byte)Math.Clamp(color.R, 0.0f, 255.0f);
    pixels[y, x, 1] = (byte)Math.Clamp(color.G, 0.0f, 255.0f);
    pixels[y, x, 2] = (byte)Math.Clamp(color.B, 0.0f, 255.0f);
}

struct EdgeTable()
{
    public List<Vector4> Vertices { get; set; } = new();
    public List<Payload> Attributes { get; set; } = new();
}

readonly struct Frustum()
{
    public const int PlaneCount = 6;

    private readonly Plane[] _planes = new Plane[PlaneCount];

    public Plane this[int index]
    {
        get => _planes[index];
        set => _planes[index] = value;
    }
}

struct Payload
{
    public const int PayloadCount = 8;

    public float[] Data { get; set; }

    public Payload()
    {
        Data = new float[PayloadCount];
    }
}

struct Vertex
{
    public Payload Attributes { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public Vertex()
    {
        Attributes = new Payload();
    }
}