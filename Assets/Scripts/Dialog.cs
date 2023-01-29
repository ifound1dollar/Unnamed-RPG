using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class Dialog
{
    [TextArea()]
    [SerializeField] string line;
    [SerializeField] bool isConditionalOnly;
    [SerializeField] bool beginsBattle;

    [Header("Flags : dot notation (ex. 0.0)")]
    [SerializeField] string checkFlag;
    [SerializeField] int flagRerouteIndex;
    [SerializeField] string setFlag;

    [Header("Option input")]
    [SerializeField] bool requiresInput;
    [SerializeField] string option1Text;
    [SerializeField] int option1Index;
    [SerializeField] string option2Text;
    [SerializeField] int option2Index;


    public string Line              { get { return line; } }
    public bool IsConditionalOnly   { get { return isConditionalOnly; } }
    public bool BeginsBattle        { get { return beginsBattle; } }

    public string CheckFlag         { get { return checkFlag; } }
    public int FlagRerouteIndex     { get { return flagRerouteIndex; } }
    public string SetFlag           { get { return setFlag; } }

    public bool RequiresInput       { get { return requiresInput; } }
    public string Option1Text       { get { return option1Text; } }
    public int Option1Index         { get { return option1Index; } }
    public string Option2Text       { get { return option2Text; } }
    public int Option2Index         { get { return option2Index; } }
}
