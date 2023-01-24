using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Sprite[] statusImages;
    public Image statusHudImage;
    public TMP_Text hpText;
    public TMP_Text maxHpText;
    public Slider hpBackSlider;
    public Slider hpMainSlider;
    public TMP_Text enText;
    public TMP_Text maxEnText;
    public Slider enSlider;

    bool animating;
    int oldHP;
    int currHP;
    int maxHP;
    int currEN;
    int maxEN;

    float delay;

    public void SetHUD(BattleChar battleChar)
    {
        ///sets player HUD without any animations

        ///IMPORTANT: MUST ALSO SET STATUS EFFECT VISUAL

        //set basic string data
        nameText.text = battleChar.Name;
        levelText.text = battleChar.Level.ToString();
        hpText.text = battleChar.HP.ToString();
        maxHpText.text = battleChar.MaxHP.ToString();
        enText.text = battleChar.Energy.ToString();
        maxEnText.text = battleChar.MaxEnergy.ToString();

        //get sprite image from array and set transparency correctly
        statusHudImage.sprite = statusImages[(int)battleChar.StatusActive];
        if (battleChar.StatusActive != StatusEffect.None)
        {
            statusHudImage.color = new Color(255, 255, 255, 255);
        }
        else
        {
            statusHudImage.color = new Color(255, 255, 255, 0);
        }

        //set HP slider values
        hpBackSlider.value = hpMainSlider.value = (float)battleChar.HP / battleChar.MaxHP;

        //set EN slider value
        enSlider.value = (float)battleChar.Energy / battleChar.MaxEnergy;

        //assign values that are used for animations (below)
        currHP = battleChar.HP;
        maxHP = battleChar.MaxHP;
        currEN = battleChar.Energy;
        maxEN = battleChar.MaxEnergy;
    }
    public void UpdateHUD(BattleChar battleChar)
    {
        ///updates player HUD with animations
        
        ///IMPORTANT: MUST ALSO UPDATE STATUS EFFECT VISUAL

        //store previous data in variables and update HP and EN text right away
        oldHP = currHP;
        currHP = battleChar.HP;
        currEN = battleChar.Energy;

        hpText.text = currHP.ToString();
        enText.text = currEN.ToString();

        //get sprite image from array and set transparency correctly
        statusHudImage.sprite = statusImages[(int)battleChar.StatusActive];
        if (battleChar.StatusActive != StatusEffect.None)
        {
            statusHudImage.color = new Color(255, 255, 255, 255);
        }
        else
        {
            statusHudImage.color = new Color(255, 255, 255, 0);
        }

        //verify back slider at old value, then set main slider to real current value
        hpBackSlider.value = (float)oldHP / maxHP;
        hpMainSlider.value = (float)currHP / maxHP;
        enSlider.value = (float)currEN / maxEN;

        //set animating bool, which tells Update() to animate HP bar until at correct value
        animating = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!animating) { return; }

        if (delay < 0.2) { delay += Time.deltaTime; return; }

        //move back slider down by difference in old and current HP, will take 2/3 of a second
        hpBackSlider.value -= ((oldHP - currHP) * (Time.deltaTime * 1.5f)) / maxHP;

        //if taking damage, set animating to false once less than or equal to mainSlider
        if (oldHP - currHP >= 0)
        {
            if (hpBackSlider.value <= hpMainSlider.value)
            {
                hpBackSlider.value = hpMainSlider.value;
                animating = false;
                delay = 0f;
            }
        }
        //else if healing, set animating to false once greater than or equal to mainSlider
        else
        {
            if (hpBackSlider.value >= hpMainSlider.value)
            {
                hpBackSlider.value = hpMainSlider.value;
                animating = false;
                delay = 0f;
            }
        }
    }
}
