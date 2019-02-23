using KMHelper;
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
    public SpriteRenderer sprite;
    public MeshRenderer model;
    
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
    
    private int color;
    private int type;
    private int modifier;
    
    private List<int> times;
    
    private bool isSolved = false;
    
    private int initialTime = 0;
    
    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;
    
    enum State {Open = 0, Closed};
    enum Type {Normal = 0, Alt, Smol};
    enum Mod {Normal = 0, Warp, Rift};
    
    void Start()
    {
        initialTime = (int)bombInfo.GetTime();
        _moduleId = _moduleIdCounter++;
        color = UnityEngine.Random.Range(0, colors.Length);
        type = UnityEngine.Random.Range(0, 7);
        type = (type > 4) ? 2 : (type > 2) ? 1 : 0;
        modifier = UnityEngine.Random.Range(0, 9);
        modifier = (modifier > 6) ? 2 : (modifier > 4) ? 1 : 0;
        moduleInfo.ModuleDisplayName = (color + 1).ToString() + (type + 1 + (modifier * 3)).ToString() + "XO";
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
        sprite.sprite = eyes[type * 2];
        model.material = materials[modifier];
        sprite.color = colors[color];
        model.material.color = colors[color];
        module.OnInteract += delegate() {OnPress(); return false;};
        GetComponent<KMBombModule>().OnPass += delegate(){isSolved = true; /*sprite.sprite = eyes[type * 2 + 1];*/ return true;};
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
        moduleInfo.ModuleDisplayName = moduleInfo.ModuleDisplayName.Substring(0, 3) + desiredState;
        string colorName = "", typeName = "", modName = "";
        switch (color)
        {
            case 0: colorName = "Red"; break;
            case 1: colorName = "Burgundy"; break;
            case 2: colorName = "Gold"; break;
            case 3: colorName = "Yellow"; break;
            case 4: colorName = "Green"; break;
            case 5: colorName = "Turquoise"; break;
            case 6: colorName = "Purple"; break;
            case 7: colorName = "Gray"; break;
            case 8: colorName = "White"; break;
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
        if (!isSolved)
        {
            module.AddInteractionPunch();
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
            sprite.sprite = eyes[type * 2 + ((moduleInfo.ModuleDisplayName[2] == 'C') ? 0 : 1)];
            string oldName = moduleInfo.ModuleDisplayName;
            moduleInfo.ModuleDisplayName = "" + oldName[0] + oldName[1] + ((oldName[2] == 'C') ? 'O' : 'C') + oldName[3];
            Debug.LogFormat("[The Stare #{0}] {1} the Eye at " + bombInfo.GetFormattedTime() + '.', _moduleId, (moduleInfo.ModuleDisplayName[2] == 'C' ? "Closed" : "Opened"));
            if (moduleInfo.ModuleDisplayName[2] == moduleInfo.ModuleDisplayName[3])
            {
                StartCoroutine(WaitForSolve());
            }
        }
    }
    
    IEnumerator WaitForSolve()
    {
        while (moduleInfo.ModuleDisplayName[2] == moduleInfo.ModuleDisplayName[3])
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
                Debug.LogFormat("[The Stare #{0}] All Eyes are in their desired states. Module solved.", _moduleId);
                moduleInfo.ModuleDisplayName = "The Stare";
                moduleInfo.HandlePass();
                break;
            }
            yield return new WaitForSeconds(0.2f);
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
        Debug.LogFormat("[The Stare #{0}] Current time (" + bombInfo.GetFormattedTime() + ") has {1} needed digit{2}.{3}", _moduleId, count, (count == 1) ? "" : "s", (count % 2 == 1) ? "" : " Strike!");
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
                desiredState = ((count < 9) == (count2 % 2 == 0)) ? State.Closed : State.Open;
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
    
    
    
    
    #pragma warning disable 0414
    private string TwitchManualCode = "The Stare";
    #pragma warning restore 0414

}
/*
    public void TwitchHandleForcedSolve()
    {
        isHeld = false;
        int sprite = UnityEngine.Random.Range(0, 14);
        sprite += (sprite < deathSprite) ? 0 : 1;
        moduleSprite.sprite = itemSprites[sprite];
        Debug.LogFormat("[Question Mark #{0}] Module forcibly solved.", _moduleId);
        GetComponent<KMBombModule>().HandlePass();
    }

    public IEnumerator ProcessTwitchCommand(string cmd)
    {
        yield return null;
        cmd = cmd.ToLowerInvariant();
        if (cmd.StartsWith("hold"))
        {
            if (isHeld)
            {
                yield return "sendtochaterror Module is already held.";
                yield break;
            }

            yield return "Question Mark";
            yield return module;
            yield break;
        }
        else if(cmd.StartsWith("release"))
        {
            if(!cmd.StartsWith("release "))
            {
                yield return "sendtochaterror No release times specified.";
                yield break;
            }
            if(!isHeld)
            {
                yield return "sendtochaterror Module is not currently held.";
                yield break;
            }
            cmd = cmd.Substring(8);

            string[] timeList = cmd.Split(' ');
            List<int> times = new List<int>();
            for(int i = 0; i < timeList.Length; i++)
            {
                times.Add((int)timeList[i][timeList[i].Length - 1] - '0');
                if (timeList[i].Length != 1)
                {
                    yield return "sendtochaterror Release times can only be specified as the last second digit.";
                    yield break;
                }
            }

            yield return "Question Mark";
            
            int currentTime = (int)info.GetTime();
            int targetTime = -1;
            
            if (TwitchZenMode)
            {
                foreach(int time in times)
                {
                    int t = time;
                    while(t < currentTime) t += 10;
                    if(t < targetTime || targetTime == -1) targetTime = t;
                }
            }
            else
            {
                foreach(int time in times) 
                {
                    int t = time;
                    while(t <= currentTime)
                    {
                        t += 10;
                    }
                    t -= 10;
                    if(t > targetTime) targetTime = t;
                }
            }
            
            if(targetTime == -1)
            {
                yield return "sendtochaterror No valid release times specified.";
                yield break;
            }
            
            yield return "sendtochat Target release time: " + (targetTime / 60).ToString("D2") + ":" + (targetTime % 60).ToString("D2");

            while(true)
            {
                currentTime = (int)info.GetTime();
                if(currentTime != targetTime)
                {
                    yield return "trycancel";
                }
                else
                {
                    yield return module;
                    break;
                }
            }
            yield break;
        }
        else yield return "sendtochaterror Commands must start with \"hold\" or \"release\".";
        yield break;
    }
}
*/
