using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PartyChar : MonoBehaviour
{
    [SerializeField] TMP_Text _name;

    public TMP_Text Name { get { return _name; } }
}
