using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Custom Tile")]
public class CustomTile : Tile
{
    public enum StairType { None, Right, Left }
    public enum EnterDirection { Both, Right, Left }


    [Header("Custom Tile Attributes")]
    [SerializeField] StairType stairDirection;
    [SerializeField] EnterDirection fromDirection;

    public StairType StairDirection { get => stairDirection; }
    public EnterDirection FromDirection { get => fromDirection;}
}
