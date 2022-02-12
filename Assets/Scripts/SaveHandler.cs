using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class SaveHandler : Singleton<SaveHandler> {
    Dictionary<string, Tilemap> tilemaps = new Dictionary<string, Tilemap>();
    Dictionary<TileBase, BuildingObjectBase> tileBaseToBuildingObject = new Dictionary<TileBase, BuildingObjectBase>();
    Dictionary<string, TileBase> guidToTileBase = new Dictionary<string, TileBase>();

    [SerializeField] BoundsInt bounds;
    [SerializeField] string filename = "tilemapData.json";

    private void Start() {
        InitTilemaps();
        InitTileReferences();
    }

    private void InitTileReferences() {
        BuildingObjectBase[] buildables = Resources.LoadAll<BuildingObjectBase>("Scriptables/Buildables");

        foreach (BuildingObjectBase buildable in buildables) {
            if (!tileBaseToBuildingObject.ContainsKey(buildable.TileBase)) {
                tileBaseToBuildingObject.Add(buildable.TileBase, buildable);
                guidToTileBase.Add(buildable.name, buildable.TileBase);
            } else {
                Debug.LogError("TileBase " + buildable.TileBase.name + " is already in use by " + tileBaseToBuildingObject[buildable.TileBase].name);
            }
        }
    }

    private void InitTilemaps() {
        // get all tilemaps from scene
        // and write to dictionary
        Tilemap[] maps = FindObjectsOfType<Tilemap>();

        // the hierarchy name must be unique
        // you might add some checks here to make sure
        foreach (var map in maps) {
            // if you have tilemaps you don't want to safe - filter them here
            tilemaps.Add(map.name, map);
        }
    }

    public void OnSave() {
        // List that will later be safed
        List<TilemapData> data = new List<TilemapData>();

        // foreach existing tilemap
        foreach (var mapObj in tilemaps) {
            TilemapData mapData = new TilemapData();
            mapData.key = mapObj.Key;

            // use your boundsInt variable for the bounds
            // alternatetively you can use mapObj.Value.cellBounds
            // https://docs.unity3d.com/ScriptReference/Tilemaps.Tilemap-cellBounds.html

            BoundsInt boundsForThisMap = mapObj.Value.cellBounds;

            for (int x = boundsForThisMap.xMin; x < boundsForThisMap.xMax; x++) {
                for (int y = boundsForThisMap.yMin; y < boundsForThisMap.yMax; y++) {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    TileBase tile = mapObj.Value.GetTile(pos);

                    if (tile != null && tileBaseToBuildingObject.ContainsKey(tile)) {
                        String guid = tileBaseToBuildingObject[tile].name;
                        TileInfo ti = new TileInfo(pos, guid);
                        // Add "TileInfo" to "Tiles" List of "TilemapData"
                        mapData.tiles.Add(ti);
                    }
                }
            }

            // Add "TilemapData" Object to List
            data.Add(mapData);
        }
        FileHandler.SaveToJSON<TilemapData>(data, filename);
    }

    public void OnLoad() {
        List<TilemapData> data = FileHandler.ReadListFromJSON<TilemapData>(filename);

        foreach (var mapData in data) {
            // if key does NOT exist in dictionary skip it
            if (!tilemaps.ContainsKey(mapData.key)) {
                Debug.LogError("Found saved data for tilemap called '" + mapData.key + "', but Tilemap does not exist in scene.");
                continue;
            }

            // get according map
            var map = tilemaps[mapData.key];

            // clear map
            map.ClearAllTiles();

            if (mapData.tiles != null && mapData.tiles.Count > 0) {
                foreach (var tile in mapData.tiles) {

                    if (guidToTileBase.ContainsKey(tile.guidForBuildable)) {
                        map.SetTile(tile.position, guidToTileBase[tile.guidForBuildable]);
                    } else {
                        Debug.LogError("Refernce " + tile.guidForBuildable + " could not be found.");
                    }

                }
            }
        }
    }
}


[Serializable]
public class TilemapData {
    public string key; // the key of your dictionary for the tilemap - here: the name of the map in the hierarchy
    public List<TileInfo> tiles = new List<TileInfo>();
}

[Serializable]
public class TileInfo {
    public string guidForBuildable;
    public Vector3Int position;

    public TileInfo(Vector3Int pos, string guid) {
        position = pos;
        guidForBuildable = guid;
    }
}