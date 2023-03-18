using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class GroupManager {
    private PartitionsScript _module;

    private List<Group> _groups;
    private List<int> _groupValues;

    private List<Group> _selectedGroups;
    private List<int> _mergeSizes;

    private List<int> _targetSizes;

    private bool _isSelecting = false;

    public GroupManager(PartitionsScript module)
    {
        _module = module;
        _groups = new List<Group>();
        _selectedGroups = new List<Group>();
        _mergeSizes = new List<int>();
    }

    public void AddGroup(Group group)
    {
        _groups.Add(group);
    }

    public void Crush(int count)
    {
        while (_groups.Count > count)
            CrushSingle();
        SetValues();
    }

    public void SetValues()
    {
        _groupValues = new List<int>();
        while (_groupValues.Count < _groups.Count)
            _groupValues.Add((int)(_groups.Count - Mathf.Sqrt(Rnd.Range(1, _groups.Count * _groups.Count))));

        //excluding white for highlight
        List<int> colorValues = Enumerable.Range(0, 63).ToList().Shuffle();
        int i = 0;
        foreach (Group group in _groups)
        {
            group.SetColor((colorValues[i] / 16) % 4, (colorValues[i] / 4) % 4, (colorValues[i] / 1) % 4);
            group.Reset();
            i++;
        }

        _module.Log(String.Format("Group values in order are: {0}.", _groupValues.Join(", ")));

        CalculateSolution();
    }

    private void CrushSingle()
    {
        int minSize = _groups[0].GetSize();
        List<Group> minimal = new List<Group>();
        foreach (Group group in _groups)
        {
            if (group.GetSize() == minSize)
                minimal.Add(group);
            else if (group.GetSize() < minSize)
                minimal = new List<Group>() { group };
        }
        Group mergeable = minimal.PickRandom();

        List<Group> neighbours = new List<Group>();
        foreach (Group group in _groups)
        {
            if (group.Neighbours(mergeable))
                neighbours.Add(group);
        }
        Group neighbour = neighbours.PickRandom();

        _groups.Remove(mergeable);
        _groups.Remove(neighbour);
        _groups.Add(new Group(mergeable, neighbour));
    }

    public void ToggleSelect()
    {
        _module.PlaySound("Select");

        _isSelecting = !_isSelecting;
        if (_isSelecting)
            _mergeSizes.Add(0);
        else if (_selectedGroups.Count == _groups.Count)
            HandleCompletion();
        else
            foreach (Group group in _selectedGroups)
                group.Regroup();
    }

    public void SelectGroup(Group group)
    {
        if (_selectedGroups.Contains(group))
            return;

        _selectedGroups.Add(group);
        _mergeSizes[_mergeSizes.Count - 1]++;
    }

    public bool IsSelecting()
    {
        return _isSelecting;
    }

    private void HandleCompletion()
    {

        if (ValidateSolution())
        {
            _module.Log(String.Format("Your entries were: {0}. This is correct!", _mergeSizes.Join(", ")));

            _groups = new List<Group>();
            Group baseGroup = null;
            int mergeCount = 0;
            foreach (Group group in _selectedGroups)
            {
                if (baseGroup == null)
                    baseGroup = group;
                else
                    baseGroup = new Group(baseGroup, group);
                if (++mergeCount == _mergeSizes[_groups.Count])
                {
                    _groups.Add(baseGroup);
                    mergeCount = 0;
                    baseGroup = null;
                }
            }

            _selectedGroups = new List<Group>();
            _mergeSizes = new List<int>();

            if (_groups.Count == 1)
            {
                _groups[0].Kill();
                _module.Pass();
            }
            else
                SetValues();
        }
        else
        {
            _module.Log(String.Format("Your entries were: {0}. This is incorrect!", _mergeSizes.Join(", ")));

            foreach (Group group in _groups)
                group.Reset();
            _selectedGroups = new List<Group>();
            _mergeSizes = new List<int>();
            _module.Strike();
        }
    }

    private void CalculateSolution()
    {
        _targetSizes = new List<int>();
        List<int> remainingValues = new List<int>(_groupValues);
        while (_targetSizes.Sum() < _groups.Count)
        {
            int maxSize = _groups.Count - _targetSizes.Sum();
            int index = remainingValues.Sum() % remainingValues.Count();
            int value = ReductiveMod(remainingValues[index], maxSize) + 1;
            _targetSizes.Add(value);
            remainingValues.RemoveAt(index);
        }

        _module.Log(String.Format("Calculated group sizes, in order of calculation are: {0}.", _targetSizes.Join(", ")));
    }

    private int ReductiveMod(int a, int b)
    {
        if (b <= 0)
            return 0;
        if (a < b)
            return a;
        return ReductiveMod(a - b, b - 1);
    }

    private bool ValidateSolution()
    {
        List<int> merges = _mergeSizes.OrderBy(x => x).ToList();
        List<int> target = _targetSizes.OrderBy(x => x).ToList();

        if (merges.Count != target.Count)
            return false;
        else
            for (int i = 0; i < merges.Count; i++)
                if (merges[i] != target[i])
                    return false;

        return true;
    }

    private int GetIndex(Group group)
    {
        int index = 0;
        foreach (Group compare in _groups)
            if (compare.SoonerThan(group))
                index++;
        return index;
    }

    public void Display(Group group)
    {
        if (group == null)
            _module.Display("");
        else
        {
            int index = GetIndex(group);
            _module.Display((index + 1).ToString("00") + "/" + _groups.Count.ToString("00") + ":" + _groupValues[index].ToString("00"));
        }
    }

    public List<Group> GetGroups()
    {
        return _groups.OrderBy(x => GetIndex(x)).ToList();
    }

    public List<Group> GetSelectedGroups()
    {
        return _selectedGroups.OrderBy(x => GetIndex(x)).ToList();
    }

    public List<int> GetTargetValues()
    {
        return _targetSizes.OrderByDescending(x => x).ToList();
    }

    public void Clear()
    {
        _isSelecting = false;

        foreach (Group group in _groups)
            group.Reset();

        _selectedGroups = new List<Group>();
        _mergeSizes = new List<int>();
    }
}