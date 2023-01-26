using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] TMP_Text dialogText;

    int dialogSpeed;

    int currentLine;
    Dialog dialog;
    bool textAnimating = false;


    //singleton making this DialogManager accessible anywhere, set in Setup
    public static DialogManager Instance { get; private set; }
    public void Setup(int dialogSpeed)
    {
        Instance = this;
        this.dialogSpeed = dialogSpeed;
    }


    public IEnumerator ShowDialog(Dialog dialog)
    {
        //wait for end of frame because Update() should be run first
        yield return new WaitForEndOfFrame();

        GameState.InDialog = true;
        this.dialog = dialog;
        currentLine = 0;

        dialogBox.SetActive(true);
        StartCoroutine(DialogSet(dialog.Lines[0]));
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




    private void Update()
    {
        if (!GameState.InDialog || textAnimating)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (currentLine < dialog.Lines.Count - 1)
            {
                currentLine++;
                StartCoroutine(DialogSet(dialog.Lines[currentLine]));
            }
            else
            {
                currentLine = 0;
                GameState.InDialog = false;
                gameObject.SetActive(false);
            }
        }
    }
}
