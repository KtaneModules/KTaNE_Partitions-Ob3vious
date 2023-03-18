using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit {
    private Group _parent;
    private KMSelectable _selectable;
    private MeshRenderer _tile;

    private int _x;
    private int _y;

    public Unit(KMSelectable tile, int x, int y)
    {
        _selectable = tile;
        _tile = tile.GetComponent<MeshRenderer>();

        _selectable.OnInteract += delegate
        {
            if (_parent != null)
            {
                _selectable.AddInteractionPunch(0.25f);
                _parent.Press();
            }
            return false;
        };

        _selectable.OnHighlight += delegate
        {
            if (_parent != null)
                _parent.Highlight(true);
        };
        _selectable.OnHighlightEnded += delegate
        {
            if (_parent != null)
                _parent.Highlight(false);
        };

        _x = x;
        _y = y;
    }

    public void SetParent(Group group)
    {
        _parent = group;
    }

    public void SetColor(Color color)
    {
        _tile.material.color = color;
    }

    public int GetX()
    {
        return _x;
    }

    public int GetY()
    {
        return _y;
    }

    public KMSelectable GetSelectable()
    {
        return _selectable;
    }
}
