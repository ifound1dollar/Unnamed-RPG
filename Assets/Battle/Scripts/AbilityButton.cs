using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityButton : MonoBehaviour, ISelectHandler
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
            //if back button, temporarily cover ability info; else show ability info
            if (buttonIndex == -1)
            {
                partyMenu.CoverAbilityInfo();
            }
            else
            {
                partyMenu.ShowAbilityInfo(buttonIndex);
            }
        }
    }

    IEnumerator SelectButton()
    {
        yield return new WaitForEndOfFrame();

        //if BackButtonJumped, then was sitting on back button, so select first
        if (partyMenu.BackButtonJumped)
        {
            partyMenu.SelectCorrectNormalButton();
        }
        //else valid button last selected, so auto-select back button
        else
        {
            partyMenu.SelectCorrectBackButton();
        }
    }
}
