using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PartyButton : MonoBehaviour, ISelectHandler
{
    [SerializeField] PartyMenu partyMenu;
    [SerializeField] Button parentButton;
    [SerializeField] int buttonIndex;

    public void OnSelect(BaseEventData eventData)
    {
        //if this button non-interactable, must find valid button to select
        if (!parentButton.interactable)
        {
            //must delay before selecting another button, so waits for end of frame here
            StartCoroutine(SelectButton());
        }
        else
        {
            if (buttonIndex == -1)
            {
                partyMenu.CoverDetails();
            }
            else
            {
                partyMenu.CurrButtonIndex = buttonIndex;

                //if details focused already, also show details of this character
                if (partyMenu.DetailsFocused)
                {
                    partyMenu.ShowDetails();
                }
            }
        }
    }

    IEnumerator SelectButton()
    {
        yield return new WaitForEndOfFrame();

        //if BackButtonJumped, then was sitting on back button, so select first
        if (partyMenu.BackButtonJumped)
        {
            partyMenu.SelectLastCharButton();
        }
        //else valid button last selected, so auto-select back button
        else
        {
            partyMenu.SelectCorrectBackButton();
        }
    }
}
