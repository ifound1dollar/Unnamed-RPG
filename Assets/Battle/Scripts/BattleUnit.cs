using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] Image image;

    Vector3 origPos;
    Color origColor;

    public Image Image { get { return image; } }
    public bool IsPlayer { get; set; }

    private void Awake()
    {
        origPos = transform.localPosition;
        image.gameObject.SetActive(false);
    }

    public void PlayStartAnimation()
    {
        image.gameObject.SetActive(true);

        if (IsPlayer)
        {
            image.transform.localPosition = new(transform.localPosition.x - 500, transform.localPosition.y);
        }
        else
        {
            image.transform.localPosition = new(transform.localPosition.x + 500, transform.localPosition.y);
        }

        image.transform.DOLocalMoveX(origPos.x, 1.0f);
    }

    public void PlaySlainAnimation()
    {
        if (IsPlayer)
        {
            image.transform.DOLocalMoveY(transform.localPosition.y - 500, 1.0f);
        }
        else
        {
            image.transform.DOLocalMoveY(transform.localPosition.y - 500, 1.0f);
        }

        image.gameObject.SetActive(false);
    }

    public void PlayAttackAnimation()
    {
        //universal attack animation (not specific to Ability, sometimes not called)
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        if (IsPlayer)
        {
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + 50f, 0.25f));
        }
        else
        {
            sequence.Append(image.transform.DOLocalMoveX(origPos.x - 50f, 0.25f));
        }

        sequence.Append(image.transform.DOLocalMoveX(origPos.x, 0.25f));
        //apparently auto-plays sequence
    }

    public void PlayShakingAnimation()
    {
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        if (IsPlayer)
        {
            //PUT THESE IN LOOP LATER
            //if player, move left first (half distance, so half the time) then back right
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + -10.0f, 0.10f));
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + 10.0f, 0.20f));
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + -10.0f, 0.20f));
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + 10.0f, 0.20f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + -15.0f, 0.16f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + 15.0f, 0.16f));
        }
        else
        {
            //else move right first
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + 10.0f, 0.10f));
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + -10.0f, 0.20f));
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + 10.0f, 0.20f));
            sequence.Append(image.transform.DOLocalMoveX(origPos.x + -10.0f, 0.20f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + 15.0f, 0.16f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + -15.0f, 0.16f));
        }

        sequence.Append(image.transform.DOLocalMoveX(origPos.x, 0.10f));
    }

    public void PlayDamagedAnimation()
    {
        //universal damage animation
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        sequence.Append(image.DOColor(Color.gray, 0.1f));
        sequence.Append(image.DOColor(Color.white, 0.1f));
    }
}
