using UnityEngine;

public class Node
{
    public bool _paintable { get; set; }
    public Color? _color { get; set; }
    public Vector3 _worldPosition { get; set; }
    Vector2Int _gridIndex;
    public int _gridX 
    {
        get => _gridIndex.x;
        set => _gridIndex.x = value;
    }
    public int _grideY
    {
        get => _gridIndex.y;
        set => _gridIndex.y = value;
    }

    public Node(Vector3 worldPosition, int gridX, int grideY)
    {
        _worldPosition = worldPosition + Vector3.up * 20;
        _gridX = gridX;
        _grideY = grideY;
    }

    public void CheckNodePosColor(LayerMask paintableMask)
    {
        Ray ray = new Ray(_worldPosition, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, paintableMask))
        {
            // WorldInkZoneReceiver receiver = hit.collider.GetComponent<WorldInkZoneReceiver>();
            //
            // if (receiver != null)
            // {
            //     _color = receiver.CheckPaintColor(hit);
            // }

            Paintabale paintable = hit.collider.GetComponentInParent<Paintabale>();

            if (paintable != null)
            {
                _color = paintable.CheckPaintColor(hit);
            }
        }
    }
}
