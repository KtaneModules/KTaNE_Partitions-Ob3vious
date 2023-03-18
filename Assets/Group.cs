using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Group {
    private List<Unit> _units;
    private int[] _color = new int[3];

    private bool _isSelected = false;
    private bool _isRegrouped = false;
    private bool _isHighlighted = false;

    private GroupManager _manager;

    public Group(Unit unit, GroupManager manager)
    {
        _manager = manager;
        unit.SetParent(this);
        _units = new List<Unit>() { unit };
    }

    public Group(Group a, Group b)
    {
        _manager = a._manager;

        _units = new List<Unit>();
        _units.AddRange(a._units);
        _units.AddRange(b._units);
        foreach (Unit unit in _units)
            unit.SetParent(this);
    }

    public bool Contains(int x, int y)
    {
        foreach (Unit unit in _units)
            if (unit.GetX() == x && unit.GetY() == y)
                return true;
        return false;
    }

    public bool Neighbours(Group group)
    {
        foreach (Unit unit in _units)
        {
            int x = unit.GetX();
            int y = unit.GetY();
            if (group.Contains(x + 1, y) || group.Contains(x - 1, y) || group.Contains(x, y + 1) || group.Contains(x, y - 1))
                return true;
        }
        return false;
    }

    public Unit FirstCell()
    {
        Unit first = _units[0];
        foreach (Unit unit in _units)
            if (unit.GetY() < first.GetY() || (unit.GetY() == first.GetY() && unit.GetX() < first.GetX()))
                first = unit;
        return first;
    }

    public bool SoonerThan(Group group)
    {
        Unit first = FirstCell();
        Unit compare = group.FirstCell();
        return first.GetY() < compare.GetY() || (first.GetY() == compare.GetY() && first.GetX() < compare.GetX());
    }

    public void Kill()
    {
        Color matColor = new Color(0, 0, 0, 0.125f);

        foreach (Unit unit in _units)
        {
            unit.SetColor(matColor);
            unit.SetParent(null);
        }
    }

    public void Reset()
    {
        _isSelected = false;
        _isRegrouped = false;
        Highlight(_isHighlighted);
    }

    public void Regroup()
    {
        _isRegrouped = true;
        Highlight(true);
    }

    public void Press()
    {
        _manager.Display(null);
        _manager.ToggleSelect();
        if (_manager.IsSelecting())
            Highlight(true);
    }

    public void Highlight(bool isActive)
    {
        _isHighlighted = isActive;

        if (_manager.IsSelecting())
        {
            _isSelected = true;
            _manager.SelectGroup(this);
        }
        if (_isSelected)
        {
            Color matColor = new Color(_isRegrouped ? 0 : 1, _isRegrouped ? 0 : 1, _isRegrouped ? 0 : 1, _isRegrouped ? 0.125f : 1);
            foreach (Unit unit in _units)
                unit.SetColor(matColor);
        }
        else
        {
            Color matColor = new Color(_color[0] / 3f, _color[1] / 3f, _color[2] / 3f, isActive ? 1 : 0.75f);
            foreach (Unit unit in _units)
                unit.SetColor(matColor);

            _manager.Display(isActive ? this : null);
        }
    }

    public void SetColor(int r, int g, int b)
    {
        _color = new int[] { r, g, b };
        Color matColor = new Color(r / 3f, g / 3f, b / 3f, 0.75f);
        foreach (Unit unit in _units)
            unit.SetColor(matColor);
    }

    public int GetSize()
    {
        return _units.Count;
    }
}
