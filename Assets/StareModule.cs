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
    public KMColorblindMode Colorblind;
    public SpriteRenderer sprite;
    public MeshRenderer model;
    public GameObject cblind;
    public GameObject tpautosolvetext;

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

    sealed class StareBombInfo
    {
        public List<StareModule> Modules = new List<StareModule>();
        public List<bool> altered = new List<bool>();
    }

    private bool localalt = false;

    private static readonly Dictionary<string, StareBombInfo> _infos = new Dictionary<string, StareBombInfo>();

    private StareBombInfo info;

    public string moduleName = "";

    private bool almostDone = false;
    private bool isSolved = false;

    private int initialTime = 0;

    private int initialInt;

    private bool colorblindActive = false;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    enum State { Open = 0, Closed };
    enum Type { Normal = 0, Alt, Smol };
    enum Mod { Normal = 0, Warp, Rift };

    private State initialState;
    private char neededState;

    private bool coRunning = false;

    void Start()
    {
        colorblindActive = Colorblind.ColorblindModeActive;
        cblind.GetComponent<TextMesh>().text = "";
        _moduleId = _moduleIdCounter++;
        color = UnityEngine.Random.Range(0, 9);
        type = UnityEngine.Random.Range(0, 7);
        type = (type > 4) ? 2 : (type > 2) ? 1 : 0;
        modifier = UnityEngine.Random.Range(0, 9);
        modifier = (modifier > 6) ? 2 : (modifier > 4) ? 1 : 0;
        initialInt = UnityEngine.Random.Range(0, 2);
        if(initialInt == 0)
        {
            initialState = State.Closed;
            moduleName = (color + 1).ToString() + (type + 1 + (modifier * 3)).ToString() + "CX";
        }
        else
        {
            initialState = State.Open;
            moduleName = (color + 1).ToString() + (type + 1 + (modifier * 3)).ToString() + "OX";
        }
        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void ActivateModule()
    {
        initialTime = (int)bombInfo.GetTime();
        Init();
        SetDesiredState();
    }

    void Init()
    {
        if(initialInt == 0)
        {
            sprite.sprite = eyes[(type * 2) + 1];
        }
        else
        {
            sprite.sprite = eyes[type * 2];
        }
        model.material = materials[modifier];
        sprite.color = colors[color];
        model.material.color = colors[color];
        var serialNumber = bombInfo.GetSerialNumber();
        if (!_infos.ContainsKey(serialNumber))
            _infos[serialNumber] = new StareBombInfo();
        info = _infos[serialNumber];
        info.Modules.Add(this);
        info.altered.Add(false);
        module.OnInteract += delegate () { OnPress(); return false; };
        GetComponent<KMBombModule>().OnPass += delegate () { isSolved = true; /*sprite.sprite = eyes[type * 2 + 1];*/ return true; };
    }

    void SetDesiredState()
    {
        string typeName = "", modName = "";
        colorName = "";
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
        cblind.GetComponent<TextMesh>().color = colors[color];
        tpautosolvetext.GetComponent<TextMesh>().color = colors[color];
        if (colorblindActive)
        {
            Debug.LogFormat("[The Stare #{0}] Colorblind mode is active!", _moduleId);
            cblind.GetComponent<TextMesh>().text = colorName.Substring(0, 3);
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
        Debug.LogFormat("[The Stare #{0}] This is a " + typeName + " " + modName + " " + colorName + " Eye.", _moduleId);
        //Here so that all other stares can load in before checking for desired state
        StartCoroutine(delayedStareStateGetter());
    }

    IEnumerator delayedStareStateGetter()
    {
        yield return new WaitForSeconds(0.1f);
        List<string> stares = new List<string>();
        Regex stareRegex = new Regex(@"[1-9][1-9][OC][XOC]");
        foreach (StareModule mod in info.Modules)
        {
            Match match = stareRegex.Match(mod.moduleName);
            if (match.Success)
            {
                stares.Add(mod.moduleName);
            }
        }
        char desiredState = state(moduleName, stares);
        if(color != 8)
        {
            neededState = desiredState;
            moduleName = moduleName.Substring(0, 3) + desiredState;
            Debug.LogFormat("[The Stare #{0}] This makes the Eye's desired state: {1}", _moduleId, (desiredState == 'C' ? "closed" : "open"));
        }
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
                //moduleInfo.ModuleDisplayName = "The Stare";
                moduleInfo.HandlePass();
                isSolved = true;
                return;
            }
            List<string> stares = new List<string>();
            Regex stareRegex = new Regex(@"[1-9][1-9][OC][XOC]");
            foreach (StareModule mod in info.Modules)
            {
                Match match = stareRegex.Match(mod.moduleName);
                if (match.Success)
                {
                    stares.Add(mod.moduleName);
                }
            }
            if (!ToggleTime(moduleName, stares, true))
            {
                /**string temp = moduleInfo.ModuleDisplayName;
                moduleInfo.ModuleDisplayName = "The Stare";*/
                moduleInfo.HandleStrike();
                //moduleInfo.ModuleDisplayName = temp;
            }
            else
            {
                if(localalt == false)
                {
                    localalt = true;
                    info.altered.RemoveAt(0);
                    info.altered.Add(true);
                }
                sprite.sprite = eyes[type * 2 + ((moduleName[2] == 'C') ? 0 : 1)];
                string oldName = moduleName;
                moduleName = "" + oldName[0] + oldName[1] + ((oldName[2] == 'C') ? 'O' : 'C') + oldName[3];
                if ((int)bombInfo.GetTime() < 10)
                {
                    Debug.LogFormat("[The Stare #{0}] Successfully {1} the Eye at 00:0" + (int)bombInfo.GetTime() + '.', _moduleId, (moduleName[2] == 'C' ? "closed" : "opened"));
                }
                else if ((int)bombInfo.GetTime() < 60 && (int)bombInfo.GetTime() > 10)
                {
                    Debug.LogFormat("[The Stare #{0}] Successfully {1} the Eye at 00:" + (int)bombInfo.GetTime() + '.', _moduleId, (moduleName[2] == 'C' ? "closed" : "opened"));
                }
                else {
                    Debug.LogFormat("[The Stare #{0}] Successfully {1} the Eye at " + bombInfo.GetFormattedTime() + '.', _moduleId, (moduleName[2] == 'C' ? "closed" : "opened"));
                }
                if (moduleName[2] == moduleName[3] && coRunning == false)
                {
                    StartCoroutine(WaitForSolve());
                }
            }
        }
    }

    IEnumerator WaitForSolve()
    {
        coRunning = true;
        while (info.altered.Contains(false) || !allAreEqual())
        {
            yield return new WaitForSeconds(0.05f);
        }
        Debug.LogFormat("[The Stare #{0}] All Eyes are in their desired states.", _moduleId);
        tpautosolvetext.SetActive(false);
        almostDone = true;
        coRunning = false;
    }

    private bool allAreEqual()
    {
        for(int i = 0; i < info.Modules.Count; i++)
        {
            if(info.Modules.ElementAt(i).moduleName[2] != info.Modules.ElementAt(i).moduleName[3])
            {
                return false;
            }
        }
        return true;
    }

    Type eyeType(string eye)
    {
        return (Type)((eye[1] - '1') % 3);
    }

    Mod eyeMod(string eye)
    {
        return (Mod)((eye[1] - '1') / 3);
    }

    bool ToggleTime(string eye, List<string> allEyes, bool logtime)
    {
        if (logtime)
        {
            Debug.LogFormat("[The Stare #{0}] -----------------------------------------------------------------------", _moduleId);
            Debug.LogFormat("[The Stare #{0}] The Eye has been pressed! Calculating Needed Digits for Current Time...", _moduleId);
        }
        int count = 0;
        bool applied = false;
        string time = bombInfo.GetFormattedTime();
        if ((int)bombInfo.GetTime() < 10)
        {
            time = "00:0" + (int)bombInfo.GetTime();
        }
        else if ((int)bombInfo.GetTime() < 60 && (int)bombInfo.GetTime() > 10)
        {
            time = "00:" + (int)bombInfo.GetTime();
        }
        if (eye[2] != 'C')
        {
            applied = true;
            count += Regex.Matches(time, "0").Count;
            if(logtime)
                Debug.LogFormat("[The Stare #{0}] This eye is open, 0 is a needed digit.", _moduleId);
        }
        else
        {
            if(logtime)
                Debug.LogFormat("[The Stare #{0}] This eye is closed, 0 is not a needed digit.", _moduleId);
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
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] This eye is unique in its colour, 1 is a needed digit.", _moduleId);
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] This eye is not unique in its colour, 1 is not a needed digit.", _moduleId);
        }
        if (Regex.Matches(bombInfo.GetSerialNumber(), "[24680]").Count == 2)
        {
            applied = true;
            count += Regex.Matches(time, "2").Count;
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] There is exactly two even digits in this bomb's serial number, 2 is a needed digit.", _moduleId);
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] There is not exactly two even digits in this bomb's serial number, 2 is not a needed digit.", _moduleId);
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
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] There is at least three differently coloured eyes on the bomb ({1}), 3 is a needed digit.", _moduleId, colors.Count);
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] There is not at least three differently coloured eyes on the bomb ({1}), 3 is not a needed digit.", _moduleId, colors.Count);
        }
        if (eyeType(eye) != Type.Normal && eyeMod(eye) != Mod.Normal)
        {
            applied = true;
            count += Regex.Matches(time, "4").Count;
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] This eye's type is not Normal and background is not Plain, 4 is a needed digit.", _moduleId);
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] This eye's type is Normal or background is Plain, 4 is not a needed digit.", _moduleId);
        }
        if (bombInfo.GetSolvedModuleNames().Count() % 5 == 0)
        {
            applied = true;
            count += Regex.Matches(time, "5").Count;
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] The number of disarmed modules on the bomb is evenly divisible by 5 ({1}), 5 is a needed digit.", _moduleId, bombInfo.GetSolvedModuleNames().Count());
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] The number of disarmed modules on the bomb is not evenly divisible by 5 ({1}), 5 is not a needed digit.", _moduleId, bombInfo.GetSolvedModuleNames().Count());
        }
        if ((eye[0] == '3' || eye[0] == '5' || eye[0] == '8'))
        {
            applied = true;
            count += Regex.Matches(time, "6").Count;
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] This eye's color starts with the letter 'G', 6 is a needed digit.", _moduleId);
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] This eye's color does not start with the letter 'G', 6 is not a needed digit.", _moduleId);
        }
        if (((eyeType(eye) == Type.Smol) == (eye[0] == '1' || eye[0] == '2')))
        {
            applied = true;
            count += Regex.Matches(time, "7").Count;
            if (logtime)
            {
                if (eyeType(eye) == Type.Smol && (eye[0] != '1' && eye[0] != '2'))
                {
                    Debug.LogFormat("[The Stare #{0}] This eye's type is Small and its color is neither red nor burgundy, 7 is not a needed digit.", _moduleId);
                }
                else if (eyeType(eye) != Type.Smol && (eye[0] != '1' && eye[0] != '2'))
                {
                    Debug.LogFormat("[The Stare #{0}] This eye's type is not Small and its color is neither red nor burgundy, 7 is a needed digit.", _moduleId);
                }
                else if (eyeType(eye) == Type.Smol && (eye[0] == '1' || eye[0] == '2'))
                {
                    Debug.LogFormat("[The Stare #{0}] This eye's type is Small and its color is either red or burgundy, 7 is a needed digit.", _moduleId);
                }
                else
                {
                    Debug.LogFormat("[The Stare #{0}] This eye's type is not Small and its color is either red or burgundy, 7 is not a needed digit.", _moduleId);
                }
            }
        }
        if ((allEyes.Count() == 8))
        {
            applied = true;
            count += Regex.Matches(time, "8").Count;
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] There are exactly eight eyes on the bomb, 8 is a needed digit.", _moduleId);
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] There are not exactly eight eyes on the bomb, 8 is not a needed digit.", _moduleId);
        }
        if (!applied)
        {
            count += Regex.Matches(time, "9").Count;
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] None of the previous digits are needed, 9 is a needed digit.", _moduleId);
        }
        else
        {
            if (logtime)
                Debug.LogFormat("[The Stare #{0}] At least one of the previous digits is needed, 9 is not a needed digit.", _moduleId);
        }
        if (logtime)
        {
            if ((int)bombInfo.GetTime() < 10)
            {
                Debug.LogFormat("[The Stare #{0}] Current time (00:0" + (int)bombInfo.GetTime() + ") has {1} needed digit{2}.{3}", _moduleId, count, (count == 1) ? "" : "s", (count % 2 == 1) ? "" : " Strike due to an even number of needed digits!");
            }
            else if ((int)bombInfo.GetTime() < 60 && (int)bombInfo.GetTime() > 10)
            {
                Debug.LogFormat("[The Stare #{0}] Current time (00:" + (int)bombInfo.GetTime() + ") has {1} needed digit{2}.{3}", _moduleId, count, (count == 1) ? "" : "s", (count % 2 == 1) ? "" : " Strike due to an even number of needed digits!");
            }
            else
            {
                Debug.LogFormat("[The Stare #{0}] Current time (" + bombInfo.GetFormattedTime() + ") has {1} needed digit{2}.{3}", _moduleId, count, (count == 1) ? "" : "s", (count % 2 == 1) ? "" : " Strike due to an even number of needed digits!");
            }
        }
        return (count % 2 == 1);
    }

    char state(string eye, List<string> allEyes)
    {
        if ((Regex.Matches(bombInfo.GetSerialNumber(), "D").Count == 2) && (Regex.Matches(bombInfo.GetSerialNumber(), "[A-Z]").Count == 2))
        {
            Debug.LogFormat("[The Stare #{0}] Unicorn Eye Rule: There are exactly two D's and no other letters in the serial number.", _moduleId);
            return 'C';
        }
        State desiredState = State.Open;
        int count, count2, count3;
        switch (eye[0] - '0')
        {
            case 1: //red
                if (eyeType(eye) == Type.Normal || (eyeType(eye) == Type.Alt && eyeMod(eye) == Mod.Normal))
                {
                    if(eyeType(eye) == Type.Normal)
                    {
                        Debug.LogFormat("[The Stare #{0}] Red Eye Rule: The eye's type is Normal.", _moduleId);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Red Eye Rule: The eye's type is Special and its background is Plain.", _moduleId);
                    }
                    desiredState = State.Closed;
                }
                else if (eyeType(eye) == Type.Smol || (eyeType(eye) == Type.Alt && eyeMod(eye) == Mod.Rift))
                {
                    if(eyeType(eye) == Type.Smol)
                    {
                        Debug.LogFormat("[The Stare #{0}] Red Eye Rule: The eye's type is Small.", _moduleId);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Red Eye Rule: The eye's type is Special and its background is Rifted.", _moduleId);
                    }
                    desiredState = State.Open;
                }
                else
                {
                    desiredState = State.Open;
                    int ct = 0;
                    foreach (string i in allEyes)
                    {
                        if (i[0] == '1' && eyeType(i) == Type.Alt && eyeMod(i) == Mod.Warp)
                        {
                            ct++;
                            desiredState = (desiredState == State.Open) ? State.Closed : State.Open;
                        }
                    }
                    if(desiredState == State.Open)
                    {
                        Debug.LogFormat("[The Stare #{0}] Red Eye Rule: There is an even number of Red Special Warped eyes ({1}).", _moduleId, ct);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Red Eye Rule: There is an odd number of Red Special Warped eyes ({1}).", _moduleId, ct);
                    }
                }
                /**if (eyeType(eye) == Type.Normal || (eyeType(eye) == Type.Alt && eyeMod(eye) == Mod.Normal))
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
                    int ct = 0;
                    foreach (string i in allEyes)
                    {
                        if (i[0] == '1' && eyeType(i) == Type.Alt && eyeMod(i) == Mod.Warp)
                        {
                            ct++;
                            desiredState = (desiredState == State.Open) ? State.Closed : State.Open;
                        }
                    }
                }*/
                break;

            case 2: //burg.
                count = 0;
                foreach (string i in allEyes)
                {
                    if (eyeType(i) == eyeType(eye) && eyeMod(i) == eyeMod(eye))
                    {
                        count++;
                    }
                }
                if(count > 1)
                {
                    Debug.LogFormat("[The Stare #{0}] Burgundy Eye Rule: There is another eye with the same type/background combo.", _moduleId);
                    desiredState = State.Open;
                }
                else
                {
                    count2 = 0;
                    count3 = 0;
                    foreach (string i in allEyes)
                    {
                        if (eyeType(i) == eyeType(eye))
                        {
                            count2++;
                        }
                        if (eyeMod(i) == eyeMod(eye))
                        {
                            count3++;
                        }
                    }
                    if (count2 == 1 && count3 == 1)
                    {
                        Debug.LogFormat("[The Stare #{0}] Burgundy Eye Rule: This eye is unique in both its type and background.", _moduleId);
                        desiredState = State.Open;
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Burgundy Eye Rule: Neither rule is true.", _moduleId);
                        desiredState = State.Closed;
                    }
                }
                /**count = 0;
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
                desiredState = (count < 0) ? State.Closed : State.Open;*/
                break;

            case 3: //gold
                if (allEyes.Count() < (initialTime / 60))
                {
                    Debug.LogFormat("[The Stare #{0}] Gold Eye Rule: Total number of eyes ({1}) is less than initial minutes remaining ({2}).", _moduleId, allEyes.Count, initialTime / 60);
                    desiredState = State.Open;
                }
                else
                {
                    Debug.LogFormat("[The Stare #{0}] Gold Eye Rule: Total number of eyes ({1}) is greater than or equal to initial minutes remaining ({2}).", _moduleId, allEyes.Count, initialTime / 60);
                    desiredState = State.Closed;
                }
                if (eyeType(eye) == Type.Smol)
                {
                    Debug.LogFormat("[The Stare #{0}] Gold Eye Rule: This eye's type is Small.", _moduleId);
                    if (desiredState == State.Open)
                    {
                        desiredState = State.Closed;
                    }
                    else
                    {
                        desiredState = State.Open;
                    }
                }
                else
                {
                    Debug.LogFormat("[The Stare #{0}] Gold Eye Rule: This eye's type is not Small.", _moduleId);
                }
                /**if (allEyes.Count() < (initialTime / 60))
                {
                    desiredState = State.Open;
                }
                else
                {
                    desiredState = State.Closed;
                }
                if (eyeType(eye) == Type.Smol)
                {
                    if (desiredState == State.Open)
                    {
                        desiredState = State.Closed;
                    }
                    else
                    {
                        desiredState = State.Open;
                    }
                }*/
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
                string typeName = "";
                switch (type)
                {
                    case 0: typeName = "Normal"; break;
                    case 1: typeName = "Special"; break;
                    case 2: typeName = "Small"; break;
                }
                if (count == count2)
                {
                    Debug.LogFormat("[The Stare #{0}] Yellow Eye Rule: There is an equal number of Yellow {1} eye's and Purple {1} eye's ({2}).", _moduleId, typeName, count);
                    count = 0;
                    count2 = 0;
                    List<string> inds = bombInfo.GetIndicators().ToList();
                    foreach (string ind in inds)
                    {
                        count += Regex.Matches(ind, "[PROSPIT]").Count;

                        count2 += Regex.Matches(ind, "[DERSE]").Count;
                    }
                    if(count > count2)
                    {
                        Debug.LogFormat("[The Stare #{0}] Yellow Eye Rule: There is more letters from the word 'PROSPIT' in this bomb's indicators ({1}) than the word 'DERSE' ({2}).", _moduleId, count, count2);
                    }
                    else if (count < count2)
                    {
                        Debug.LogFormat("[The Stare #{0}] Yellow Eye Rule: There is more letters from the word 'DERSE' in this bomb's indicators ({1}) than the word 'PROSPIT' ({2}).", _moduleId, count2, count);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Yellow Eye Rule: There is an equal number of letters from the word 'PROSPIT' and the word 'DERSE' in the bomb's indicators ({1}).", _moduleId, count);
                    }
                    desiredState = (count2 > count) ? State.Closed : State.Open;
                }
                else
                {
                    if(count > count2)
                    {
                        Debug.LogFormat("[The Stare #{0}] Yellow Eye Rule: There is more Yellow {1} eye's ({2}) than Purple {1} eye's ({3}).", _moduleId, typeName, count, count2);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Yellow Eye Rule: There is more Purple {1} eye's ({2}) than Yellow {1} eye's ({3}).", _moduleId, typeName, count2, count);
                    }
                    desiredState = (count2 > count) ? State.Closed : State.Open;
                }
                /**count = 0;
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
                }*/
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
                string modName = "";
                switch (modifier)
                {
                    case 0: modName = "Plain"; break;
                    case 1: modName = "Warped"; break;
                    case 2: modName = "Rifted"; break;
                }
                Debug.LogFormat("[The Stare #{0}] Green Eye Rule: This eye's background is {1}, which makes X = {2}.", _moduleId, modName, count);
                if (bombInfo.GetSolvableModuleNames().Count() % count > 0)
                {
                    Debug.LogFormat("[The Stare #{0}] Green Eye Rule: The number of non-needy modules on the bomb ({1}) is not evenly divisible by X.", _moduleId, bombInfo.GetSolvableModuleNames().Count());
                }
                else
                {
                    Debug.LogFormat("[The Stare #{0}] Green Eye Rule: The number of non-needy modules on the bomb ({1}) is evenly divisible by X.", _moduleId, bombInfo.GetSolvableModuleNames().Count());
                }
                desiredState = (bombInfo.GetSolvableModuleNames().Count() % count > 0) ? State.Closed : State.Open;
                /**if (eyeMod(eye) == Mod.Normal)
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
                desiredState = (bombInfo.GetSolvableModuleNames().Count() % count > 0) ? State.Closed : State.Open;*/
                break;

            case 6: //turquoise
                count = 0;
                count2 = 0;
                foreach (string i in allEyes)
                {
                    if (eyeType(i) == eyeType(eye))
                    {
                        count++;
                    }
                    if (eyeMod(i) == eyeMod(eye))
                    {
                        count2++;
                    }
                }
                count3 = Regex.Matches(bombInfo.GetSerialNumber(), "[TURQUOISE]").Count;
                if ((count == 1 || count2 == 1) && (count3 % 2 == 0))
                {
                    Debug.LogFormat("[The Stare #{0}] Turquoise Eye Rule: This eye is unique in its type or background and there is an even number of letters from the word 'TURQUOISE' in the serial number ({1})", _moduleId, count3);
                    desiredState = State.Closed;
                }
                else if ((count == 1 || count2 == 1) && (count3 % 2 == 1))
                {
                    Debug.LogFormat("[The Stare #{0}] Turquoise Eye Rule: This eye is unique in its type or background and there is not an even number of letters from the word 'TURQUOISE' in the serial number ({1})", _moduleId, count3);
                    desiredState = State.Open;
                }
                else if ((count != 1 && count2 != 1) && (count3 % 2 == 0))
                {
                    Debug.LogFormat("[The Stare #{0}] Turquoise Eye Rule: The eye is not unique in its type or background and there is an even number of letters from the word 'TURQUOISE' in the serial number ({1})", _moduleId, count3);
                    desiredState = State.Open;
                }
                else
                {
                    Debug.LogFormat("[The Stare #{0}] Turquoise Eye Rule: The eye is not unique in its type or background and there is not an even number of letters from the word 'TURQUOISE' in the serial number ({1})", _moduleId, count3);
                    desiredState = State.Closed;
                }
                /**count = 0;
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
                desiredState = ((count < 8) == (count2 % 2 == 0)) ? State.Closed : State.Open;*/
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
                string typeName2 = "";
                switch (type)
                {
                    case 0: typeName2 = "Normal"; break;
                    case 1: typeName2 = "Special"; break;
                    case 2: typeName2 = "Small"; break;
                }
                if (count == count2)
                {
                    Debug.LogFormat("[The Stare #{0}] Purple Eye Rule: There is an equal number of Yellow {1} eye's and Purple {1} eye's ({2})", _moduleId, typeName2, count);
                    count = 0;
                    count2 = 0;
                    List<string> inds = bombInfo.GetIndicators().ToList();
                    foreach (string ind in inds)
                    {
                        count += Regex.Matches(ind, "[PROSPIT]").Count;

                        count2 += Regex.Matches(ind, "[DERSE]").Count;
                    }
                    if (count > count2)
                    {
                        Debug.LogFormat("[The Stare #{0}] Purple Eye Rule: There is more letters from the word 'PROSPIT' in this bomb's indicators ({1}) than the word 'DERSE' ({2}).", _moduleId, count, count2);
                    }
                    else if (count < count2)
                    {
                        Debug.LogFormat("[The Stare #{0}] Purple Eye Rule: There is more letters from the word 'DERSE' in this bomb's indicators ({1}) than the word 'PROSPIT' ({2}).", _moduleId, count2, count);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Purple Eye Rule: There is an equal number of letters from the word 'PROSPIT' and the word 'DERSE' in the bomb's indicators ({1}).", _moduleId, count);
                    }
                    desiredState = (count2 < count) ? State.Closed : State.Open;
                }
                else
                {
                    if (count > count2)
                    {
                        Debug.LogFormat("[The Stare #{0}] Purple Eye Rule: There is more Yellow {1} eye's ({2}) than Purple {1} eye's ({3}).", _moduleId, typeName2, count, count2);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Purple Eye Rule: There is more Purple {1} eye's ({2}) than Yellow {1} eye's ({3}).", _moduleId, typeName2, count2, count);
                    }
                    desiredState = (count2 < count) ? State.Closed : State.Open;
                }
                /**count = 0;
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
                }*/
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
                Debug.LogFormat("[The Stare #{0}] Gray Eye Rule: The total number received from each gray eye was {1}.", _moduleId, count);
                int temp = count;
                count %= 50;
                Debug.LogFormat("[The Stare #{0}] Gray Eye Rule: {1} modulo 50 is {2}.", _moduleId, temp, count);
                if (count % 21 == 0)
                {
                    Debug.LogFormat("[The Stare #{0}] Gray Eye Rule: {1} is divisible by 21.", _moduleId, count);
                    desiredState = State.Open;
                }
                else if ((count % 3 == 0) || (count % 7 == 0))
                {
                    if((count % 3 == 0) || (count % 7 == 0))
                    {
                        Debug.LogFormat("[The Stare #{0}] Gray Eye Rule: {1} is divisible by 3 and 7 but not 21.", _moduleId, count);
                    }
                    else if(count % 3 == 0)
                    {
                        Debug.LogFormat("[The Stare #{0}] Gray Eye Rule: {1} is divisible by 3 but not 21.", _moduleId, count);
                    }
                    else
                    {
                        Debug.LogFormat("[The Stare #{0}] Gray Eye Rule: {1} is divisible by 7 but not 21.", _moduleId, count);
                    }
                    desiredState = State.Closed;
                }
                else
                {
                    Debug.LogFormat("[The Stare #{0}] Gray Eye Rule: {1} is not divisible by 3, 7, or 21.", _moduleId, count);
                    desiredState = State.Open;
                }
                /**count = 0;
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
                desiredState = ((count % 3 == 0) != (count % 7 == 0)) ? State.Closed : State.Open;*/
                break;

            case 9: //white
                count = 0;
                count2 = 0;
                StartCoroutine(delayWhiteDetection(count, count2, eye, allEyes));
                /**count = 0;
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
                desiredState = (count > count2) ? State.Open : State.Closed;*/
                break;
        }

        return (desiredState == State.Open) ? 'O' : 'C';
    }

    private IEnumerator delayWhiteDetection(int count, int count2, string eye, List<string> allEyes)
    {
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < allEyes.Count; i++)
        {
            if (eyeType(allEyes[i]) == eyeType(eye) && allEyes[i][0] != '9')
            {
                if (info.Modules[i].getNeededState() == 'C')
                {
                    count += 1;
                }
                else
                {
                    count2 += 1;
                }
            }
        }
        string typeName3 = "";
        switch (type)
        {
            case 0: typeName3 = "Normal"; break;
            case 1: typeName3 = "Special"; break;
            case 2: typeName3 = "Small"; break;
        }
        if (count > count2)
        {
            neededState = 'O';
            Debug.LogFormat("[The Stare #{0}] White Eye Rule: There are more non-white eyes desiring to be closed ({1}) rather than opened ({2}) of the type {3}.", _moduleId, count, count2, typeName3);
        }
        else
        {
            neededState = 'C';
            Debug.LogFormat("[The Stare #{0}] White Eye Rule: There are not more non-white eyes desiring to be closed ({1}) rather than opened ({2}) of the type {3}.", _moduleId, count, count2, typeName3);
        }
        moduleName = moduleName.Substring(0, 3) + neededState;
        Debug.LogFormat("[The Stare #{0}] This makes the Eye's desired state: {1}", _moduleId, (neededState == 'C' ? "closed" : "open"));
    }

    private char getNeededState()
    {
        return neededState;
    }

    //twitch plays
    private bool timeIsValid(string s)
    {
        Regex stareRegex1 = new Regex(@"[0-9][0-9]");
        Regex stareRegex2 = new Regex(@"[0-9][:][0-9][0-9]");
        Regex stareRegex3 = new Regex(@"[0-9][0-9][:][0-9][0-9]");
        Regex stareRegex4 = new Regex(@"[0-9][:][0-9][0-9][:][0-9][0-9]");
        Match match = stareRegex1.Match(s);
        Match match2 = stareRegex2.Match(s);
        Match match3 = stareRegex3.Match(s);
        Match match4 = stareRegex4.Match(s);
        if(match.Success && s.Length == 2)
        {
            return true;
        }
        else if(match2.Success && s.Length == 4)
        {
            return true;
        }else if (match3.Success && s.Length == 5)
        {
            return true;
        }else if (match4.Success && s.Length == 7)
        {
            return true;
        }
        return false;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} toggle <time> [Switches the state of the eye when the bomb's timer is the specified time] | !{0} toggle [Switches the state of the eye (the last toggle for submitting needs no time parameter)] | !{0} colorblind [Toggle colorblind mode] | Valid time formats are ##, #:##, ##:##, and #:##:##";
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
            yield return null;
            if(parameters.Length == 2)
            {
                if (timeIsValid(parameters[1]))
                {
                    if(parameters[1].Length == 2)
                    {
                        parameters[1] = "00:" + parameters[1];
                    }
                    else if (parameters[1].Length == 4)
                    {
                        parameters[1] = "0" + parameters[1];
                    }
                    else if (parameters[1].Length == 7)
                    {
                        int temp = 0;
                        int temp2 = 0;
                        int.TryParse(parameters[1].Substring(0, 1), out temp);
                        temp *= 60;
                        int.TryParse(parameters[1].Substring(2, 2), out temp2);
                        temp += temp2;
                        string tem = "" + temp;
                        tem += parameters[1].Substring(4, 3);
                        parameters[1] = tem;
                    }
                    yield return "sendtochat Eye toggle time set for '" + parameters[1] + "'";
                    if ((int)bombInfo.GetTime() < 60)
                    {
                        int temp = 0;
                        int.TryParse(parameters[1].Substring(parameters[1].Length-2, 2), out temp);
                        while ((int)bombInfo.GetTime() != temp) yield return "trycancel The Eye's toggle was cancelled due to a cancel request.";
                    }
                    else
                    {
                        while (!bombInfo.GetFormattedTime().Equals(parameters[1])) yield return "trycancel The Eye's toggle was cancelled due to a cancel request.";
                    }
                    module.OnInteract();
                }
                else
                {
                    yield return "sendtochaterror The specified time to toggle the eye at '"+parameters[1]+"' is invalid!";
                }
            }
            else
            {
                yield return "sendtochaterror Too many parameters!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        List<string> stares = new List<string>();
        Regex stareRegex = new Regex(@"[1-9][1-9][OC][XOC]");
        foreach (StareModule mod in info.Modules)
        {
            Match match = stareRegex.Match(mod.moduleName);
            if (match.Success)
            {
                stares.Add(mod.moduleName);
            }
        }
        while (moduleName[2] != moduleName[3] || !localalt)
        {
            while (!ToggleTime(moduleName, stares, false))
            {
                yield return true;
                yield return new WaitForSeconds(0.01f);
            }
            module.OnInteract();
        }
        bool announce = false;
        while (coRunning)
        {
            if(!announce && stares.Count != 1)
            {
                announce = true;
                tpautosolvetext.SetActive(true);
            }
            yield return true;
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(0.1f);
        module.OnInteract();
    }
}