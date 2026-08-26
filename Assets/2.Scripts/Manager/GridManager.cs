using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Setting")]
    [SerializeField] Vector2 _gridWorldSize;
    [SerializeField] float _nodeRadius;
    [SerializeField] LayerMask _paintableMask;

    [Header("Gizzmos Setting")]
    [SerializeField] bool _displayGizzmos;

    [Header("Debug")]
    [SerializeField] KeyCode _debugCheckColorKey = KeyCode.F9;
    [SerializeField] int _nodesPerFrame = 50;

    Node[,] _grid;
    Coroutine _checkRoutine;

    public bool IsChecking { get; private set; }
    public event Action OnCheckComplete;

    float _nodeDiameter;
    int _gridSizeX, _gridSizeY;

    public int _maxSize => _gridSizeX * _gridSizeY;

    public void CreateGride(Vector2 WorldSize)
    {
        _gridWorldSize = WorldSize; 

        _nodeDiameter = _nodeRadius * 2;
        _gridSizeX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
        _gridSizeY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);

        _grid = new Node[_gridSizeX, _gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * _gridWorldSize.x / 2 - Vector3.forward * _gridWorldSize.y / 2;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * ( x * _nodeDiameter + _nodeRadius) + Vector3.forward * (y * _nodeDiameter + _nodeRadius);

                _grid[x,y] = new Node(worldPoint, x, y);                   
            }
        }
    }

    //public void CheckPaintableColor()
    //{
    //    if (_checkRoutine != null) return;
    //    _checkRoutine = StartCoroutine(CheckPaintableColorRoutine());
    //}

    public bool PaintNodesInRadius(Vector3 point, float radius, Color color)
    {
        if (_grid == null) return false;

        bool isColorChanged = false;

        Vector3 worldBottomLeft = transform.position
            - Vector3.right * _gridWorldSize.x / 2
            - Vector3.forward * _gridWorldSize.y / 2;

        float percentX = Mathf.Clamp01((point.x - worldBottomLeft.x) / _gridWorldSize.x);
        float percentY = Mathf.Clamp01((point.z - worldBottomLeft.z) / _gridWorldSize.y);

        int centerX = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int centerY = Mathf.RoundToInt((_gridSizeY - 1) * percentY);

        int cellRadius = Mathf.CeilToInt(radius / _nodeDiameter);
        float sqrRadius = radius * radius;

        int xMin = Mathf.Max(0, centerX - cellRadius);
        int xMax = Mathf.Min(_gridSizeX - 1, centerX + cellRadius);
        int yMin = Mathf.Max(0, centerY - cellRadius);
        int yMax = Mathf.Min(_gridSizeY - 1, centerY + cellRadius);

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                Node node = _grid[x, y];

                float dx = node._worldPosition.x - point.x;
                float dz = node._worldPosition.z - point.z;

                if (dx * dx + dz * dz <= sqrRadius)
                {
                    if (node._color != color)
                        isColorChanged = true;

                    node._color = color;
                }
            }
        }

        return isColorChanged;
    }

    public Dictionary<Color, float> GetColorRate()
    {
        var colorCounts = new Dictionary<Color, int>();
        int totalCount = 0;

        if (_grid != null)
        {
            foreach (Node node in _grid)
            {
                if (node._color == null || node._color.Value.a < 1) continue;

                Color c = node._color.Value;
                colorCounts.TryGetValue(c, out int count);
                colorCounts[c] = count + 1;
                totalCount++;
            }
        }

        var colorRateDic = new Dictionary<Color, float>();
        foreach (var kv in colorCounts)
            colorRateDic[kv.Key] = totalCount > 0 ? (float)kv.Value / totalCount : 0f;

        return colorRateDic;
    }

    IEnumerator CheckPaintableColorRoutine()
    {
        IsChecking = true;

        if (_grid != null)
        {
            int count = 0;
            foreach (Node node in _grid)
            {
                node.CheckNodePosColor(_paintableMask);
                count++;
                if (count % _nodesPerFrame == 0)
                    yield return null;
            }
        }

        IsChecking = false;
        _checkRoutine = null;
        OnCheckComplete?.Invoke();
    }

    void Update()
    {
        //if (Input.GetKeyDown(_debugCheckColorKey))
        //{
        //    CheckPaintableColor();
        //}
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(_gridWorldSize.x, 1, _gridWorldSize.y));

        if (_grid != null && _displayGizzmos)
        {
            foreach (Node node in _grid)
            {
                if (node._color != null)
                    Gizmos.color = node._color.Value;
                else
                    Gizmos.color = Color.white;

                Gizmos.DrawCube(node._worldPosition, Vector3.one * (_nodeDiameter - 0.1f));

            }
        }
    }
}
