using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] TMP_Text dialogText;
    [SerializeField] Button[] optionButtons;
    [SerializeField] TMP_Text[] optionTexts;

    int dialogSpeed;

    int currentLine;
    bool textAnimating = false;
    bool awaitingBattle = false;

    NPCData npcData;


    public void Setup(int dialogSpeed)
    {
        this.dialogSpeed = dialogSpeed;
    }


    /// <summary>
    /// Initially shows dialog, sets gameObject active, and sets GameState to InDialog
    /// </summary>
    /// <param name="dialogs"></param>
    /// <returns></returns>
    public IEnumerator BeginDialog(NPCData npcData)
    {
        //wait for end of frame because Update() should be run first
        yield return new WaitForEndOfFrame();

        GameManager.Instance.InDialog = true;
        this.npcData = npcData;
        awaitingBattle = false;

        dialogBox.SetActive(true);
        StartCoroutine(HandleDialog(lineIndex: 0));
    }

    /// <summary>
    /// Sets currentLine and shows dialog and input buttons based on context
    /// </summary>
    /// <param name="lineIndex">Index of dialog object being accessed</param>
    /// <returns></returns>
    public IEnumerator HandleDialog(int lineIndex)
    {
        while (GameManager.Instance.PersistentData.Flags.ContainsKey(npcData.dialogs[lineIndex].CheckFlag))
        {
            //will not contain empty string; if contains actual flag, move onto reroute index
            lineIndex = npcData.dialogs[lineIndex].FlagRerouteIndex;
        }

        Dialog dialog = npcData.dialogs[lineIndex];
        currentLine = lineIndex;

        if (dialog.RequiresInput)
        {
            //IN THE FUTURE, WILL CHECK IF NOT EMPTY STRING IN OPTION TEXTS BEFORE SHOWING
            optionTexts[0].text = dialog.Option1Text;
            optionTexts[1].text = dialog.Option2Text;

            //wait for dialog completion before showing buttons
            yield return DialogSet(dialog.Line);

            optionButtons[0].gameObject.SetActive(true);
            optionButtons[1].gameObject.SetActive(true);
            optionButtons[0].Select();
        }
        else
        {
            optionButtons[0].gameObject.SetActive(false);
            optionButtons[1].gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);

            yield return DialogSet(dialog.Line);
        }

        if (dialog.SetFlag != "")
        {
            //if valid flag, attempt to set flag by adding to dictionary
            GameManager.Instance.PersistentData.Flags.TryAdd(dialog.SetFlag, true);
        }

        if (dialog.BeginsBattle)
        {
            //will start battle from NPCData once dialog ends
            awaitingBattle = true;
        }
    }

    /// <summary>
    /// Sets dialog text to the text argument
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public IEnumerator DialogSet(string text)
    {
        //set text TRANSPARENT (scrolling text will move a character down a line in the middle
        //  of a word if it reaches text box border, which looks weird to the player; starting
        //  with the text already sized correctly will prevent this weird behavior)

        dialogText.text = "<color=#ffffff00>" + text + "</color>";  //17, 8

        textAnimating = true;
        yield return ScrollText(text);
        textAnimating = false;
    }

    /// <summary>
    /// Displays dialog text one character at a time
    /// </summary>
    /// <param name="text">Text to be displayed one character at a time</param>
    /// <param name="startIndex">Index to begin displaying text from, inclusive (append)</param>
    /// <returns></returns>
    IEnumerator ScrollText(string text, int startIndex = 0)
    {
        ///This method declares a StringBuilder with the recently set dialogText. It removes
        /// the first DISPLAYED character in the original dialogText (after the color
        /// definition, which uses 17 characters) then inserts the character at this index
        /// before the color definition (thus is the default, visible color).
        ///Parameter startIndex is optional and is for appending to existing text. If defined,
        /// all text before startIndex will already be visible, and only the characters after
        /// will be displayed one by one.
        ///This gives the illusion that the text is being printed one character at a time,
        /// but the text already exists transparently for text box sizing purposes.

        StringBuilder temp = new(dialogText.text);

        for (int i = 0; i < text.Length; i++)
        {
            temp.Remove(i + startIndex + 17, 1);    //remove AT index i, 1 character
            temp.Insert(i + startIndex, text[i]);   //insert AFTER index i
            dialogText.text = temp.ToString();
            yield return new WaitForSeconds(1f / dialogSpeed);
        }

        //remove the now useless color definition (17 for beginning, 8 for end)
        dialogText.text = dialogText.text[..^25];
    }




    public void OnDialogButtonPress(int buttonIndex)
    {
        if (buttonIndex == 0)
        {
            StartCoroutine(HandleDialog(npcData.dialogs[currentLine].Option1Index));
        }
        else if (buttonIndex == 1)
        {
            StartCoroutine(HandleDialog(npcData.dialogs[currentLine].Option2Index));
        }
    }




    private void Update()
    {
        if (!GameManager.Instance.InDialog || textAnimating)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (currentLine < npcData.dialogs.Count - 1)
            {
                //only show next if does not require button input
                if (npcData.dialogs[currentLine].RequiresInput)
                {
                    return;
                }

                //if valid next index, handle dialog at that index
                if (npcData.dialogs[currentLine].DefaultNextIndex != -1)
                {
                    StartCoroutine(HandleDialog(npcData.dialogs[currentLine].DefaultNextIndex));
                    return;
                }
            }

            //if reaches here, then is at last line or no remaining valid dialog to show
            optionButtons[0].gameObject.SetActive(false);
            optionButtons[1].gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);

            GameManager.Instance.InDialog = false;
            gameObject.SetActive(false);

            //START BATTLE HERE INSTEAD OF WITHIN PLAYERCONTROLLER
            if (awaitingBattle)
            {
                GameManager.Instance.BeginBattle(npcData.battleParty, npcData.battleFlag);
                //USE NPCDATA STRUCT (BattleParty, battleFlag) to begin battle from game
            }
        }
    }
}
