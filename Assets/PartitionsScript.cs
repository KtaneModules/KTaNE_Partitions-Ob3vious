using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class PartitionsScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMSelectable Pixel;
    public TextMesh Text;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private GroupManager _manager;

    void Awake()
    {
        _moduleId = _moduleIdCounter++;
    }

    //j
    void Start()
    {
        _manager = GenerateTiles(14, 10);
        _manager.Crush(10);
    }

    private GroupManager GenerateTiles(int w, int h)
    {
        Transform parent = Pixel.transform.parent;

        KMSelectable[,] tiles = new KMSelectable[h, w];
        Unit[,] units = new Unit[h, w];
        GroupManager manager = new GroupManager(this);

        List<KMSelectable> selectables = new List<KMSelectable>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                tiles[y, x] = Instantiate(Pixel, parent);
                tiles[y, x].transform.localPosition = new Vector3(x, -y);
                units[y, x] = new Unit(tiles[y, x], x, y);
                Group group = new Group(units[y, x], manager);
                group.SetColor(Rnd.Range(0, 4), Rnd.Range(0, 4), Rnd.Range(0, 4));
                manager.AddGroup(group);

                selectables.Add(tiles[y, x]);
            }
        }

        Module.GetComponent<KMSelectable>().Children = selectables.ToArray();
        Module.GetComponent<KMSelectable>().UpdateChildren();
        Pixel.transform.localScale = new Vector3(0, 0, 0);

        return manager;
    }

    public void Display(string text)
    {
        Text.text = text;
    }

    public void PlaySound(string sound)
    {
        Audio.PlaySoundAtTransform(sound, Module.transform);
    }

    public void Pass()
    {
        Module.HandlePass();
        PlaySound("Solve");
        Log("Module passed!");
    }

    public void Strike()
    {
        Module.HandleStrike();
    }

    public void Log(string message)
    {
        Debug.LogFormat("[Partitions #{0}] {1}", _moduleId, message);
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} inspect 3 8 1' to highlight those groups for 1 second each. '!{0} select 11 12 13' to group those groups together.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (Regex.IsMatch(command, @"^(inspect|select)(\s\d{1,2})+$"))
        {
            bool selecting = false;
            int lastIndex = -1;

            MatchCollection matches = Regex.Matches(command, @"\b\d{1,2}\b");

            foreach (Match match in matches)
                foreach (Capture capture in match.Captures)
                {
                    int index = int.Parse(capture.ToString()) - 1;
                    if (index >= _manager.GetGroups().Count || index == -1)
                        yield return "sendtochaterror Invalid command.";
                }

            foreach (Match match in matches)
                foreach (Capture capture in match.Captures)
                {
                    int index = int.Parse(capture.ToString()) - 1;
                    if (command.Split(' ')[0] == "inspect")
                    {
                        _manager.GetGroups()[index].FirstCell().GetSelectable().OnHighlight();
                        for (float t = 0f; t < 1; t += Time.deltaTime)
                            yield return "trycancel Highlighting groups has been stopped.";
                        _manager.GetGroups()[index].FirstCell().GetSelectable().OnHighlightEnded();
                    }
                    else
                    {
                        lastIndex = index;
                        if (!selecting)
                            _manager.GetGroups()[index].FirstCell().GetSelectable().OnInteract();
                        else
                            _manager.GetGroups()[index].FirstCell().GetSelectable().OnHighlight();
                        selecting = true;
                        yield return new WaitForSeconds(0.1f);
                        _manager.GetGroups()[index].FirstCell().GetSelectable().OnHighlightEnded();
                    }
                }
            if (lastIndex != -1)
            {
                KMSelectable select = _manager.GetGroups()[lastIndex].FirstCell().GetSelectable();
                select.OnInteract();
                select.OnHighlightEnded();
            }
        }
        else
            yield return "sendtochaterror Invalid command.";
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        _manager.Clear();
        while (_manager.GetGroups().Count != 1)
            for (int i = 0; i < _manager.GetTargetValues().Count; i++)
            {
                List<Group> availableGroups = _manager.GetGroups().Except(_manager.GetSelectedGroups()).ToList();

                int minSize = availableGroups[0].GetSize();
                List<Group> minimal = new List<Group>();
                foreach (Group group in availableGroups)
                {
                    if (group.GetSize() == minSize)
                        minimal.Add(group);
                    else if (group.GetSize() < minSize)
                        minimal = new List<Group>() { group };
                }
                List<Group> mergeable = new List<Group>() { minimal.PickRandom() };

                int size = 1;

                while (size < _manager.GetTargetValues()[i])
                {
                    List<Group> neighbours = new List<Group>();
                    foreach (Group group in availableGroups.Except(mergeable))
                    {
                        if (mergeable.Any(x => group.Neighbours(x)))
                            neighbours.Add(group);
                    }

                    List<Group> minimal2 = new List<Group>();
                    if (neighbours.Count != 0)
                    {
                        minSize = neighbours[0].GetSize();
                        foreach (Group group in neighbours)
                        {
                            if (group.GetSize() == minSize)
                                minimal2.Add(group);
                            else if (group.GetSize() < minSize)
                                minimal2 = new List<Group>() { group };
                        }
                    }

                    if (minimal2.Count == 0)
                    {
                        minSize = availableGroups.Except(mergeable).ToList()[0].GetSize();
                        foreach (Group group in availableGroups.Except(mergeable))
                        {
                            if (group.GetSize() == minSize)
                                minimal2.Add(group);
                            else if (group.GetSize() < minSize)
                                minimal2 = new List<Group>() { group };
                        }
                    }
                    mergeable.Add(minimal2.PickRandom());
                    size++;
                }

                KMSelectable cell = mergeable[0].FirstCell().GetSelectable();

                cell.OnInteract();
                foreach (Group group in mergeable)
                {
                    group.FirstCell().GetSelectable().OnHighlight();
                    yield return new WaitForSeconds(0.05f);
                    group.FirstCell().GetSelectable().OnHighlightEnded();
                }
                cell.OnInteract();
                cell.OnHighlightEnded();
                yield return new WaitForSeconds(0.1f);
            }
    }
}
