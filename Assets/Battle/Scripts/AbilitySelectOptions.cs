using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilitySelectOptions : MonoBehaviour
{
    [SerializeField] Button reorderButton;
    [SerializeField] Button removeButton;

    public Button ReorderButton { get { return reorderButton; } }
    public Button RemoveButton { get { return removeButton; } }
}
