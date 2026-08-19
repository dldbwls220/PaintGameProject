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

    public void CheckPaintableColor()
    {
        if (_checkRoutine != null) return;
        _checkRoutine = StartCoroutine(CheckPaintableColorRoutine());
    }

    public Dictionary<Color, float> GetColorRate()
    {
        var colorCounts = new Dictionary<Color, int>();
        int totalCount = 0;

        if (_grid != null)
        {
            foreach (Node node in _grid)
            {
                if (node._color == null) continue;

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
        if (Input.GetKeyDown(_debugCheckColorKey))
        {
            CheckPaintableColor();
        }
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
