using Godot;
using System;
using System.Collections.Generic;

public partial class WorldBase : Node3D
{

    double dd = 0f;
    public override void _Process(double delta)
    {

        if (dd <= 200f)
        {
            dd += 1f;
            return;
        }

        dd = 0f;        

        var counts = new Dictionary<string, int>();

        CountNodes(GetTree().Root, counts);

        foreach (var kv in counts)
        {
            GD.Print($"{kv.Key}: {kv.Value}");
        }
    }

    private void CountNodes(Node node, Dictionary<string, int> counts)
    {
        string t = node.GetType().Name;

        if (!counts.ContainsKey(t))
            counts[t] = 0;

        counts[t]++;

        foreach (Node child in node.GetChildren())
        {
            CountNodes(child, counts);
        }
    }
}
