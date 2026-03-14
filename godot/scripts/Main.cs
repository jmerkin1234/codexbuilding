using CodexBuilding.Billiards.Core.Geometry;
using Godot;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main : Node3D
{
    public override void _Ready()
    {
        var table = CustomTable9FtSpec.Create();
        GD.Print($"Loaded {table.Name} with {table.Cushions.Count} cushions and {table.Pockets.Count} pockets.");
    }
}
