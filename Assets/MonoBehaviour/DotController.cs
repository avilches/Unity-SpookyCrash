using System;
using UnityEditor.SceneManagement;
using UnityEngine;

public class DotController : MonoBehaviour {
    private Vector2 clickStart;
    private Vector2 clickEnd;

    private Camera cam;

    [NonSerialized] public Vector2 target;
    [NonSerialized] public bool stopped;
    [NonSerialized] public BoardController board;
    [NonSerialized] public DotType dotType;

    public int dot {
        get { return dotType.number; }
    }

    public void SetType(DotType dotType) {
        this.dotType = dotType;
        GetComponent<SpriteRenderer>().color = dotType.color;
        GetComponent<SpriteRenderer>().sprite = dotType.sprite;
    }

    [NonSerialized] public bool destroyed;
    [NonSerialized] public float speed = 13F;

    public GameObject hintUp;
    public GameObject hintDown;
    public GameObject hintRight;
    public GameObject hintLeft;

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
    

    public void NiceDestroy() {
        destroyed = true;
        gameObject.SetActive(false);
        
        GameObject explosion = Instantiate(explosionPrefab, new Vector3(transform.position.x,
            transform.position.y, -1F), Quaternion.identity);
        explosion.GetComponent<Renderer>().sortingLayerName = "particles";
        Destroy(explosion, 0.4F);
    }

    public void Recycle() {
        destroyed = false;
        gameObject.SetActive(true);
    }

    public void FallTo(int x, int y, int yFrom) {
        transform.position = new Vector3(x, yFrom, 0F);
        speed = 13F;
        target = new Vector2(x, y);
        name = "Dot(" + x + "," + y + ")";
        stopped = false;
    }

    public void FallTo(int x, int y) {
        speed = 13F;
        target = new Vector2(x, y);
        name = "Dot(" + x + "," + y + ")";
        stopped = false;
    }

    public void MoveSlowly(int x, int y) {
        speed = 4F;
        target = new Vector2(x, y);
        name = "Dot(" + x + "," + y + ")";
        stopped = false;
    }

    private void Update() {
        if (!stopped) {
            transform.position = Vector2.MoveTowards(transform.position, target, Time.deltaTime * speed);
            stopped = Math.Abs(Vector2.Distance(transform.position, target)) < 0.01F;
            if (stopped) {
                transform.position = target;
            }
        }
    }

    public void DisableHints() {
        hintLeft.SetActive(false);
        hintRight.SetActive(false);
        hintUp.SetActive(false);
        hintDown.SetActive(false);
    }

    public void ShowHintLeft() {
        DisableHints();
        hintLeft.SetActive(true);
    }

    public void ShowHintRight() {
        DisableHints();
        hintRight.SetActive(true);
    }

    public void ShowHintUp() {
        DisableHints();
        hintUp.SetActive(true);
    }

    public void ShowHintDown() {
        DisableHints();
        hintDown.SetActive(true);
    }

    public void ConfigureBoard(BoardController board) {
        this.board = board;
        transform.parent = board.gameObject.transform;

    }
}