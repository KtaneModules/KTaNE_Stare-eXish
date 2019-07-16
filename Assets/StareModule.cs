﻿using KMHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class StareModule : MonoBehaviour
{
    public KMBombInfo bombInfo;
    public KMBombModule moduleInfo;
    public KMSelectable module;
    public KMColorblindMode Colorblind;
    public SpriteRenderer sprite;
    public MeshRenderer model;
    public GameObject cblind;

    public Sprite[] eyes = new Sprite[6];
    public Material[] materials = new Material[3];
    public Color[] colors = new Color[] {
        new Color(0.875f, 0.0f, 0.0f),
        new Color(0.5f, 0.0f, 0.0f),
        new Color(0.625f, 0.5f, 0.0f),
        new Color(1.0f, 0.875f, 0.0f),
        new Color(0.0f, 0.625f, 0.0f),
        new Color(0.0f, 0.625f, 0.5f),
        new Color(0.5f, 0.25f, 0.625f),
        new Color(0.625f, 0.625f, 0.625f),
        new Color(0.9375f, 0.9375f, 0.9375f)
    };
    private string colorName;

    private int color;
    private int type;
    private int modifier;

    private string moduleName = "";

    private List<int> times;

    private bool almostDone = false;
    private bool isSolved = false;

    private int initialTime = 0;

    private bool colorblindActive = false;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    enum State { Open = 0, Closed };
    enum Type { Normal = 0, Alt, Smol };
    enum Mod { Normal = 0, Warp, Rift };

    private State initialState;

    void Start()
    {
        colorblindActive = Colorblind.ColorblindModeActive;
        cblind.GetComponent<TextMesh>().text = "";
        initialTime = (int)bombInfo.GetTime();
        _moduleId = _moduleIdCounter++;
        color = UnityEngine.Random.Range(0, colors.Length);
        type = UnityEngine.Random.Range(0, 7);
        type = (type > 4) ? 2 : (type > 2) ? 1 : 0;
        modifier = UnityEngine.Random.Range(0, 9);
        modifier = (modifier > 6) ? 2 : (modifier > 4) ? 1 : 0;
        moduleName = (color + 1).ToString() + (type + 1 + (modifier * 3)).ToString() + "XO";
        List<string> names = bombInfo.GetModuleNames();
        List<string> stares = new List<string>();
        Regex stareRegex = new Regex(@"[1-9][1-9][XOC][OC]");
        foreach (string name in names)
        {
            Match match = stareRegex.Match(name);
            if (match.Success)
            {
                stares.Add(name);
            }
        }
        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void ActivateModule()
    {
        Init();
        SetDesiredState();
    }

    void Init()
    {
        int rand = UnityEngine.Random.Range(0, 2);
        if(rand == 0)
        {
            sprite.sprite = eyes[(type * 2) + 1];
            initialState = State.Closed;
        }
        else
        {
            sprite.sprite = eyes[type * 2];
            initialState = State.Open;
        }
        model.material = materials[modifier];
        sprite.color = colors[color];
        model.material.color = colors[color];
        module.OnInteract += delegate () { OnPress(); return false; };
        GetComponent<KMBombModule>().OnPass += delegate () { isSolved = true; /*sprite.sprite = eyes[type * 2 + 1];*/ return true; };
    }

    void SetDesiredState()
    {
        List<string> names = bombInfo.GetModuleNames();
        List<string> stares = new List<string>();
        Regex stareRegex = new Regex(@"[1-9][1-9][XOC][OC]");
        foreach (string name in names)
        {
            Match match = stareRegex.Match(name);
            if (match.Success)
            {
                stares.Add(name);
            }
        }
        char desiredState = state(moduleInfo.ModuleDisplayName, stares);
        if(initialState == State.Closed)
        {
            moduleName = moduleName.Substring(0, 2) + 'C' + desiredState;
        }
        else
        {
            moduleName = moduleName.Substring(0, 2) + 'O' + desiredState;
        }
        string typeName = "", modName = "";
        colorName = "";
        switch (color)
        {
            case 0: colorName = "Red"; cblind.GetComponent<TextMesh>().color = colors[0]; break;
            case 1: colorName = "Burgundy"; cblind.GetComponent<TextMesh>().color = colors[1]; break;
            case 2: colorName = "Gold"; cblind.GetComponent<TextMesh>().color = colors[2]; break;
            case 3: colorName = "Yellow"; cblind.GetComponent<TextMesh>().color = colors[3]; break;
            case 4: colorName = "Green"; cblind.GetComponent<TextMesh>().color = colors[4]; break;
            case 5: colorName = "Turquoise"; cblind.GetComponent<TextMesh>().color = colors[5]; break;
            case 6: colorName = "Purple"; cblind.GetComponent<TextMesh>().color = colors[6]; break;
            case 7: colorName = "Gray"; cblind.GetComponent<TextMesh>().color = colors[7]; break;
            case 8: colorName = "White"; cblind.GetComponent<TextMesh>().color = colors[8]; break;
        }
        if (colorblindActive)
        {
            Debug.LogFormat("[The Stare #{0}] Colorblind mode is active!", _moduleId);
            cblind.GetComponent<TextMesh>().text = colorName.Substring(0,3);
        }
        switch (type)
        {
            case 0: typeName = "Normal"; break;
            case 1: typeName = "Special"; break;
            case 2: typeName = "Small"; break;
        }
        switch (modifier)
        {
            case 0: modName = "Plain"; break;
            case 1: modName = "Warped"; break;
            case 2: modName = "Rifted"; break;
        }
        Debug.LogFormat("[The Stare #{0}] This is a " + typeName + " " + modName + " " + colorName + " Eye. It needs to be {1}.", _moduleId, (desiredState == 'C' ? "closed" : "opened"));
    }

    void OnPress()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        module.AddInteractionPunch();
        if (!isSolved)
        {
            if (almostDone)
            {
                Debug.LogFormat("[The Stare #{0}] Module solved.", _moduleId);
                moduleInfo.ModuleDisplayName = "The Stare";
                moduleInfo.HandlePass();
                isSolved = true;
                return;
            }
            List<string> names = bombInfo.GetModuleNames();
            List<string> stares = new List<string>();
            Regex stareRegex = new Regex(@"[1-9][1-9][XOC][OC]");
            foreach (string name in names)
            {
                Match match = stareRegex.Match(name);
                if (match.Success)
                {
                    stares.Add(name);
                }
            }
            if (!ToggleTime(moduleInfo.ModuleDisplayName, stares))
            {
                string temp = moduleInfo.ModuleDisplayName;
                moduleInfo.ModuleDisplayName = "The Stare";
                moduleInfo.HandleStrike();
                moduleInfo.ModuleDisplayName = temp;
            }
            else
            {
                sprite.sprite = eyes[type * 2 + ((moduleName[2] == 'C') ? 0 : 1)];
                string oldName = moduleName;
                moduleName = "" + oldName[0] + oldName[1] + ((oldName[2] == 'C') ? 'O' : 'C') + oldName[3];
                Debug.LogFormat("[The Stare #{0}] Successfully {1} the Eye at " + bombInfo.GetFormattedTime() + '.', _moduleId, (moduleName[2] == 'C' ? "closed" : "opened"));
                if (moduleName[2] == moduleName[3])
                {
                    StartCoroutine(WaitForSolve());
                }
            }
        }
    }

    IEnumerator WaitForSolve()
    {
        while (moduleName[2] == moduleName[3])
        {
            List<string> names = bombInfo.GetModuleNames();
            List<string> stares = new List<string>();
            Regex stareRegex = new Regex(@"[1-9][1-9][XOC][OC]");
            foreach (string name in names)
            {
                Match match = stareRegex.Match(name);
                if (match.Success)
                {
                    stares.Add(name);
                }
            }
            bool allSolved = true;
            foreach (string eye in stares)
            {
                if (eye[2] != eye[3])
                {
                    allSolved = false;
                    /*
                    name = moduleInfo.ModuleDisplayName;
                    moduleInfo.ModuleDisplayName = "The Stare";
                    moduleInfo.HandleStrike();
                    moduleInfo.ModuleDisplayName = name;
                    */
                }
            }
            if (allSolved)
            {
                Debug.LogFormat("[The Stare #{0}] All Eyes are in their desired states.", _moduleId);
                almostDone = true;
                break;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    Type eyeType(string eye)
    {
        return (Type)((eye[1] - '1') % 3);
    }

    Mod eyeMod(string eye)
    {
        return (Mod)((eye[1] - '1') / 3);
    }

    bool ToggleTime(string eye, List<string> allEyes)
    {
        int count = 0;
        bool applied = false;
        string time = bombInfo.GetFormattedTime();
        if (eye[2] != 'C')
        {
            applied = true;
            count += Regex.Matches(time, "0").Count;
        }
        int unique = 2;
        foreach (string i in allEyes)
        {
            if (i[0] == eye[0])
            {
                unique--;
            }
        }
        if (unique > 0)
        {
            applied = true;
            count += Regex.Matches(time, "1").Count;
        }
        if (Regex.Matches(bombInfo.GetSerialNumber(), "[24680]").Count == 2)
        {
            applied = true;
            count += Regex.Matches(time, "2").Count;
        }
        List<char> colors = new List<char>();
        foreach (string i in allEyes)
        {
            if (!colors.Contains(i[0]))
            {
                colors.Add(i[0]);
            }
        }
        if (colors.Count() > 2)
        {
            applied = true;
            count += Regex.Matches(time, "3").Count;
        }
        if (eyeType(eye) != Type.Normal && eyeMod(eye) != Mod.Normal)
        {
            applied = true;
            count += Regex.Matches(time, "4").Count;
        }
        if (bombInfo.GetSolvedModuleNames().Count() % 5 == 0)
        {
            applied = true;
            count += Regex.Matches(time, "5").Count;
        }
        if ((eye[0] == '3' || eye[0] == '5' || eye[0] == '8'))
        {
            applied = true;
            count += Regex.Matches(time, "6").Count;
        }
        if (((eyeType(eye) == Type.Smol) == (eye[0] == '1' || eye[0] == '2')))
        {
            applied = true;
            count += Regex.Matches(time, "7").Count;
        }
        if ((allEyes.Count() == 8))
        {
            applied = true;
            count += Regex.Matches(time, "8").Count;
        }
        if (!applied)
        {
            count += Regex.Matches(time, "9").Count;
        }
        Debug.LogFormat("[The Stare #{0}] Current time (" + bombInfo.GetFormattedTime() + ") has {1} needed digit{2}.{3}", _moduleId, count, (count == 1) ? "" : "s", (count % 2 == 1) ? "" : " Strike due to an even number of needed digits!");
        return (count % 2 == 1);
    }

    char state(string eye, List<string> allEyes)
    {
        if ((Regex.Matches(bombInfo.GetSerialNumber(), "D").Count == 2) && (Regex.Matches(bombInfo.GetSerialNumber(), "[A-Z]").Count == 2))
        {
            return 'C';
        }
        State desiredState = State.Open;
        int count, count2;
        switch (eye[0] - '0')
        {
            case 1: //red
                if (eyeType(eye) == Type.Normal || (eyeType(eye) == Type.Alt && eyeMod(eye) == Mod.Normal))
                {
                    desiredState = State.Closed;
                }
                else if (eyeType(eye) == Type.Smol || (eyeType(eye) == Type.Alt && eyeMod(eye) == Mod.Rift))
                {
                    desiredState = State.Open;
                }
                else
                {
                    desiredState = State.Open;
                    foreach (string i in allEyes)
                    {
                        if (i[0] == '1' && eyeType(i) == Type.Alt && eyeMod(i) == Mod.Warp)
                        {
                            desiredState = (desiredState == State.Open) ? State.Closed : State.Open;
                        }
                    }
                }
                break;

            case 2: //burg.
                count = 0;
                foreach (string i in allEyes)
                {
                    if ((eyeType(i) == eyeType(eye)) != (eyeMod(i) == eyeMod(eye)) && count < 1)
                    {
                        count--;
                    }
                    if (eyeType(i) == eyeType(eye) && eyeMod(i) == eyeMod(eye))
                    {
                        count = 1;
                    }
                }
                count++;
                desiredState = (count < 0) ? State.Closed : State.Open;
                break;

            case 3: //gold
                desiredState = ((allEyes.Count() > initialTime / 60) == (eyeType(eye) == Type.Smol)) ? State.Closed : State.Open;
                break;

            case 4: //yellow
                count = 0;
                count2 = 0;
                foreach (string i in allEyes)
                {
                    if (i[0] == '4' && eyeType(i) == eyeType(eye))
                    {
                        count++;
                    }
                    if (i[0] == '7' && eyeType(i) == eyeType(eye))
                    {
                        count2++;
                    }
                }
                if (count == count2)
                {
                    count = 0;
                    count2 = 0;
                    List<string> inds = bombInfo.GetIndicators().ToList();
                    foreach (string ind in inds)
                    {
                        count += Regex.Matches(ind, "[PROSPIT]").Count;

                        count2 += Regex.Matches(ind, "[DERSE]").Count;
                    }
                    desiredState = (count2 > count) ? State.Closed : State.Open;
                }
                else
                {
                    desiredState = (count2 > count) ? State.Closed : State.Open;
                }
                break;

            case 5: //green
                if (eyeMod(eye) == Mod.Normal)
                {
                    count = 3;
                }
                else if (eyeMod(eye) == Mod.Warp)
                {
                    count = 5;
                }
                else
                {
                    count = 7;
                }
                desiredState = (bombInfo.GetSolvableModuleNames().Count() % count > 0) ? State.Closed : State.Open;
                break;

            case 6: //turquoise
                count = 0;
                foreach (string i in allEyes)
                {
                    if ((eyeType(i) == eyeType(eye)) && (count % 3 < 2))
                    {
                        count++;
                    }
                    if ((eyeMod(i) == eyeMod(eye)) && (count % 9 < 6))
                    {
                        count += 3;
                    }
                }
                count2 = Regex.Matches(bombInfo.GetSerialNumber(), "[TURQUOISE]").Count;
                desiredState = ((count < 8) == (count2 % 2 == 0)) ? State.Closed : State.Open;
                break;

            case 7: //purple
                count = 0;
                count2 = 0;
                foreach (string i in allEyes)
                {
                    if (i[0] == '4' && eyeType(i) == eyeType(eye))
                    {
                        count++;
                    }
                    if (i[0] == '7' && eyeType(i) == eyeType(eye))
                    {
                        count2++;
                    }
                }
                if (count == count2)
                {
                    count = 0;
                    count2 = 0;
                    List<string> inds = bombInfo.GetIndicators().ToList();
                    foreach (string ind in inds)
                    {
                        count += Regex.Matches(ind, "[PROSPIT]").Count;

                        count2 += Regex.Matches(ind, "[DERSE]").Count;
                    }
                    desiredState = (count2 < count) ? State.Closed : State.Open;
                }
                else
                {
                    desiredState = (count2 < count) ? State.Closed : State.Open;
                }
                break;

            case 8: //gray
                count = 0;
                foreach (string i in allEyes)
                {
                    if (eyeType(i) == Type.Smol)
                    {
                        count += 1;
                    }
                    if (eyeType(i) == Type.Normal)
                    {
                        count += 2;
                    }
                    if (eyeType(i) == Type.Alt)
                    {
                        count += 3;
                    }
                    if (eyeMod(i) == Mod.Rift)
                    {
                        count += 5;
                    }
                    if (eyeMod(i) == Mod.Warp)
                    {
                        count += 10;
                    }
                }
                count %= 50;
                desiredState = ((count % 3 == 0) != (count % 7 == 0)) ? State.Closed : State.Open;
                break;

            case 9: //white
                count = 0;
                count2 = 0;
                foreach (string i in allEyes)
                {
                    if (eyeType(i) == eyeType(eye) && i[0] != '9')
                    {
                        if (state(i, allEyes) == 'C')
                        {
                            count += 1;
                        }
                        else
                        {
                            count2 += 1;
                        }
                    }
                }
                desiredState = (count > count2) ? State.Open : State.Closed;
                break;
        }

        return (desiredState == State.Open) ? 'O' : 'C';
    }



    //twitch plays
    private bool timeIsValid(string s)
    {
        char[] valids = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', ':' };
        char[] validints = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        foreach (char c in s)
        {
            if (!valids.Contains(c))
            {
                return false;
            }
        }
        bool atleast2start = false;
        bool end = false;
        if (validints.Contains(s.ElementAt(0)) && validints.Contains(s.ElementAt(1)))
        {
            atleast2start = true;
        }
        if(validints.Contains(s.ElementAt(s.Length-1)) && validints.Contains(s.ElementAt(s.Length - 2)) && s.ElementAt(s.Length - 3).Equals(':'))
        {
            end = true;
        }
        if(atleast2start == true && end == true)
        {
            return true;
        }
        return false;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} toggle 09:44 [Switches the state of the eye when the bomb's timer is '09:44' exactly] | !{0} toggle [Switches the state of the eye (the last toggle for submitting needs no timer)] | !{0} colorblind [Toggle colorblind mode]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            module.OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if(colorblindActive == true)
            {
                cblind.GetComponent<TextMesh>().text = "";
                colorblindActive = false;
                Debug.LogFormat("[The Stare #{0}] Disabling colorblind mode! (TP)", _moduleId);
            }
            else
            {
                cblind.GetComponent<TextMesh>().text = colorName.Substring(0,3);
                colorblindActive = true;
                Debug.LogFormat("[The Stare #{0}] Enabling colorblind mode! (TP)", _moduleId);
            }
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if(parameters.Length == 2)
            {
                if (timeIsValid(parameters[1]) && parameters[1].Length >= 5)
                {
                    yield return null;
                    while(!bombInfo.GetFormattedTime().Equals(parameters[1])) yield return "trycancel The Eye's toggle was cancelled due to a cancel request.";
                    module.OnInteract();
                }
            }
            yield break;
        }
    }
}