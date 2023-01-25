using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PartyChar : MonoBehaviour
{
    [SerializeField] Transform optionsAnchor;

    [Header("Data display fields")]
    [SerializeField] TMP_Text _name;


    public Transform OptionsAnchor { get { return optionsAnchor; } }
    public TMP_Text Name { get { return _name; } }
}
