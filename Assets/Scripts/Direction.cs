using System;
using UnityEngine;

public enum Dir4 {
    None,
    N,
    S,
    E,
    W
}

public class Direction {
    public static Dir4 CalculateDir4(Vector2 clickStart, Vector2 clickEnd, float min = 1F) {
        double angle = Mathf.Atan2(clickEnd.y - clickStart.y, clickEnd.x - clickStart.x) * 180 / Mathf.PI;
        var distance = Math.Abs(Vector2.Distance(clickStart, clickEnd));
        if (distance < min) {
            return Dir4.None;
        }

        if (angle < 45F && angle > -45F) {
            return Dir4.E;
        }

        if (angle > 45F && angle < 135F) {
            return Dir4.N;
        }

        if (angle > 135F || angle < -135F) {
            return Dir4.W;
        }

        return Dir4.S;
    }
}