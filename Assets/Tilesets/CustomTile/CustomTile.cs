using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Custom Tile")]
public class CustomTile : Tile
{
    public enum StairType { None, Right, Left }


    [Header("Custom Tile Attributes")]
    [SerializeField] StairType stairs;

    public StairType Stairs { get => stairs; }
}
