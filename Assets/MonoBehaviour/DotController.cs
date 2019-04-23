using System;
using UnityEditor.SceneManagement;
using UnityEngine;

public class DotController : MonoBehaviour {
    public GameObject hintUp;
    public GameObject hintDown;
    public GameObject hintRight;
    public GameObject hintLeft;
    public GameObject explosionPrefab;
    public GameObject debugText;

    [NonSerialized] public Vector2 target;
    [NonSerialized] public bool stopped;
    [NonSerialized] public BoardController board;
    [NonSerialized] public DotType dotType;

    private Vector2 clickStart;
    private Vector2 clickEnd;
    private Camera cam;
    private float speed = 13F;

    public int dot {
        get { return dotType.number; }
    }

    public int x {
        get { return (int)target.x; }
        private set { }
    }

    public int y {
        get { return (int)target.y; }
        private set { }
    }

    public bool destroyed { get; private set; }

    public void OnValidate() {
        ConfigureTextMeshSortingLayer();
    }

    private void Awake() {
        cam = Camera.main;
        ConfigureTextMeshSortingLayer();
    }

    public void ConfigureTextMeshSortingLayer() {
        var meshRenderer = debugText.GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = "dots";
        meshRenderer.sortingOrder = 1;
    }

    public void SetDebugText(string text) {
        debugText.SetActive(true);
        debugText.GetComponent<TextMesh>().text = text;
    }

    public void ClearDebugText() {
        debugText.SetActive(false);
        debugText.GetComponent<TextMesh>().text = "";
    }

    public void Configure(BoardController board, int x, int y, DotType dotType) {
        ChangeTarget(x, y);
        transform.position = target;
        this.board = board;
        transform.parent = board.gameObject.transform;
        SetType(dotType);

    }

    public void SetType(DotType dotType) {
        this.dotType = dotType;
        GetComponent<SpriteRenderer>().color = dotType.color;
        GetComponent<SpriteRenderer>().sprite = dotType.sprite;
    }

    private void OnMouseDown() {
        clickStart = cam.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp() {
        clickEnd = cam.ScreenToWorldPoint(Input.mousePosition);
        Dir4 dir4 = Direction.CalculateDir4(clickStart, clickEnd, 0.4F);
        switch (dir4) {
            case Dir4.None:
                board.UserClicks(this);
                break; 
            case Dir4.N:
                board.UserMoves(this, x, y + 1);
                break;
            case Dir4.S:
                board.UserMoves(this, x, y - 1);
                break;
            case Dir4.E:
                board.UserMoves(this, x + 1, y);
                break;
            case Dir4.W:
                board.UserMoves(this, x - 1, y);
                break;
        }
    }


    public void MarkToBeDestroyed() {
        destroyed = true;
    }

    public void NiceDestroy() {
        if (!destroyed) return;
        gameObject.SetActive(false);

        ClearDebugText();

        GameObject explosion = Instantiate(explosionPrefab, new Vector3(transform.position.x,
            transform.position.y, -1F), Quaternion.identity);
        explosion.GetComponent<Renderer>().sortingLayerName = "particles";
        Destroy(explosion, 0.2F);
    }


    public void Recycle() {
        destroyed = false;

        ClearDebugText();
        
        gameObject.SetActive(true);
    }

    public void FallTo(int x, int y, int yFrom) {
        transform.position = new Vector3(x, yFrom, 0F);
        speed = 13F;
        ChangeTarget(x, y);
    }

    public void MoveSlowly(int x, int y) {
        speed = 4F;
        ChangeTarget(x, y);
    }

    public void FallTo(int x, int y) {
        speed = 13F;
        ChangeTarget(x, y);
    }

    private void ChangeTarget(int x, int y) {
        target = new Vector2(x, y);
        name = "Dot(" + x + "," + y + ")";
        stopped = false;
    }

    void Update() {
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
}