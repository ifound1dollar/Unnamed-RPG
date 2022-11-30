using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PartyButton : MonoBehaviour, ISelectHandler
{
    [SerializeField] PartyMenu partyMenu;
    [SerializeField] int buttonIndex;

    public void OnSelect(BaseEventData eventData)
    {
        //buttonIndex is -1 for the back button, which will simply hide the details panel
        if (buttonIndex != -1)
        {
            partyMenu.ShowDetails(buttonIndex);
        }
        else
        {
            partyMenu.HideDetails();
        }
    }
}
