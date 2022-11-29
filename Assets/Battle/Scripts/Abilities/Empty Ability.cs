using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyAbility : Ability
{
    public EmptyAbility()
    {
        Name = "EMPTY";
    }
    public override void UseAbility()
    {
        Debug.Log("Tried to use Empty Ability.");
    }
}
