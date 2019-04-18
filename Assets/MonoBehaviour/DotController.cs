using System;
using UnityEngine;

public class DotController : MonoBehaviour {
    private Vector2 clickStart;
    private Vector2 clickEnd;

    private Camera cam;

    [NonSerialized] public Vector2 target;
    [NonSerialized] public bool stopped;
    [NonSerialized] public BoardController board;
    [NonSerialized] public int dot;
    [NonSerialized] public bool destroyed;
    [NonSerialized] public float speed = 13F;

    public GameObject explosionPrefab;
    

    private void Awake() {
        cam = Camera.main;
    }

    private void OnMouseDown() {
        clickStart = cam.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp() {
        clickEnd = cam.ScreenToWorldPoint(Input.mousePosition);
        Dir4 dir4 = Direction.CalculateDir4(clickStart, clickEnd, 0.4F);
        switch (dir4) {
            case Dir4.N:
                board.UserMoves(this, (int) target.x, (int) target.y + 1);
                break;
            case Dir4.S:
                board.UserMoves(this, (int) target.x, (int) target.y - 1);
                break;
            case Dir4.E:
                board.UserMoves(this, (int) target.x + 1, (int) target.y);
                break;
            case Dir4.W:
                board.UserMoves(this, (int) target.x - 1, (int) target.y);
                break;
        }
    }

    public void Destroy() {
        destroyed = true;
        GameObject explosion = Instantiate(explosionPrefab, new Vector3(transform.position.x,
            transform.position.y, -1F), Quaternion.identity);
        explosion.GetComponent<Renderer>().sortingLayerName = "particles";
        Destroy(explosion, 0.4F);
        Destroy(gameObject, 0.4F);
        // Destroy(gameObject);
    }


    public void FallTo(float x, float y) {
        SetTargetAndSpeed(x, y, 13F);
    }

    public void SetTarget(float x, float y) {
        SetTargetAndSpeed(x, y, 4F);
    }

    public void SetTargetAndSpeed(float x, float y, float speed) {
        this.speed = speed;
        // Debug.Log("(" + target.x + "," + target.y + ") -> " + x + " " + y);

        target = new Vector2(x, y);
        name = "Dot(" + x + "," + y + ")";
        stopped = false;
    }

    private void Update() {
        if (!stopped) {
            transform.position = Vector2.MoveTowards(transform.position, target, Time.deltaTime * speed);
            stopped = Math.Abs(Vector2.Distance(transform.position, target)) < 0.001F;
        }
    }
}