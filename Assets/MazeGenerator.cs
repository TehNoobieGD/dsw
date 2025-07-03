using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private MazeCell _mazeCellPrefab;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private GameObject batteryPrefab;
    [SerializeField] private int _mazeWidth = 50;
    [SerializeField] private int _mazeDepth = 50;

    private MazeCell[,] _mazeGrid;
    private bool[,] _isRoom;
    private List<(int startX, int startZ, int width, int height)> _placedRooms = new();

    void Start()
    {
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];
        _isRoom = new bool[_mazeWidth, _mazeDepth];

        // Instantiate maze grid
        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
            }
        }

        // Place the big center room first (10x10 in center)
        PlaceCenterBigRoom();

        // Place other rooms
        PlaceRooms();

        // Generate maze from first unvisited cell
        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                if (!_mazeGrid[x, z].IsVisited)
                {
                    GenerateMaze(null, _mazeGrid[x, z]);
                    goto MazeDone;
                }
            }
        }

    MazeDone:

        // Connect rooms to maze with limited entrances
        ConnectRoomsToMaze();

        // Add random dead ends for variation
        AddRandomDeadEnds();

        // Create exit on edge and spawn exit GameObject
        CreateExit();

        // Spawn batteries in rooms
        SpawnBatteries();
    }

    private void PlaceCenterBigRoom()
    {
        int centerX = _mazeWidth / 2 - 5;
        int centerZ = _mazeDepth / 2 - 5;
        int roomWidth = 10;
        int roomHeight = 10;

        for (int x = centerX; x < centerX + roomWidth; x++)
        {
            for (int z = centerZ; z < centerZ + roomHeight; z++)
            {
                _isRoom[x, z] = true;
                _mazeGrid[x, z].Visit();
            }
        }

        // Clear inner walls but keep outer walls intact (except entrances)
        for (int x = centerX; x < centerX + roomWidth; x++)
        {
            for (int z = centerZ; z < centerZ + roomHeight; z++)
            {
                if (x < centerX + roomWidth - 1) _mazeGrid[x, z].ClearRightWall();
                if (z < centerZ + roomHeight - 1) _mazeGrid[x, z].ClearFrontWall();
                if (x > centerX) _mazeGrid[x, z].ClearLeftWall();
                if (z > centerZ) _mazeGrid[x, z].ClearBackWall();
            }
        }

        _placedRooms.Add((centerX, centerZ, roomWidth, roomHeight));
    }

    private void PlaceRooms()
    {
        int largeRoomCount = 0;
        int attempts = 0;

        while (attempts < 1000)
        {
            int roomWidth = Random.Range(2, 4);
            int roomHeight = Random.Range(2, 4);

            if (Random.value < 0.1f && largeRoomCount < 2)
            {
                roomWidth = Random.Range(4, 7);
                roomHeight = Random.Range(4, 7);
                largeRoomCount++;
            }

            int startX = Random.Range(0, _mazeWidth - roomWidth);
            int startZ = Random.Range(0, _mazeDepth - roomHeight);

            // Prevent overlapping with center big room
            if (startX < _mazeWidth / 2 + 5 && startX + roomWidth > _mazeWidth / 2 - 5 &&
                startZ < _mazeDepth / 2 + 5 && startZ + roomHeight > _mazeDepth / 2 - 5)
            {
                attempts++;
                continue;
            }

            if (CanPlaceRoom(startX, startZ, roomWidth, roomHeight))
            {
                for (int x = startX; x < startX + roomWidth; x++)
                {
                    for (int z = startZ; z < startZ + roomHeight; z++)
                    {
                        _isRoom[x, z] = true;
                        _mazeGrid[x, z].Visit();
                    }
                }

                for (int x = startX; x < startX + roomWidth; x++)
                {
                    for (int z = startZ; z < startZ + roomHeight; z++)
                    {
                        if (x < startX + roomWidth - 1) _mazeGrid[x, z].ClearRightWall();
                        if (z < startZ + roomHeight - 1) _mazeGrid[x, z].ClearFrontWall();
                        if (x > startX) _mazeGrid[x, z].ClearLeftWall();
                        if (z > startZ) _mazeGrid[x, z].ClearBackWall();
                    }
                }

                _placedRooms.Add((startX, startZ, roomWidth, roomHeight));
            }

            attempts++;
            if (largeRoomCount >= 2 && attempts > 50) break;
        }
    }

    private void ConnectRoomsToMaze()
    {
        foreach (var (startX, startZ, roomWidth, roomHeight) in _placedRooms)
        {
            if (roomWidth == 10 && roomHeight == 10 &&
                startX == _mazeWidth / 2 - 5 && startZ == _mazeDepth / 2 - 5)
            {
                ConnectBigRoomEntrances(startX, startZ, roomWidth, roomHeight);
            }
            else
            {
                List<(int x, int z, string dir)> potentialEntrances = new();

                for (int x = startX; x < startX + roomWidth; x++)
                {
                    if (startZ - 1 >= 0 && !_isRoom[x, startZ - 1] && _mazeGrid[x, startZ - 1].IsVisited)
                        potentialEntrances.Add((x, startZ, "back"));
                    if (startZ + roomHeight < _mazeDepth && !_isRoom[x, startZ + roomHeight] && _mazeGrid[x, startZ + roomHeight].IsVisited)
                        potentialEntrances.Add((x, startZ + roomHeight - 1, "front"));
                }

                for (int z = startZ; z < startZ + roomHeight; z++)
                {
                    if (startX - 1 >= 0 && !_isRoom[startX - 1, z] && _mazeGrid[startX - 1, z].IsVisited)
                        potentialEntrances.Add((startX, z, "left"));
                    if (startX + roomWidth < _mazeWidth && !_isRoom[startX + roomWidth, z] && _mazeGrid[startX + roomWidth, z].IsVisited)
                        potentialEntrances.Add((startX + roomWidth - 1, z, "right"));
                }

                var selected = potentialEntrances.OrderBy(_ => Random.value).Take(2).ToList();

                if (selected.Count < 2)
                {
                    List<(int x, int z, string dir)> forcedEntrances = new();

                    for (int x = startX; x < startX + roomWidth; x++)
                    {
                        if (startZ - 1 >= 0 && !_isRoom[x, startZ - 1])
                            forcedEntrances.Add((x, startZ, "back"));
                        if (startZ + roomHeight < _mazeDepth && !_isRoom[x, startZ + roomHeight])
                            forcedEntrances.Add((x, startZ + roomHeight - 1, "front"));
                    }

                    for (int z = startZ; z < startZ + roomHeight; z++)
                    {
                        if (startX - 1 >= 0 && !_isRoom[startX - 1, z])
                            forcedEntrances.Add((startX, z, "left"));
                        if (startX + roomWidth < _mazeWidth && !_isRoom[startX + roomWidth, z])
                            forcedEntrances.Add((startX + roomWidth - 1, z, "right"));
                    }

                    while (selected.Count < 2 && forcedEntrances.Count > 0)
                    {
                        var e = forcedEntrances[Random.Range(0, forcedEntrances.Count)];
                        forcedEntrances.Remove(e);
                        selected.Add(e);
                        _mazeGrid[e.x, e.z].Visit();
                    }
                }

                foreach (var (x, z, dir) in selected)
                {
                    var roomCell = _mazeGrid[x, z];
                    MazeCell neighbor = dir switch
                    {
                        "left" => _mazeGrid[x - 1, z],
                        "right" => _mazeGrid[x + 1, z],
                        "front" => _mazeGrid[x, z + 1],
                        "back" => _mazeGrid[x, z - 1],
                        _ => null
                    };

                    if (neighbor != null)
                    {
                        neighbor.Visit();
                        switch (dir)
                        {
                            case "left": roomCell.ClearLeftWall(); neighbor.ClearRightWall(); break;
                            case "right": roomCell.ClearRightWall(); neighbor.ClearLeftWall(); break;
                            case "front": roomCell.ClearFrontWall(); neighbor.ClearBackWall(); break;
                            case "back": roomCell.ClearBackWall(); neighbor.ClearFrontWall(); break;
                        }
                    }
                }
            }
        }
    }

    private void ConnectBigRoomEntrances(int startX, int startZ, int roomWidth, int roomHeight)
    {
        List<(int x, int z, string dir)> potentialEntrances = new();

        for (int x = startX; x < startX + roomWidth; x++)
        {
            if (startZ - 1 >= 0 && !_isRoom[x, startZ - 1] && _mazeGrid[x, startZ - 1].IsVisited)
                potentialEntrances.Add((x, startZ, "back"));
            if (startZ + roomHeight < _mazeDepth && !_isRoom[x, startZ + roomHeight] && _mazeGrid[x, startZ + roomHeight].IsVisited)
                potentialEntrances.Add((x, startZ + roomHeight - 1, "front"));
        }

        for (int z = startZ; z < startZ + roomHeight; z++)
        {
            if (startX - 1 >= 0 && !_isRoom[startX - 1, z] && _mazeGrid[startX - 1, z].IsVisited)
                potentialEntrances.Add((startX, z, "left"));
            if (startX + roomWidth < _mazeWidth && !_isRoom[startX + roomWidth, z] && _mazeGrid[startX + roomWidth, z].IsVisited)
                potentialEntrances.Add((startX + roomWidth - 1, z, "right"));
        }

        var selected = potentialEntrances.OrderBy(_ => Random.value).Take(6).ToList();

        if (selected.Count < 6)
        {
            List<(int x, int z, string dir)> forcedEntrances = new();

            for (int x = startX; x < startX + roomWidth; x++)
            {
                if (startZ - 1 >= 0 && !_isRoom[x, startZ - 1])
                    forcedEntrances.Add((x, startZ, "back"));
                if (startZ + roomHeight < _mazeDepth && !_isRoom[x, startZ + roomHeight])
                    forcedEntrances.Add((x, startZ + roomHeight - 1, "front"));
            }

            for (int z = startZ; z < startZ + roomHeight; z++)
            {
                if (startX - 1 >= 0 && !_isRoom[startX - 1, z])
                    forcedEntrances.Add((startX, z, "left"));
                if (startX + roomWidth < _mazeWidth && !_isRoom[startX + roomWidth, z])
                    forcedEntrances.Add((startX + roomWidth - 1, z, "right"));
            }

            while (selected.Count < 6 && forcedEntrances.Count > 0)
            {
                var e = forcedEntrances[Random.Range(0, forcedEntrances.Count)];
                forcedEntrances.Remove(e);
                selected.Add(e);
                _mazeGrid[e.x, e.z].Visit();
            }
        }

        foreach (var (x, z, dir) in selected)
        {
            var roomCell = _mazeGrid[x, z];
            MazeCell neighbor = dir switch
            {
                "left" => _mazeGrid[x - 1, z],
                "right" => _mazeGrid[x + 1, z],
                "front" => _mazeGrid[x, z + 1],
                "back" => _mazeGrid[x, z - 1],
                _ => null
            };

            if (neighbor != null)
            {
                neighbor.Visit();
                switch (dir)
                {
                    case "left": roomCell.ClearLeftWall(); neighbor.ClearRightWall(); break;
                    case "right": roomCell.ClearRightWall(); neighbor.ClearLeftWall(); break;
                    case "front": roomCell.ClearFrontWall(); neighbor.ClearBackWall(); break;
                    case "back": roomCell.ClearBackWall(); neighbor.ClearFrontWall(); break;
                }
            }
        }
    }

    private void AddRandomDeadEnds()
    {
        for (int x = 1; x < _mazeWidth - 1; x++)
        {
            for (int z = 1; z < _mazeDepth - 1; z++)
            {
                if (!_mazeGrid[x, z].IsVisited && !_isRoom[x, z])
                {
                    if (Random.value < 0.1f)
                    {
                        _mazeGrid[x, z].Visit();
                    }
                }
            }
        }
    }

    private void CreateExit()
    {
        int edge = Random.Range(0, 4);
        int exitX = 0, exitZ = 0;

        // Pick a random cell on one of the four edges
        switch (edge)
        {
            case 0: // Bottom edge (back)
                exitX = Random.Range(0, _mazeWidth);
                exitZ = 0;
                break;
            case 1: // Top edge (front)
                exitX = Random.Range(0, _mazeWidth);
                exitZ = _mazeDepth - 1;
                break;
            case 2: // Left edge
                exitX = 0;
                exitZ = Random.Range(0, _mazeDepth);
                break;
            case 3: // Right edge
                exitX = _mazeWidth - 1;
                exitZ = Random.Range(0, _mazeDepth);
                break;
        }

        MazeCell cell = _mazeGrid[exitX, exitZ];
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        // Determine which wall to remove and where to place the exit prefab
        switch (edge)
        {
            case 0: // Bottom edge: remove back wall, place exit at z=0 facing -Z (180°)
                cell.ClearBackWall();
                spawnPos = new Vector3(exitX + 0.5f, 0f, 0f);
                spawnRot = Quaternion.Euler(0f, 180f, 0f);
                break;
            case 1: // Top edge: remove front wall, place exit at z=depth facing +Z (0°)
                cell.ClearFrontWall();
                spawnPos = new Vector3(exitX + 0.5f, 0f, exitZ + 1f);
                spawnRot = Quaternion.identity;
                break;
            case 2: // Left edge: remove left wall, place exit at x=0 facing -X (-90°)
                cell.ClearLeftWall();
                spawnPos = new Vector3(0f, 0f, exitZ + 0.5f);
                spawnRot = Quaternion.Euler(0f, -90f, 0f);
                break;
            case 3: // Right edge: remove right wall, place exit at x=width facing +X (+90°)
                cell.ClearRightWall();
                spawnPos = new Vector3(exitX + 1f, 0f, exitZ + 0.5f);
                spawnRot = Quaternion.Euler(0f, 90f, 0f);
                break;
        }

        // Instantiate the exit prefab at the computed position and rotation
        if (exitPrefab != null)
        {
            Instantiate(exitPrefab, spawnPos, spawnRot);
        }
    }

    private bool CanPlaceRoom(int startX, int startZ, int width, int height)
    {
        for (int x = startX - 1; x <= startX + width; x++)
        {
            for (int z = startZ - 1; z <= startZ + height; z++)
            {
                if (x < 0 || x >= _mazeWidth || z < 0 || z >= _mazeDepth) return false;
                if (_isRoom[x, z]) return false;
            }
        }
        return true;
    }

    private void GenerateMaze(MazeCell parent, MazeCell current)
    {
        current.Visit();

        var neighbors = GetUnvisitedNeighbors(current);

        while (neighbors.Count > 0)
        {
            var chosen = neighbors[Random.Range(0, neighbors.Count)];

            RemoveWallBetween(current, chosen);

            GenerateMaze(current, chosen);

            neighbors = GetUnvisitedNeighbors(current);
        }
    }

    private List<MazeCell> GetUnvisitedNeighbors(MazeCell cell)
    {
        List<MazeCell> neighbors = new();

        Vector3 pos = cell.transform.position;
        int x = (int)pos.x;
        int z = (int)pos.z;

        if (x > 0 && !_mazeGrid[x - 1, z].IsVisited && !_isRoom[x - 1, z])
            neighbors.Add(_mazeGrid[x - 1, z]);
        if (x < _mazeWidth - 1 && !_mazeGrid[x + 1, z].IsVisited && !_isRoom[x + 1, z])
            neighbors.Add(_mazeGrid[x + 1, z]);
        if (z > 0 && !_mazeGrid[x, z - 1].IsVisited && !_isRoom[x, z - 1])
            neighbors.Add(_mazeGrid[x, z - 1]);
        if (z < _mazeDepth - 1 && !_mazeGrid[x, z + 1].IsVisited && !_isRoom[x, z + 1])
            neighbors.Add(_mazeGrid[x, z + 1]);

        return neighbors;
    }

    private void RemoveWallBetween(MazeCell a, MazeCell b)
    {
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        if (posA.x == posB.x)
        {
            if (posA.z > posB.z)
            {
                a.ClearBackWall();
                b.ClearFrontWall();
            }
            else
            {
                a.ClearFrontWall();
                b.ClearBackWall();
            }
        }
        else if (posA.z == posB.z)
        {
            if (posA.x > posB.x)
            {
                a.ClearLeftWall();
                b.ClearRightWall();
            }
            else
            {
                a.ClearRightWall();
                b.ClearLeftWall();
            }
        }
    }

    private void SpawnBatteries()
{
    if (batteryPrefab == null) return;

    int wallLayerMask = LayerMask.GetMask("Wall");

    // Spawn batteries in rooms as before, with increased chance
    foreach (var room in _placedRooms)
    {
        bool isBigRoom = (room.width >= 6 && room.height >= 6);
        bool spawnInSmallRoom = (!isBigRoom && Random.value < 0.6f);

        if (!(isBigRoom || spawnInSmallRoom))
            continue;

        float centerX = room.startX + (room.width - 1) / 2f + 0.5f;
        float centerZ = room.startZ + (room.height - 1) / 2f + 0.5f;
        float spawnY = 0.0664f;
        Vector3 spawnPos = new Vector3(centerX, spawnY, centerZ);

        float checkRadius = 0.3f;

        Collider[] hits = Physics.OverlapSphere(spawnPos, checkRadius, wallLayerMask);
        if (hits.Length == 0)
        {
            Instantiate(batteryPrefab, spawnPos, Quaternion.identity);
        }
    }

    // Spawn batteries in corridors, only if adjacent to walls (so batteries are placed "against" walls)
    for (int x = 1; x < _mazeWidth - 1; x++)
    {
        for (int z = 1; z < _mazeDepth - 1; z++)
        {
            if (!_isRoom[x, z] && _mazeGrid[x, z].IsVisited)
            {
                // Check if at least one neighbor cell is a wall (or has wall layer)
                bool adjacentToWall = false;

                // Check 4 neighbors: left, right, forward, back
                Vector3[] neighborPositions = new Vector3[]
                {
                    new Vector3(x - 0.5f, 0.0664f, z + 0.5f),
                    new Vector3(x + 1.5f, 0.0664f, z + 0.5f),
                    new Vector3(x + 0.5f, 0.0664f, z - 0.5f),
                    new Vector3(x + 0.5f, 0.0664f, z + 1.5f)
                };

                foreach (var neighborPos in neighborPositions)
                {
                    Collider[] wallHits = Physics.OverlapSphere(neighborPos, 0.2f, wallLayerMask);
                    if (wallHits.Length > 0)
                    {
                        adjacentToWall = true;
                        break;
                    }
                }

                if (adjacentToWall)
                {
                    // Slightly higher chance if adjacent to wall
                    if (Random.value < 0.10f) // 10% chance near walls instead of 5%
                    {
                        Vector3 spawnPos = new Vector3(x + 0.5f, 0.0664f, z + 0.5f);

                        // Double check no wall exactly here
                        Collider[] hits = Physics.OverlapSphere(spawnPos, 0.3f, wallLayerMask);
                        if (hits.Length == 0)
                        {
                            Instantiate(batteryPrefab, spawnPos, Quaternion.identity);
                        }
                    }
                }
            }
        }
    }
}

// Helper function to check if position overlaps a wall
private bool IsOverlappingWall(Vector3 position)
{
    float checkRadius = 0.3f;  // Adjust radius based on battery size

    // Use Physics.OverlapSphere to find colliders in radius around position
    Collider[] hits = Physics.OverlapSphere(position, checkRadius);

    foreach (var hit in hits)
    {
        if (hit.CompareTag("Wall"))
        {
            return true;
        }
    }

    return false;
}
}
