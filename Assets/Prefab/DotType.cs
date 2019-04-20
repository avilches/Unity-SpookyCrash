using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "DotType", menuName = "DotType", order = 1)]
public class DotType : ScriptableObject {
    public int number;
    public Sprite sprite;
    public Color color;
}
