using System;
using System.Reflection.Metadata.Ecma335;
using Godot.Collections;

[GlobalClass]
public partial class MeshIdAssignment : Resource
{
    [Export]
    public Array<int> floor;

    [Export]
    public Array<int> wall;

    [Export]
    public Array<int> wallCornerInwards;

    [Export]
    public Array<int> wallCornerInwardsDouble;

    [Export]
    public Array<int> wallBridge;

    public int Pick(Array<int> a) => a[Random.Shared.Next(a.Count() - 1)];
}
