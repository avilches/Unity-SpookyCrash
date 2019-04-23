using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class BoardController : MonoBehaviour {
    public GameObject dotPrefab;
    public DotType[] dotTypes;
    public int width;
    public int height;
    public DotController[,] matrix;

    private bool stopped = false;
    private bool destroying = false;

    private int maxHints = 3;

    // If hints is set to -1, new hints will be found (up to MAX_HINTS) in the next frame
    private int hints = -1;

    // Start is called before the first frame update
    void Start() {
        Assert.IsTrue(width >= 5, "Width should be 6 at least");
        CreateBoard();
    }

    public void LoadBoard() {
        // carga
        ClearHints();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                matrix[x, y].SetType(dotTypes[PlayerPrefs.GetInt(x + "." + y)]);
            }
        }

        ShowHints();
    }

    public void SaveBoard() {
        // Salvar
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                PlayerPrefs.SetInt(x + "." + y, matrix[x, y].dot);
            }
        }

        PlayerPrefs.Save();
    }

    public void CreateBoard() {
        CreateDots();
        ShowHints();
    }

    private void CreateDots() {
        DestroyBoard();
        matrix = new DotController[width, height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                CreateDotBoard(x, y);
            }
        }
    }

    private void CreateDotBoard(int x, int y) {
        if (y == 0) {
            // RULE!
            // This rule creates a line 0 with |?AAbA
            if (x == 1) {
                CreateNewDot(x, y, GetRandomTypeExcept(matrix[x - 1, y].dotType));
                matrix[x, y].SetDebugText("F");
                return;
            } else if (x == 2) {
                CreateNewDot(x, y, matrix[x - 1, y].dotType);
                matrix[x, y].SetDebugText("F");
                return;
            } else if (x == 4) {
                CreateNewDot(x, y, matrix[x - 2, y].dotType);
                matrix[x, y].SetDebugText("F");
                return;
            }
        }

        CreateNewDot(x, y);
    }

    private void DestroyBoard() {
        if (matrix == null) return;
        ClearHints();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Destroy(matrix[x, y].gameObject);
                matrix[x, y] = null;
            }
        }
    }

    private DotType GetRandomType() {
        return dotTypes[Random.Range(0, dotTypes.Length)];
    }

    private DotType GetRandomTypeExcept(DotType except) {
        DotType dotType = GetRandomType();
        if (except == null) {
            return dotType;
        }

        int p = dotType.number;
        if (p == except.number) {
            p = (p + 1) % dotTypes.Length;
        }

        return dotTypes[p];
    }

    private DotType GetRandomTypeExcept(MiniList<int> nop) {
        DotType dotType = GetRandomType();
        if (nop == null || nop.Empty) {
            return dotType;
        }

        int p = dotType.number;
        while (nop.Contains(p)) {
            p = (p + 1) % dotTypes.Length;
        }

        return dotTypes[p];
    }

    private void RecyleDot(DotController dot, int x, int y, int destroyedAmount) {
        dot.SetType(GetRandomType());
        dot.FallTo(x, y, y + destroyedAmount);
        dot.Recycle();
        matrix[x, y] = dot;
        stopped = false;
    }

    private void CreateNewDot(int x, int y, DotType dotType = null) {
        GameObject o = Instantiate(dotPrefab, new Vector3(x, y, 0F), Quaternion.identity);
        DotController dot = o.GetComponent<DotController>();
        if (dotType == null) {
            var nop = FindForbiddenColorsForPosition(x, y);
            dotType = GetRandomTypeExcept(nop);
        }

        dot.Configure(this, x, y, dotType);
        matrix[x, y] = dot;
        stopped = false;
    }

    private void FallTo(DotController dot, int x, int y) {
        matrix[x, y] = dot;
        matrix[x, y].FallTo(x, y);
    }


    private MiniList<int> FindForbiddenColorsForPosition(int x, int y) {
        MiniList<int> nop = new MiniList<int>(2);
        // Previous two dots in the same row are equals?
        if (x >= 2 && matrix[x - 1, y].dot == matrix[x - 2, y].dot) {
            nop.Add(matrix[x - 1, y].dot);
        }

        // Previous two dots in the same column are equals?
        if (y >= 2 && matrix[x, y - 1].dot == matrix[x, y - 2].dot) {
            nop.Add(matrix[x, y - 1].dot);
        }

        return nop;
    }

    public void UserMoves(DotController dot, int x, int y) {
        if (!stopped || destroying ||
            x < 0 || x >= width || y < 0 || y >= height) {
            return;
        }

        ClearHints();
        DotController other = matrix[x, y];
        SwapDots(dot, other);

        rollbackOne = rollbackOther = null;
        var calculateMatchesFrom = CalculateMatchesFor(dot.x, dot.y);
        if (calculateMatchesFrom.Empty) {
            var calculateMatchesTo = CalculateMatchesFor(other.x, other.y);
            if (calculateMatchesTo.Empty) {
                // Bad move! Set a rollback after the move
                rollbackOne = dot;
                rollbackOther = other;
            }
        }

        // Only for testing
        // validateCalculateMatches(dot, other);
    }

    private void validateCalculateMatches(DotController dot, DotController other) {
        var calculateMatchesDot = CalculateMatchesFor(dot.x, dot.y);
        var calculateMatches = MarkToBeDestroyedAndCalculateMatches().matches;
        foreach (DotController candidate in calculateMatchesDot) {
            if (!calculateMatches.Contains(candidate)) {
                throw new Exception("validateCalculateMatches. Match not found for dot");
            }
        }

        var calculateMatchesOther = CalculateMatchesFor(other.x, other.y);
        foreach (DotController candidate in calculateMatchesOther) {
            if (!calculateMatches.Contains(candidate)) {
                throw new Exception("validateCalculateMatches. Match not found for other");
            }
        }

        if (calculateMatchesDot.Count + calculateMatchesOther.Count != calculateMatches.Count) {
            throw new Exception("validateCalculateMatches. Wrong final count");
        }

    }

    private void SwapDots(DotController one, DotController other) {
        stopped = false;
        // Swap positions
        matrix[one.x, one.y] = other;
        matrix[other.x, other.y] = one;
        // Swap targets
        int tempX = one.x;
        int tempY = one.y;
        one.MoveSlowly(other.x, other.y);
        other.MoveSlowly(tempX, tempY);
    }

    private DotController rollbackOne;
    private DotController rollbackOther;


    // Update is called once per frame
    void Update() {
        if (destroying) return;
        if (!stopped) {
            // Dots are still moving...
            stopped = CalculateStopped();
            return;
        }

        // Dots stopped: new board or user moved (good move or bad move)
        if (rollbackOne != null) {
            // bad move!
            SwapDots(rollbackOne, rollbackOther);
            rollbackOne = rollbackOther = null;
            return;
        }

        // good move or still new board
        var matches = MarkToBeDestroyedAndCalculateMatches();
        if (matches.HasMatches()) {
            // matches!
            StartCoroutine(DestroyMatchesAndFill(matches));
        } else {
            if (hints == -1) {
                ClearHints();
                ShowHints();
            }
        }
    }

    private LinkedList<DotController> hintsToDisable = new LinkedList<DotController>();

    public void AddHintToDisable(DotController dot) {
        hintsToDisable.AddLast(dot);
    }

    private void ClearHints() {
        foreach (DotController dot in hintsToDisable) {
            dot.DisableHints();
        }

        hintsToDisable.Clear();
        hints = -1;
    }

    private int ShowHints() {
        hints = 0;
        if (hints == maxHints) return hints;
        for (int x = 0; x < width - 2; x++) {
            for (int y = 0; y < height; y++) {
                if (matrix[x, y].dot == matrix[x + 1, y].dot &&
                    matrix[x, y].dot != matrix[x + 2, y].dot) {
                    hints += CheckLastDotRow(x + 2, y, matrix[x, y].dot) ? 1 : 0;
                    if (hints == maxHints) return hints;
                } else if (matrix[x, y].dot != matrix[x + 1, y].dot &&
                           matrix[x + 1, y].dot == matrix[x + 2, y].dot) {
                    hints += CheckFirstDotRow(x, y, matrix[x + 2, y].dot) ? 1 : 0;
                    if (hints == maxHints) return hints;
                }
            }
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height - 2; y++) {
                if (matrix[x, y].dot == matrix[x, y + 1].dot &&
                    matrix[x, y].dot != matrix[x, y + 2].dot) {
                    hints += CheckFirstDotColumn(x, y + 2, matrix[x, y].dot) ? 1 : 0;
                    if (hints == maxHints) return hints;
                } else if (matrix[x, y].dot != matrix[x, y + 1].dot &&
                           matrix[x, y + 1].dot == matrix[x, y + 2].dot) {
                    hints += CheckLastDotColumn(x, y, matrix[x, y + 2].dot) ? 1 : 0;
                    if (hints == maxHints) return hints;
                }
            }
        }

        return hints;
    }

    private bool CheckLastDotRow(int x, int y, int p) {
        // [x] [x] [?]
        // We have to check if dots up, down or right of [?] is == p
        return ShowHintLeft(x + 1, y, p) || // right dot
               ShowHintUp(x, y - 1, p) || // down dot
               ShowHintDown(x, y + 1, p); // up dot
    }

    private bool CheckFirstDotRow(int x, int y, int p) {
        // [?] [x] [x] 
        // We have to check if dots up, down or left of [?] is == p
        return ShowHintRight(x - 1, y, p) || // left dot
               ShowHintUp(x, y - 1, p) || // down dot
               ShowHintDown(x, y + 1, p); // up dot
    }

    private bool CheckFirstDotColumn(int x, int y, int p) {
        // [?]
        // [x]
        // [x] 
        // We have to check if dots up, left or right of [?] is == p
        return ShowHintRight(x - 1, y, p) || // left dot
               ShowHintLeft(x + 1, y, p) || // right dot
               ShowHintDown(x, y + 1, p); // up dot
    }

    private bool CheckLastDotColumn(int x, int y, int p) {
        // [x]
        // [x] 
        // [?]
        // We have to check if dots down, left or right of [?] is == p
        return ShowHintRight(x - 1, y, p) || // left dot
               ShowHintLeft(x + 1, y, p) || // right dot
               ShowHintUp(x, y - 1, p); // down dot
    }

    private bool ShowHintLeft(int x, int y, int p) {
        if (IsDot(x, y, p)) {
            matrix[x, y].ShowHintLeft();
            AddHintToDisable(matrix[x, y]);
            return true;
        }

        return false;
    }

    private bool ShowHintRight(int x, int y, int p) {
        if (IsDot(x, y, p)) {
            matrix[x, y].ShowHintRight();
            AddHintToDisable(matrix[x, y]);
            return true;
        }

        return false;
    }

    private bool ShowHintUp(int x, int y, int p) {
        if (IsDot(x, y, p)) {
            matrix[x, y].ShowHintUp();
            AddHintToDisable(matrix[x, y]);
            return true;
        }

        return false;
    }

    private bool ShowHintDown(int x, int y, int p) {
        if (IsDot(x, y, p)) {
            matrix[x, y].ShowHintDown();
            AddHintToDisable(matrix[x, y]);
            return true;
        }

        return false;
    }

    private bool IsDot(int x, int y, int p) {
        return x >= 0 && x < width && y >= 0 && y < height && matrix[x, y].dot == p;
    }


    private MiniList<DotController> CalculateMatchesFor(int x, int y) {
        MiniList<DotController> rowMatches = new MiniList<DotController>(4 * 2); // 4 is enough actually...
        int p = matrix[x, y].dot;
        // To the right
        for (int xx = x + 1; xx < width; xx++) {
            if (matrix[xx, y].dot != p) break;
            rowMatches.Add(matrix[xx, y]);
        }

        // To the left
        for (int xx = x - 1; xx >= 0; xx--) {
            if (matrix[xx, y].dot != p) break;
            rowMatches.Add(matrix[xx, y]);
        }

        MiniList<DotController> colMatches = new MiniList<DotController>(4 * 2); // 4 is enough actually...
        // Up
        for (int yy = y + 1; yy < height; yy++) {
            if (matrix[x, yy].dot != p) break;
            colMatches.Add(matrix[x, yy]);
        }

        // Down
        for (int yy = y - 1; yy >= 0; yy--) {
            if (matrix[x, yy].dot != p) break;
            colMatches.Add(matrix[x, yy]);
        }

        if (rowMatches.Count < 2) {
            if (colMatches.Count < 2) {
                return new MiniList<DotController>(0);
            }

            // colMatches >= 2
            colMatches.Add(matrix[x, y]);
            return colMatches;
        }

        // rowMatches >= 2
        if (colMatches.Count < 2) {
            rowMatches.Add(matrix[x, y]);
            return rowMatches;
        }

        // rowMatches >= 2 && colMatches >= 2
        foreach (var colMatch in colMatches) {
            rowMatches.Add(colMatch);
        }

        rowMatches.Add(matrix[x, y]);

        return rowMatches;
    }

    class Matches {
        public readonly MiniList<DotController> matches;
        public int firstRowX = -1;
        public int firstColX = -1;
        public bool[] dirtyCol;
        private DotController[,] matrix;

        public Matches(DotController[,] matrix, int width, int height) {
            this.matrix = matrix;
            matches = new MiniList<DotController>(width * height);
            dirtyCol = new bool[width];
        }

        public bool HasFirstRow() {
            return firstRowX != -1;
        }

        public void SetFirstRow(int x) {
            firstRowX = x;
        }

        public bool HasMatches() {
            return !matches.Empty;
        }

        public bool HasFirstCol() {
            return firstColX != -1;
        }

        public void SetFirstCol(int x) {
            firstColX = x;
        }

        public void MarkToBeDestroyed(DotController dot) {
            if (matrix[dot.x, dot.y].destroyed) return;
            dirtyCol[dot.x] = true;
            dot.MarkToBeDestroyed();
            matches.Add(dot);
        }

    }

    private Matches MarkToBeDestroyedAndCalculateMatches() {
        Matches matches = new Matches(matrix, width, height);
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (x >= 2) {
                    // Check for row matches 3 to the left
                    if (matrix[x, y].dot == matrix[x - 1, y].dot &&
                        matrix[x, y].dot == matrix[x - 2, y].dot) {
                        if (!matches.HasFirstRow()) {
                            matches.SetFirstRow(x - 2);
                        }
                        matches.MarkToBeDestroyed(matrix[x, y]);
                        matches.MarkToBeDestroyed(matrix[x - 1, y]);
                        matches.MarkToBeDestroyed(matrix[x - 2, y]);
                    }
                }

                if (y >= 2) {
                    // Check for col matches 3 to down
                    if (matrix[x, y].dot == matrix[x, y - 1].dot &&
                        matrix[x, y].dot == matrix[x, y - 2].dot) {
                        if (!matches.HasFirstCol()) {
                            matches.SetFirstCol(x);
                        }
                        matches.MarkToBeDestroyed(matrix[x, y]);
                        matches.MarkToBeDestroyed(matrix[x, y - 1]);
                        matches.MarkToBeDestroyed(matrix[x, y - 2]);
                    }
                }
            }
        }

        return matches;
    }

    private IEnumerator<YieldInstruction> DestroyMatchesAndFill(Matches matches) {
        if (!matches.HasMatches()) {
            yield break;
        }

        destroying = true;
        yield return new WaitForSeconds(0.25F);

        for (int i = 0; i < matches.matches.Count; i++) {
            matches.matches[i].NiceDestroy();
        }

        yield return new WaitForSeconds(0.25F);

        // Check for destroyed dots and move down
        ReplaceDestroyed(matches);
        destroying = false;
    }

    private void ReplaceDestroyed(Matches matches) {
        // Loop over the columns

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                matrix[x, y].ClearDebugText();
            }
        }

        for (int x = 0; x < width; x++) {
            if (matches.dirtyCol[x] == false) continue;
            
            // Reorganize the column
            MiniList<DotController> columnAlive = new MiniList<DotController>(height);
            MiniList<DotController> columnDestroyed = new MiniList<DotController>(height);
            for (int y = 0; y < height; y++) {
                if (matrix[x, y].destroyed) {
                    columnDestroyed.Add(matrix[x, y]);
                } else {
                    columnAlive.Add(matrix[x, y]);
                }
            }


            // Move them to fill the holes
            for (int yy = 0; yy < columnAlive.Count; yy++) {
                FallTo(columnAlive[yy], x, yy);
            }

            // Add more on top
            for (int yyy = 0; yyy < columnDestroyed.Count; yyy++) {
                RecyleDot(columnDestroyed[yyy], x, yyy + columnAlive.Count, columnDestroyed.Count);
            }
        }

        ClearHints();
        int h = ShowHints();
        ClearHints();
        if (h == 0) {
            Debug.Log("FUCK");
        }

        if (matches.HasFirstRow()) {
            ForceRow(h, matches.firstRowX, height - 1);            
        }
        
    }

    private void ForceRow(int hints, int x, int y) {
        if (x == 0) {
            // |[aac]A
            // | ... C
            DotType dotA = matrix[x + 3, y].dotType;
            matrix[x, y].SetType(dotA);
            matrix[x + 1, y].SetType(dotA);
            DotType dotC = y == 0 ? matrix[x + 3, y + 1].dotType : matrix[x + 3, y - 1].dotType;
            var debugText = "r";
            if (dotC.number == dotA.number) {
                // |[aac]AC
                // | ... A
                dotC = matrix[x + 4, y].dotType;
                debugText = "rc";
            }

            matrix[x + 2, y].SetType(dotC);
            matrix[x + 2, y].SetDebugText(debugText);
            matrix[x, y].SetDebugText("r");
            matrix[x + 1, y].SetDebugText("r");
        } else if (x >= 1) {
            // |A[caa]
            // |C ... 
            DotType dotA = matrix[x - 1, y].dotType;
            matrix[x + 1, y].SetType(dotA);
            matrix[x + 2, y].SetType(dotA);
            DotType dotC = y == 0 ? matrix[x - 1, y + 1].dotType : matrix[x - 1, y - 1].dotType;
            var debugText = "R";
            if (dotC.number == dotA.number) {
                if (x >= 2) {
                    debugText = "RC";
                    // |CA[caa]
                    // | A ... 
                    dotC = matrix[x - 2, y].dotType;
                } else {
                    // |A[?aa]|
                    // |A ... |
                    debugText = "R?";
                    dotC = GetRandomTypeExcept(dotA);
                }
            }

            matrix[x, y].SetType(dotC);
            matrix[x, y].SetDebugText(debugText);

            matrix[x + 1, y].SetDebugText("R");
            matrix[x + 2, y].SetDebugText("R");
        }
    }


    private bool CalculateStopped() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (!matrix[x, y].stopped) {
                    return false;
                }
            }
        }

        return true;
    }

    public void UserClicks(DotController dot) {
        dot.SetType(dotTypes[(dot.dotType.number + 1) % dotTypes.Length]);
        ClearHints();
        ShowHints();
    }
}