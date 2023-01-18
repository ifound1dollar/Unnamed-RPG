using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartySelectOptions : MonoBehaviour
{
    [SerializeField] Button swapInButton;
    [SerializeField] Button detailsButton;
    [SerializeField] Button reorderButton;
    [SerializeField] Button cancelButton;

    public Button SwapInButton { get { return swapInButton; } }
    public Button DetailsButton { get { return detailsButton; } }
    public Button ReorderButton { get { return reorderButton; } }
    public Button CancelButton { get { return cancelButton; } }
}
