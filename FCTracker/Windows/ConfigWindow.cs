namespace FCTracker.Windows;

using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

public class ConfigWindow : Window, IDisposable
{
    public ConfigWindow() : base("A Wonderful Configuration Window###With a constant ID")
    {
        this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                     ImGuiWindowFlags.NoScrollWithMouse;

        this.Size          = new Vector2(232, 90);
        this.SizeCondition = ImGuiCond.Always;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        /*
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (Configuration.Instance.IsConfigWindowMovable)
        {
            this.Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            this.Flags |= ImGuiWindowFlags.NoMove;
        }*/
    }

    public override void Draw()
    {
        
    }
}
