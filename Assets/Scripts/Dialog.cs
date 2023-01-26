using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class Dialog
{
    [TextArea()]
    [SerializeField] List<string> lines;

    public List<string> Lines { get { return lines; } }
}
