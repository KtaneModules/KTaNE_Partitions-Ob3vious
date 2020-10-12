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

	private int[,] colour = new int[25, 3];
	private int[][] Groups = new int[25][];
	private int[][] TempGroups = new int[0][];
	private int[] TempGroups2 = { };
	private int[] targetGrouping = new int[0];
	private int[] selectedGroups = new int[0];
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
					if (!selecting)
					{
						bool proceed = true;
						for (int j = 0; j < TempGroups.Length; j++)
						{
							if (TempGroups[j].Contains(x))
							{
								proceed = false;
							}
						}
						if (proceed)
						{
							int groupbit = 0;
							for (int j = 0; j < Groups.Length; j++)
							{
								if (Groups[j].Contains(x))
								{
									groupbit = j;
								}
							}
							foreach (var pixel in Groups[groupbit])
							{
								Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.125f, 0.125f, 0.125f);
							}
							selectedGroups = selectedGroups.Concat(new int[] { groupbit }).ToArray();
							selecting = true;
							Text.text = string.Empty;
						}
					}
					else
					{
						TempGroups = TempGroups.Concat(new int[][] { new int[] { } }).ToArray();
						TempGroups2 = TempGroups2.Concat(new int[] { 0 }).ToArray();
						for (int j = 0; j < selectedGroups.Length; j++)
						{
							TempGroups[TempGroups.Length - 1] = TempGroups[TempGroups.Length - 1].Concat(Groups[selectedGroups[j]]).ToArray();
							TempGroups2[TempGroups2.Length - 1]++;
						}
						foreach (var pixel in TempGroups[TempGroups.Length - 1])
						{
							Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.375f, 0.375f, 0.375f);
						}
						selectedGroups = new int[0];
						selecting = false;
						int proceed = 0;
						for (int j = 0; j < TempGroups.Length; j++)
						{
							proceed += TempGroups[j].Length;
						}
						if (proceed == Pixel.Length)
						{
							Reorganise();
						}
					}
				}
				return false;
			};
			Pixel[x].OnHighlight += delegate
			{
				if (unsolved)
				{
					int groupbit = 0;
					for (int j = 0; j < Groups.Length; j++)
					{
						if (Groups[j].Contains(x))
						{
							groupbit = j;
						}
					}
					if (selecting)
					{
						bool proceed = true;
						for (int j = 0; j < TempGroups.Length; j++)
						{
							if (TempGroups[j].Contains(x))
							{
								proceed = false;
							}
						}
						if (proceed)
						{
							if (!selectedGroups.Contains(groupbit))
							{
								selectedGroups = selectedGroups.Concat(new int[] { groupbit }).ToArray();
								foreach (var pixel in Groups[groupbit])
								{
									Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.125f, 0.125f, 0.125f);
								}
							}
						}
					}
					else
					{
						bool proceed = true;
						for (int j = 0; j < TempGroups.Length; j++)
						{
							if (TempGroups[j].Contains(x))
							{
								proceed = false;
							}
						}
						if (proceed)
						{
							foreach (var pixel in Groups[groupbit])
							{
								Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(0.875f, 0.875f, 0.875f);
							}
							Text.text = colour[groupbit, 0].ToString() + colour[groupbit, 1].ToString() + colour[groupbit, 2].ToString() + " - " + (groupbit + 1).ToString();
						}
					}
				}
			};
			Pixel[x].OnHighlightEnded += delegate
			{
				if (unsolved)
				{
					if (!selecting)
					{
						bool proceed = true;
						for (int j = 0; j < TempGroups.Length; j++)
						{
							if (TempGroups[j].Contains(x))
							{
								proceed = false;
							}
						}
						if (proceed)
						{
							int groupbit = 0;
							for (int j = 0; j < Groups.Length; j++)
							{
								if (Groups[j].Contains(x))
								{
									groupbit = j;
								}
							}
							foreach (var pixel in Groups[groupbit])
							{
								Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(colour[groupbit, 0] / 4f, colour[groupbit, 1] / 4f, colour[groupbit, 2] / 4f);
							}
							Text.text = string.Empty;
						}
					}
				}
			};
		}
	}

	void Start()
	{
		Text.text = string.Empty;
		for (int i = 0; i < Pixel.Length; i++)
		{
			Groups[i] = new int[] { i };
			for (int j = 0; j < 3; j++)
			{
				colour[i, j] = Rnd.Range(0, 5);
			}
			Pixel[i].GetComponent<MeshRenderer>().material.color = new Color(colour[i, 0] / 4f, colour[i, 1] / 4f, colour[i, 2] / 4f);
		}
		CalcGrouping();
	}

	void CalcGrouping()
	{
		targetGrouping = new int[0];
		int togroup = Groups.Length;
		int[] stuffleft = new int[0];
		for (int i = 0; i < Groups.Length; i++)
		{
			stuffleft = stuffleft.Concat(new int[] { colour[i, 0] + colour[i, 1] + colour[i, 2] }).ToArray();
		}
		while (togroup > 0)
		{
			int number = 0;
			for (int i = 0; i < stuffleft.Length; i++)
			{
				number += stuffleft[i];
			}
			Debug.LogFormat("[Partitions #{0}] The total number is currently {1}. Inspecting grouplet {2} adds a group of {3}.", _moduleId, number, number % stuffleft.Length + 1, ReducMod(stuffleft[number % stuffleft.Length], togroup) + 1);
			targetGrouping = targetGrouping.Concat(new int[] { ReducMod(stuffleft[number % stuffleft.Length], togroup) + 1 }).ToArray();
			togroup -= targetGrouping.Last();
			stuffleft = stuffleft.Take(number % stuffleft.Length).ToArray().Concat(stuffleft.Skip(number % stuffleft.Length + 1).ToArray()).ToArray();
		}
		targetGrouping = targetGrouping.OrderBy(x => x).ToArray();
		if (!(targetGrouping.Last() == 1))
		{
			Debug.LogFormat("[Partitions #{0}] The expected grouping is [{1}].", _moduleId, targetGrouping.Join(", "));
		}
		else
		{
			if (targetGrouping.Length == 1)
			{
				Debug.LogFormat("[Partitions #{0}] Oh wait! the module is solved.", _moduleId);
			}
			else
			{
				targetGrouping = targetGrouping.Take(targetGrouping.Length - 2).Concat(new int[] { 2 }).ToArray();
				Debug.LogFormat("[Partitions #{0}] The expected grouping is [{1}].", _moduleId, targetGrouping.Join(", "));
			}
		}
	}

	void Reorganise()
	{
		TempGroups2 = TempGroups2.OrderBy(x => x).ToArray();
		bool proceed = true;
		for (int i = 0; i < TempGroups2.Length && i < targetGrouping.Length; i++)
		{
			if (TempGroups2[i] != targetGrouping[i])
			{
				proceed = false;
			}
		}
		if (proceed)
		{
			Debug.LogFormat("[Partitions #{0}] Your grouping is correct!", _moduleId);
			Groups = TempGroups.OrderBy(x => x.Min()).ToArray();
		}
		else
		{
			Debug.LogFormat("[Partitions #{0}] Your grouping of [{1}] does not match the expected grouping of [{2}].", _moduleId, TempGroups2.Join(", "), targetGrouping.Join(", "));
			Module.HandleStrike();
			for (int i = 0; i < Groups.Length; i++)
			{
				foreach (var pixel in Groups[i])
				{
					Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(colour[i, 0] / 4f, colour[i, 1] / 4f, colour[i, 2] / 4f);
				}
			}
		}
		TempGroups = new int[0][];
		TempGroups2 = new int[] { };
		if (Groups.Length == 1)
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
		colour = new int[Groups.Length, 3];
		for (int i = 0; i < Groups.Length; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				colour[i, j] = Rnd.Range(0, 5);
			}
			foreach (var pixel in Groups[i])
			{
				Pixel[pixel].GetComponent<MeshRenderer>().material.color = new Color(colour[i, 0] / 4f, colour[i, 1] / 4f, colour[i, 2] / 4f);
			}
		}
	}

	int ReducMod(int a, int b)
	{
		int x = a;
		for (int i = b; i > 0; i--)
		{
			if (x >= i)
				x -= i;
			else
				return x;
		}
		return 0;
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
		{
			if (!validcoords.Contains(cmds[i]))
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
		}
		for (int i = 1; i < cmds.Length; i++)
		{
			for (int j = 0; j < validcoords.Length; j++)
			{
				if (validcoords[j] == cmds[i])
				{
					Pixel[j].OnHighlight();
					if (cmds[0] == validcmds[0])
					{
						yield return new WaitForSeconds(1f);
					}
					else
					{
						yield return null;
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
					}
					Pixel[j].OnHighlightEnded();
				}
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return true;
		int[] numbers = new int[25];
		for (int i = 0; i < numbers.Length; i++)
		{
			numbers[i] = i;
		}
		selectedGroups = new int[0];
		TempGroups = new int[0][];
		TempGroups2 = new int[] { };
		while (unsolved)
		{
			for (int i = 0; i < targetGrouping.Length; i++)
			{
				Pixel[numbers.Where(x => !TempGroups.Any(y => y.Contains(x))).Min()].OnInteract();
				yield return null;
				for (int j = numbers.Where(x => !TempGroups.Any(y => y.Contains(x))).Min(); selectedGroups.Length < targetGrouping[i]; j++)
				{
					Pixel[j].OnHighlight();
					yield return null;
				}
				yield return null;
				Pixel[Groups[selectedGroups[0]][0]].OnInteract();
				yield return true;
			}
			yield return true;
		}
	}
}
