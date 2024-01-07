using System.Numerics;

namespace SimpleSoftwareRenderer.Rasterization;

internal class Scene
{
    public List<ModelInstance> Instances { get; set; }

    public List<Light> Lights { get; set; }
}
