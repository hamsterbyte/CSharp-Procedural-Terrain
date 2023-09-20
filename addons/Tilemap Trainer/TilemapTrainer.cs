#if TOOLS
using System;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mime;
using System.Text;


[Tool]
public partial class TilemapTrainer : Control{
    #region GODOT

    public override void _EnterTree(){
        Initialize();
    }

    public override void _ExitTree(){
        Release();
    }

    #endregion

    #region INIT

    /// <summary>
    /// Initialize the plugin
    /// </summary>
    private void Initialize(){
        GD.Print("Tilemap Trainer Plugin Enabled");
        AssignMemberVariables();
        ConnectEvents();
        ClearPermutations();
    }


    private Button _generateButton;
    private Button _analyzeButton;
    private SpinBox _tileSizeBox;
    private FileDialog _tilesetFileDialog;
    private LineEdit _tilesetPath;
    private Button _tilesetSelectButton;
    private FileDialog _analysisFileDialog;
    private LineEdit _analysisPath;
    private Button _analysisSelectButton;
    private Node _root;

    /// <summary>
    /// Get and assign appropriate values for required member variables
    /// </summary>
    private void AssignMemberVariables(){
        _root = GetTree().EditedSceneRoot;
        _generateButton = GetNode("VContainer/Buttons/Generate Button") as Button;
        _analyzeButton = GetNode("VContainer/Buttons/Analyze Button") as Button;

        _tileSizeBox = GetNode("VContainer/Panel/Margin/Parameters/Tile Size/SpinBox") as SpinBox;

        _tilesetFileDialog = GetNode("VContainer/Panel/Margin/Parameters/Tileset/FileDialog") as FileDialog;
        _tilesetPath = GetNode("VContainer/Panel/Margin/Parameters/Tileset/Control/Path") as LineEdit;
        _tilesetSelectButton =
            GetNode("VContainer/Panel/Margin/Parameters/Tileset/Control/MarginContainer/Button") as Button;

        _analysisFileDialog = GetNode("VContainer/Panel/Margin/Parameters/Texture Coords/FileDialog") as FileDialog;
        _analysisPath = GetNode("VContainer/Panel/Margin/Parameters/Texture Coords/Control/Path") as LineEdit;
        _analysisSelectButton =
            GetNode("VContainer/Panel/Margin/Parameters/Texture Coords/Control/MarginContainer/Button") as Button;
    }

    /// <summary>
    /// Connect events to this script
    /// </summary>
    private void ConnectEvents(){
        if (_generateButton != null) _generateButton.Pressed += Generate;
        if (_analyzeButton != null) _analyzeButton.Pressed += Analyze;

        if (_tilesetSelectButton != null) _tilesetSelectButton.Pressed += ShowTilesetFileDialog;
        if (_tilesetFileDialog != null) _tilesetFileDialog.FileSelected += SetTileSetPath;
        if (_tilesetPath != null) _tilesetPath.TextChanged += HandleTilesetPathChanged;

        if (_analysisSelectButton != null) _analysisSelectButton.Pressed += ShowAnalysisFileDialog;
        if (_analysisFileDialog != null) _analysisFileDialog.FileSelected += SetAnalysisPath;
        if (_analysisPath != null) _analysisPath.TextChanged += HandleAnalysisPathChanged;
    }

    /// <summary>
    /// Show the Tileset selection dialog
    /// </summary>
    private void ShowTilesetFileDialog(){
        _tilesetFileDialog.Visible = true;
    }

    private void SetTileSetPath(string path){
        _tilesetPath.Text = _tilesetFileDialog.CurrentPath;
        _tilesetSelectButton.Visible = false;
    }

    private void HandleTilesetPathChanged(string path){
        _tilesetSelectButton.Visible = path == "";
        GD.Print("Path Changed");
    }

    private void ShowAnalysisFileDialog(){
        _analysisFileDialog.Visible = true;
    }

    private void SetAnalysisPath(string path){
        _analysisPath.Text = _analysisFileDialog.CurrentPath;
        _analysisSelectButton.Visible = false;
    }

    private void HandleAnalysisPathChanged(string path){
        _analysisSelectButton.Visible = path == "";
        GD.Print("Analysis Path Changed");
    }

    /// <summary>
    /// Release and cleanup the plugin
    /// </summary>
    private void Release(){
        GD.Print("Tilemap Trainer Plugin Disabled");
        DisconnectEvents();
    }

    /// <summary>
    /// Disconnect events from this script
    /// </summary>
    private void DisconnectEvents(){
        if (_generateButton != null) _generateButton.Pressed -= Generate;
        if (_analyzeButton != null) _analyzeButton.Pressed -= Analyze;

        if (_tilesetSelectButton != null) _tilesetSelectButton.Pressed -= ShowTilesetFileDialog;
        if (_tilesetFileDialog != null) _tilesetFileDialog.FileSelected -= SetTileSetPath;
        if (_tilesetPath != null) _tilesetPath.TextChanged -= HandleTilesetPathChanged;

        if (_analysisSelectButton != null) _analysisSelectButton.Pressed -= ShowAnalysisFileDialog;
        if (_analysisFileDialog != null) _analysisFileDialog.FileSelected -= SetAnalysisPath;
        if (_analysisPath != null) _analysisPath.TextChanged -= HandleAnalysisPathChanged;
    }

    #endregion

    #region GENERATION

    private List<int[]> _permutations;
    private Vector2I _startCoords = new(2, 2);
    private const int OFFSET = 4;
    private TileMap _templateTilemap;
    private TileMap _analysisTilemap;

    private readonly Vector2I[] _neighbourOffsets = {
        new(-1, -1), //Up Left
        new(0, -1), //Up Center
        new(1, -1), //Up Right
        new(-1, 0), //Left Center
        new(1, 0), //Right Center
        new(-1, 1), //Down Left
        new(0, 1), //Down Center
        new(1, 1), //Down Center
    };

    /// <summary>
    /// Generate a template tilemap that includes all 256 possible permutations
    /// Also generates a tilemap to accept the tiles to be analyzed
    /// </summary>
    private void Generate(){
        _root = GetTree().EditedSceneRoot;
        GenerateTemplateTilemap();
        GenerateAnalysisTileMap();
    }

    /// <summary>
    /// Generate the template tilemap
    /// </summary>
    private void GenerateTemplateTilemap(){
        GD.Print("Generating Template Map...");
        ClearPermutations();
        _templateTilemap?.Free();
        TileSet tileSet = GD.Load<TileSet>("res://addons/Tilemap Trainer/templateTiles.tres");
        tileSet.TileSize = new Vector2I(32, 32);
        _templateTilemap = new TileMap();
        _templateTilemap.Scale = (float)(_tileSizeBox.Value / 32f) * Vector2.One;
        _templateTilemap.Name = "Template";
        _templateTilemap.TileSet = tileSet;
        _root.AddChild(_templateTilemap);
        _templateTilemap.Owner = GetTree().EditedSceneRoot;
        GeneratePermutations();
    }

    private void GenerateAnalysisTileMap(){
        GD.Print("Generating Analysis Map...");
        _analysisTilemap?.Free();
        _analysisTilemap = new TileMap();
        _analysisTilemap.Name = "Analysis";
        _root.AddChild(_analysisTilemap);
        _analysisTilemap.Owner = GetTree().EditedSceneRoot;
        if (ResourceLoader.Exists(_tilesetPath.Text)){
            TileSet tileSet = GD.Load<TileSet>(_tilesetPath.Text);
            tileSet.TileSize = new Vector2I((int)_tileSizeBox.Value, (int)_tileSizeBox.Value);
            _analysisTilemap.TileSet = tileSet;
        }
        else{
            GD.Print("No Tileset provided. You will have to set one up on the Analysis tilemap before you continuing");
        }
    }

    /// <summary>
    /// Generate permutations for all 8 neighbours in the tile map and place the appropriate sprites in the template
    /// </summary>
    private void GeneratePermutations(){
        int[] arr = new int[8];
        GenerateBinaryStrings(arr, 0);

        Vector2I _centerTileCoords = new(1, 0);
        Vector2I _neighbourTileCoords = new(0, 0);
        int currentY = 0;
        int currentX = 0;
        foreach (int[] permutation in _permutations){
            Vector2I permutationLocation =
                _startCoords + new Vector2I(currentX * OFFSET, currentY / 16 * OFFSET);
            //Set center tile
            _templateTilemap.SetCell(0, permutationLocation, 0, _centerTileCoords);
            for (int i = 0; i < 8; i++){
                if (permutation[i] == 0) continue;
                _templateTilemap.SetCell(0, permutationLocation + _neighbourOffsets[i], 0, _neighbourTileCoords);
            }

            if (currentX == 15){
                currentX = 0;
            }
            else{
                currentX++;
            }

            currentY++;
        }
    }

    #endregion

    /// <summary>
    /// Analyze the user placed tiles to associate each permutation with a particular tile
    /// </summary>
    private void Analyze(){
        GD.Print("Analyzing Tilemap...");
        AnalyzeAll();
    }


    #region PERMUTATIONS

    /// <summary>
    /// Clear permutations list or generate a new one if it doesn't exist
    /// </summary>
    private void ClearPermutations(){
        //Clear permutations list
        if (_permutations != null){
            _permutations.Clear();
        }
        else{
            _permutations = new List<int[]>();
        }
    }


    /// <summary>
    /// Generate all binary strings that represent bitmasks
    /// </summary>
    /// <param name="arr">Input array</param>
    /// <param name="i">Current index in array</param>
    /// <param name="powerOf">Number of permutations = 2 ^ this number</param>
    private void GenerateBinaryStrings(int[] arr, int i, int powerOf = 8){
        while (true){
            if (i == powerOf){
                AddToList(arr);
                return;
            }

            arr[i] = 0;
            GenerateBinaryStrings(arr, i + 1, powerOf);

            arr[i] = 1;
            i += 1;
        }
    }

    /// <summary>
    /// Add permutation to the permutations list
    /// </summary>
    /// <param name="arr"></param>
    private void AddToList(int[] arr){
        int[] current = new int[arr.Length];
        arr.CopyTo(current, 0);
        _permutations.Add(current);
    }

    #endregion

    #region ANALYSIS

    private Dictionary<int, Vector2I> _bitmaskDictionary;

    private void AnalyzeAll(){
        if (_analysisPath.Text == string.Empty){
            GD.Print("Please create a coordinate file.");
            return;
        }

        _bitmaskDictionary = new Dictionary<int, Vector2I>();
        int xOffset = 0;
        int yOffset = 0;
        int current = 0;
        StringBuilder builder = new();


        while (current <= 255){
            Vector2I currentLocation = _startCoords + new Vector2I(xOffset * OFFSET, yOffset * OFFSET);
            _bitmaskDictionary.Add(current, _analysisTilemap.GetCellAtlasCoords(0, currentLocation));

            if (xOffset == 15){
                xOffset = 0;
                yOffset++;
            }
            else{
                xOffset++;
            }

            current++;
        }

        string filePath = _analysisPath.Text.Remove(0, 6);
        GD.Print($"Saving Texture Coordinates to {filePath}");
        TilemapHelper.SaveTextureCoords(filePath, _bitmaskDictionary);
    }

    #endregion
}
#endif