using UnityEngine;

[System.Serializable]
public struct GridCoord
{
    public int x;
    public int y;

    public GridCoord(int x, int y)
    { 
        this.x = x; 
        this.y = y; 
    }

    public override string ToString()
    {
        return $"(GridCoord : {this.x}, {this.y})";
    }
}
