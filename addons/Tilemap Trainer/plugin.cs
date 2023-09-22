#if TOOLS
using Godot;


[Tool]
public partial class plugin : EditorPlugin{
    Control dock;
    #region GODOT

    public override void _EnterTree()
    {
        Initialize();
    }

    public override void _ExitTree(){
        Release();
    }
    #endregion

    #region INIT RELEASE
    /// <summary>
    /// Initialize the plugin dock
    /// </summary>
    private void Initialize(){
        dock = (Control)GD.Load<PackedScene>("addons/Tilemap Trainer/TilemapTrainer.tscn").Instantiate();
        AddControlToDock(DockSlot.LeftBl, dock);
    }

    /// <summary>
    /// Release and cleanup the plugin dock
    /// </summary>
    private void Release(){
        RemoveControlFromDocks(dock);
        dock.Free();
    }
    #endregion

}
#endif