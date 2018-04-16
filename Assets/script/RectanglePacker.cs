using System;
using System.Collections.Generic;

public struct PackerRect
{
    public float x;
    public float y;
    public float w;
    public float h;

    public int ID;

    public PackerRect(float _x, float _y, float _w, float _h, int id)
    {
        x = _x;
        y = _y;
        w = _w;
        h = _h;
        ID = id;
    }

    public static PackerRect Default = new PackerRect(0,0,0,0,0);

    public float Area
    {
        get { return w * h; }
    }
}
public class RectanglePacker
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    List<Node> nodes = new List<Node>();

    public RectanglePacker()
    {
        nodes.Add(new Node(0, 0, int.MaxValue, int.MaxValue));
    }

    public bool Pack(int w, int h, out int x, out int y)
    {
        for (int i = 0; i < nodes.Count; ++i)
        {
            if (w <= nodes[i].W && h <= nodes[i].H)
            {
                var node = nodes[i];
                nodes.RemoveAt(i);
                x = node.X;
                y = node.Y;
                int r = x + w;
                int b = y + h;
                nodes.Add(new Node(r, y, node.Right - r, h));
                nodes.Add(new Node(x, b, w, node.Bottom - b));
                nodes.Add(new Node(r, b, node.Right - r, node.Bottom - b));
                Width = Math.Max(Width, r);
                Height = Math.Max(Height, b);
                return true;
            }
        }
        x = 0;
        y = 0;
        return false;
    }

    public struct Node
    {
        public int X;
        public int Y;
        public int W;
        public int H;

        public Node(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public int Right
        {
            get { return X + W; }
        }

        public int Bottom
        {
            get { return Y + H; }
        }
    }
}