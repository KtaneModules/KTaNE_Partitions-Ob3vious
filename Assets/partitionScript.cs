using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class partitionScript : MonoBehaviour {

	public KMAudio Audio;
	public KMBombModule Module;
	public KMSelectable[] Pixel;
	public TextMesh Text;

	private List<int[]> colour = new List<int[]> { };
	private List<List<int>> Groups = new List<List<int>> { };
	private List<List<int>> TempGroups = new List<List<int>> { };
	private List<int> TempGroups2 = new List<int> { };
	private List<int> targetGrouping = new List<int> { };
	private List<int> selectedGroups = new List<int> { };
	private bool selecting = false;
	private bool unsolved = true;

	private static int _moduleIdCounter = 1;
	private int _moduleId;

	void Awake () {
		_moduleId = _moduleIdCounter++;
		for (int i = 0; i < Pixel.Length; i++)
		{
			int x = i;
			Pixel[x].OnInteract += delegate
			{
				if (unsolved)
				{
					Audio.PlaySoundAtTransform("Select", Module.transform);
					if (selecting)
					{
						TempGroups2.Add(0);
						List<int> TempSet = new List<int> { };
                        foreach (int group in selectedGroups)
                        {
							TempSet = TempSet.Concat(Groups[group]).ToList();
							TempGroups2[TempGroups2.Count() - 1]++;
						}
						TempGroups.Add(TempSet);
						foreach (int pixel in TempGroups.Last())
							Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.375f, 0.375f, 0.375f);
						selectedGroups = new List<int> { };
						selecting = false;
						if (TempGroups.Sum(y => y.Count()) == Pixel.Length)
							Reorganise();
					}
					else if (!TempGroups.Any(y => y.Contains(x)))
					{
						int groupbit = Enumerable.Range(0, Groups.Count()).First(y => Groups[y].Contains(x));
						foreach (int pixel in Groups[groupbit])
							Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.125f, 0.125f, 0.125f);
						selectedGroups.Add(groupbit);
						selecting = true;
						Text.text = string.Empty;
					}
				}
				return false;
			};
			Pixel[x].OnHighlight += delegate
			{
				if (unsolved && !TempGroups.Any(y => y.Contains(x)))
				{
					int groupbit = Enumerable.Range(0, Groups.Count()).First(y => Groups[y].Contains(x));
					if (!selecting)
					{
						foreach (int pixel in Groups[groupbit])
							Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.875f, 0.875f, 0.875f);
						Text.text = colour[groupbit].Join("") + " - " + (groupbit + 1).ToString();
					}
					else if (!selectedGroups.Contains(groupbit))
					{
						selectedGroups.Add(groupbit);
						foreach (int pixel in Groups[groupbit])
							Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.125f, 0.125f, 0.125f);
					}
				}
			};
			Pixel[x].OnHighlightEnded += delegate
			{
				if (unsolved && !selecting && !TempGroups.Any(y => y.Contains(x)))
				{
					int groupbit = Enumerable.Range(0, Groups.Count()).First(y => Groups[y].Contains(x));
					foreach (int pixel in Groups[groupbit])
						Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(colour[groupbit][0] / 4f, colour[groupbit][1] / 4f, colour[groupbit][2] / 4f);
					Text.text = string.Empty;
				}
			};
		}
	}

	void Start()
	{
		Text.text = string.Empty;
		for (int i = 0; i < Pixel.Length; i++)
		{
			Groups.Add(new List<int> { i });
			colour.Add(new int[] { Rnd.Range(0, 5), Rnd.Range(0, 5), Rnd.Range(0, 5) });
			Pixel[i].GetComponent<MeshRenderer>().material.color = new Color(colour[i][0] / 4f, colour[i][1] / 4f, colour[i][2] / 4f);
		}
		CalcGrouping();
	}

	void CalcGrouping()
	{
		Debug.LogFormat("[Partitions #{0}] The current group colours are {1}.", _moduleId, colour.Select(x => x.Join("")).Join(", "));
		targetGrouping = new List<int> { };
		int togroup = Groups.Count();
		List<int> stuffleft = new List<int> { };
		for (int i = 0; i < Groups.Count(); i++)
			stuffleft.Add(colour[i].Sum());
		while (togroup > 0)
		{
			int number = stuffleft.Sum();
			Debug.LogFormat("[Partitions #{0}] The total number is currently {1}. Inspecting grouplet {2} adds a group of {3}.", _moduleId, number, number % stuffleft.Count() + 1, ReducMod(stuffleft[number % stuffleft.Count()], togroup) + 1);
			targetGrouping.Add(ReducMod(stuffleft[number % stuffleft.Count()], togroup) + 1);
			togroup -= targetGrouping.Last();
			stuffleft.RemoveAt(number % stuffleft.Count());
		}
		targetGrouping = targetGrouping.OrderBy(x => x).ToList();
		if (!(targetGrouping.Last() == 1))
			Debug.LogFormat("[Partitions #{0}] The expected grouping is [{1}].", _moduleId, targetGrouping.Join(", "));
		else if (targetGrouping.Count() == 1)
			Debug.LogFormat("[Partitions #{0}] Oh wait! the module is solved.", _moduleId);
		else
		{
			targetGrouping = targetGrouping.Take(targetGrouping.Count() - 2).Concat(new int[] { 2 }).ToList();
			Debug.LogFormat("[Partitions #{0}] The expected grouping is [{1}].", _moduleId, targetGrouping.Join(", "));
		}
	}

	void Reorganise()
	{
		TempGroups2 = TempGroups2.OrderBy(x => x).ToList();
		bool proceed = true;
		for (int i = 0; i < TempGroups2.Count() && i < targetGrouping.Count(); i++)
			if (TempGroups2[i] != targetGrouping[i])
				proceed = false;
		if (proceed)
		{
			Debug.LogFormat("[Partitions #{0}] Your grouping is correct!", _moduleId);
			Groups = TempGroups.OrderBy(x => x.Min()).ToList();
		}
		else
		{
			Debug.LogFormat("[Partitions #{0}] Your grouping of [{1}] does not match the expected grouping of [{2}].", _moduleId, TempGroups2.Join(", "), targetGrouping.Join(", "));
			Module.HandleStrike();
			for (int i = 0; i < Groups.Count(); i++)
				foreach (int pixel in Groups[i])
					Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(colour[i][0] / 4f, colour[i][1] / 4f, colour[i][2] / 4f);
		}
		TempGroups = new List<List<int>> { };
		TempGroups2 = new List<int> { };
		if (Groups.Count() == 1)
		{
			Module.HandlePass();
			unsolved = false;
			Audio.PlaySoundAtTransform("Solve", Module.transform);
		}
		if (proceed)
		{
			Regroup();
			CalcGrouping();
		}
	}

	void Regroup()
	{
		colour = new List<int[]> { };
		for (int i = 0; i < Groups.Count(); i++)
		{
			colour.Add(new int[] { Rnd.Range(0, 5), Rnd.Range(0, 5), Rnd.Range(0, 5) });
			foreach (int pixel in Groups[i])
				Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(colour[i][0] / 4f, colour[i][1] / 4f, colour[i][2] / 4f);
		}
	}

	int ReducMod(int a, int b)
	{
		for (int i = b; i > 0; i--)
			if (a >= i)
				a -= i;
			else
				return a;
		return 0;
	}

	IEnumerator TwitchHighlight(int pos)
    {
		Pixel[pos].OnHighlight();
		for (float t = 0f; t < 1; t += Time.deltaTime)
			yield return null;
		Pixel[pos].OnHighlightEnded();
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} highlight A1 B1 B2' to highlight those pixels for 1 second each. '!{0} select A1 B1' to group the groups of those pixels.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		string[] validcmds = { "highlight", "select" };
		string[] validcoords = { "a1", "b1", "c1", "d1", "e1", "a2", "b2", "c2", "d2", "e2", "a3", "b3", "c3", "d3", "e3", "a4", "b4", "c4", "d4", "e4", "a5", "b5", "c5", "d5", "e5" };
		command = command.ToLowerInvariant();
		string[] cmds = command.Split(' ');
		if (cmds.Length < 2 || !validcmds.Contains(cmds[0]))
		{
			yield return "sendtochaterror Invalid command.";
			yield break;
		}
		for (int i = 1; i < cmds.Length; i++)
			if (!validcoords.Contains(cmds[i]))
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
		for (int i = 1; i < cmds.Length; i++)
			for (int j = 0; j < validcoords.Length; j++)
				if (validcoords[j] == cmds[i])
				{
					if (cmds[0] == validcmds[0])
					{
						StartCoroutine(TwitchHighlight(j));
						for (float t = 0f; t < 1; t += Time.deltaTime)
							yield return "trycancel Highlighting tiles has been stopped.";
					}
					else
					{
						Pixel[j].OnHighlight();
						if (i == 1)
						{
							Pixel[j].OnInteract();
							yield return null;
						}
                        if (i == cmds.Length - 1)
                        {
							Pixel[j].OnInteract();
							yield return null;
						}
						Pixel[j].OnHighlightEnded();
					}
					
				}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return true;
		selectedGroups = new List<int> { };
		TempGroups = new List<List<int>> { };
		TempGroups2 = new List<int> { };
		while (unsolved)
		{
			for (int i = 0; i < targetGrouping.Count(); i++)
			{
				Pixel[Enumerable.Range(0, 25).First(x => !TempGroups.Any(y => y.Contains(x)))].OnInteract();
				yield return null;
				for (int j = Enumerable.Range(0, 25).First(x => !TempGroups.Any(y => y.Contains(x))); selectedGroups.Count() < targetGrouping[i]; j++)
				{
					Pixel[j].OnHighlight();
					yield return null;
				}
				yield return null;
				Pixel[Groups[selectedGroups[0]][0]].OnInteract();
				yield return null;
			}
			yield return null;
		}
	}
}
