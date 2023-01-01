using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleType
{
    None,
    Nature,
    Water,
    Fire,
    Earth,
    Air,
    Electric,
    Ice,
    Metal,
    Vital,
    Light,
    Dark,
    Runic,
    Void
}


public static class Effectiveness
{
    public static float GetMultiplier(BattleType userType, BattleType targetType1, BattleType targetType2)
    {
        //simple floating-point multiplication to get preliminary value (rounding errors are fine)
        float value = CalcSingle(userType, targetType1) * CalcSingle(userType, targetType2);

        //clamp between 3.0 and 0.33, unless 0
        if (value > 3.0)
        {
            value = 3.0f;
        }
        else if (value < 0.33 && value != 0)
        {
            value = 1/3.0f;
        }

        return value;
    }
    static float CalcSingle(BattleType userType, BattleType targetType)
    {
        switch (userType)
        {
            case BattleType.Nature:     { return Nature(targetType); }
            case BattleType.Water:      { return Water(targetType); }
            case BattleType.Fire:       { return Fire(targetType); }
            case BattleType.Earth:      { return Earth(targetType); }
            case BattleType.Air:        { return Air(targetType); }
            case BattleType.Electric:   { return Electric(targetType); }
            case BattleType.Ice:        { return Ice(targetType); }
            case BattleType.Metal:      { return Metal(targetType); }
            case BattleType.Vital:      { return Vital(targetType); }
            case BattleType.Light:      { return Light(targetType); }
            case BattleType.Dark:       { return Dark(targetType); }
            case BattleType.Runic:      { return Runic(targetType); }
            case BattleType.Void:       { return Void(targetType); }
        }

        return 1.0f;    //None always defaults to 1x
    }

    static float Nature(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;    //all others, including None, default to 1x
    }
    static float Water(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Fire(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Earth(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Air(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Electric(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Ice(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Metal(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Vital(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }
            case BattleType.Dark: { return 2.0f; }
        }

        return 1.0f;
    }
    static float Light(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Dark(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Runic(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
    static float Void(BattleType targetType)
    {
        switch (targetType)
        {
            case BattleType.Water: { return 2.0f; }
            case BattleType.Fire: { return 0.5f; }

        }

        return 1.0f;
    }
}
