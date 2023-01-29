using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] Image belowImage;
    [SerializeField] Image unitImage;
    [SerializeField] Image aboveImage;

    Vector3 origPos;
    Color origColor;

    public Image Image { get { return unitImage; } }
    public bool IsPlayer { get; set; }

    private void Awake()
    {
        origPos = transform.localPosition;
        unitImage.gameObject.SetActive(false);
    }

    public void PlayEnterAnimation()
    {
        if (IsPlayer)
        {
            transform.localPosition = new(origPos.x - 500, origPos.y);
        }
        else
        {
            transform.localPosition = new(origPos.x + 500, origPos.y);
        }

        unitImage.gameObject.SetActive(true);
        transform.DOLocalMoveX(origPos.x, 1.0f);
    }

    public void PlayExitAnimation()
    {
        if (IsPlayer)
        {
            transform.DOLocalMoveX(origPos.x - 500, 1.0f);
        }
        else
        {
            transform.DOLocalMoveX(origPos.x + 500, 1.0f);
        }
    }

    public void PlaySlainAnimation()
    {
        if (IsPlayer)
        {
            transform.DOLocalMoveY(origPos.y - 500, 1.0f);
        }
        else
        {
            transform.DOLocalMoveY(origPos.y - 500, 1.0f);
        }
    }

    public void PlayAttackAnimation()
    {
        //universal attack animation (not specific to Ability, sometimes not called)
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        if (IsPlayer)
        {
            sequence.Append(transform.DOLocalMoveX(origPos.x + 50f, 0.20f));
        }
        else
        {
            sequence.Append(transform.DOLocalMoveX(origPos.x - 50f, 0.20f));
        }

        sequence.Append(transform.DOLocalMoveX(origPos.x, 0.20f));
        //apparently auto-plays sequence
    }

    public void PlayShakingAnimation()
    {
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        if (IsPlayer)
        {
            //PUT THESE IN LOOP LATER
            //if player, move left first (half distance, so half the time) then back right
            sequence.Append(transform.DOLocalMoveX(origPos.x + -10.0f, 0.10f));
            sequence.Append(transform.DOLocalMoveX(origPos.x + 10.0f, 0.20f));
            sequence.Append(transform.DOLocalMoveX(origPos.x + -10.0f, 0.20f));
            sequence.Append(transform.DOLocalMoveX(origPos.x + 10.0f, 0.20f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + -15.0f, 0.16f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + 15.0f, 0.16f));
        }
        else
        {
            //else move right first
            sequence.Append(transform.DOLocalMoveX(origPos.x + 10.0f, 0.10f));
            sequence.Append(transform.DOLocalMoveX(origPos.x + -10.0f, 0.20f));
            sequence.Append(transform.DOLocalMoveX(origPos.x + 10.0f, 0.20f));
            sequence.Append(transform.DOLocalMoveX(origPos.x + -10.0f, 0.20f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + 15.0f, 0.16f));
            //sequence.Append(image.transform.DOLocalMoveX(origPos.x + -15.0f, 0.16f));
        }

        sequence.Append(transform.DOLocalMoveX(origPos.x, 0.10f));
    }

    public void PlayKnockbackAnimation()
    {
        //universal attack animation (not specific to Ability, sometimes not called)
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        if (IsPlayer)
        {
            sequence.Append(transform.DOLocalMoveX(origPos.x - 50f, 0.4f));
        }
        else
        {
            sequence.Append(transform.DOLocalMoveX(origPos.x + 50f, 0.4f));
        }

        sequence.Append(transform.DOLocalMoveX(origPos.x, 0.4f));
    }

    public void PlayDamagedAnimation()
    {
        //universal damage animation
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        sequence.Append(unitImage.DOColor(Color.gray, 0.1f));
        sequence.Append(unitImage.DOColor(Color.white, 0.1f));
    }
}
