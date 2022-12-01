using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BadAbility : Ability
{
    public BadAbility()
    {
        Name = "MISSING";
    }
    public override void UseAbility()
    {
        Debug.Log("Tried to use Bad Ability.");
    }
}
