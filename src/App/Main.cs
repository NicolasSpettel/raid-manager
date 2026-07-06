using Engine;
using Godot;

namespace App;

/// <summary>
/// M0 App shell. Proves "one engine, two consumers": it runs the SAME Engine.SimulateEncounter that
/// the Sim CLI runs and prints the byte-identical event stream + hash. Windowed, it also shows the
/// log in a RichTextLabel; headless, the GD.Print output is what godot-mcp verifies against the CLI.
/// </summary>
public partial class Main : Control
{
    public override void _Ready()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Dummy(1));
        string log = EventStream.Serialize(result.Events).TrimEnd('\n');
        string hash = result.Hash();

        GD.Print(log);
        GD.Print($"outcome={result.Outcome} seed={result.Seed} engine=v{result.EngineVersion} schema=v{result.EventSchemaVersion}");
        GD.Print($"hash={hash}");

        var label = new RichTextLabel();
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        label.Text = $"{log}\n\nhash={hash}";
        AddChild(label);
    }
}
