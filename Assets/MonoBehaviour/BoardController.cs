using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
using Random = UnityEngine.Random;

public enum BoardState {
    unknown,
    destroying,
    moving,
    ready
}

public class BoardController : MonoBehaviour {
    /*
     TASKS
     - Store scoring: points, rows, cols, types
     - Objetive
     
     Visual
     - Animate hints     
     - Explosion + animate scoring     
     - Sounds
     - Replace elements    
     */

    # region config

    public GameObject dotPrefab;
    public DotType[] dotTypes;
    public int width;
    public int height;

    public int maxTypes = 5;

    # endregion

    private GameObject _textMeshPro;


    private DotController[,] matrix;
    private BoardState state;

    private bool destroying {
        get { return state == BoardState.destroying; }
    }

    private bool moving {
        get { return state == BoardState.moving; }
    }

    private bool ready {
        get { return state == BoardState.ready; }
    }

    private bool unknown {
        get { return state == BoardState.unknown; }
    }

    private void SetStateDestroying() {
        state = BoardState.destroying;
    }

    private void SetStateReady() {
        state = BoardState.ready;
    }

    private void SetStateMoving() {
        state = BoardState.moving;
    }

    private void SetStateUnknown() {
        state = BoardState.unknown;
    }

    // Start is called before the first frame update
    void Start() {
        _textMeshPro = GameObject.Find("Score");

        Assert.IsTrue(width >= 5, "Width should be 6 at least");
        CreateBoard();
    }

    public void LoadBoard() {
        // carga
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                matrix[x, y].SetType(dotTypes[PlayerPrefs.GetInt(x + "." + y)]);
            }
        }
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
        return dotTypes[Random.Range(0, maxTypes)];
    }

    private DotType GetRandomTypeExcept(DotType except) {
        DotType dotType = GetRandomType();
        if (except == null) {
            return dotType;
        }

        int p = dotType.number;
        if (p == except.number) {
            p = (p + 1) % maxTypes;
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
            p = (p + 1) % maxTypes;
        }

        return dotTypes[p];
    }

    private void RecyleDot(DotController dot, int x, int y, int destroyedAmount) {
        dot.SetType(GetRandomType());
        dot.FallTo(x, y, y + destroyedAmount);
        dot.Recycle();
        matrix[x, y] = dot;
        SetStateMoving();
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
        SetStateMoving();
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
        if (!ready ||
            x < 0 || x >= width || y < 0 || y >= height) {
            return;
        }

        SetStateMoving();

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
        SetStateMoving();
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
        if (destroying) {
            return;
        }

        if (moving) {
            if (CalculateStopped()) {
                SetStateUnknown();
                return;
            }
        }

        if (unknown) {
            if (rollbackOne != null) {
                // bad move!
                SwapDots(rollbackOne, rollbackOther);
                rollbackOne = rollbackOther = null;
                return;
            }

            var matches = MarkToBeDestroyedAndCalculateMatches();
            if (matches.HasMatches()) {
                // matches!
                StartCoroutine(DestroyMatchesAndFill(matches));
            } else {
                SetStateReady();
                ClearHints();
                ShowHints();
            }
        }
    }

    private MiniList<DotController> hints = null;

    private void ClearHints() {
        if (hints != null)
        foreach (DotController dot in hints) {
            dot.DisableHint();
        }

        hints = null;
    }

    private void ShowHints() {
        ClearHints();
        hints = FindHints();

        if (hints != null) {
            foreach (DotController dot in hints) {
                dot.EnableHint();
            }
        }
    }

    private MiniList<DotController> FindHints() {
        MiniList<DotController> hints = null;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (x < width - 3) {
                    hints = GetCandidateTypeRow3(y, x, x + 1, x + 2, x + 3);
                    if (hints != null) break;
                }

                if (x < width - 2) {
                    hints = GetCandidateTypeRowL(y, x, x + 1, x + 2);
                    if (hints != null) break;
                }

                if (y < height - 3) {
                    hints = GetCandidateTypeCol3(x, y, y + 1, y + 2, y + 3);
                    if (hints != null) break;
                }

                if (y < height - 2) {
                    hints = GetCandidateTypeColL(x, y, y + 1, y + 2);
                    if (hints != null) break;
                }
            }
            if (hints != null) break;
        }
        return hints;
    }

    // Check for XoXX or XXoX
    private MiniList<DotController> GetCandidateTypeRow3(int y, int x1, int x2, int x3, int x4) {
        if (matrix[x1, y].dot == matrix[x2, y].dot &&
            matrix[x1, y].dot == matrix[x4, y].dot) {
            // XXoX
            var row3 = MiniListRow(y, x1, x2, x4);
            if (x4 < width -1 && matrix[x1, y].dot == matrix[x4 + 1, y].dot) {
                // XXoXX
                // We only look for one single hint, so we got the XXoXX here first
                row3.Add(matrix[x4 + 1, y]);
            }
            return row3;
        }

        if (matrix[x1, y].dot == matrix[x3, y].dot &&
            matrix[x1, y].dot == matrix[x4, y].dot) {
            // XoXX
            return MiniListRow(y, x1, x3, x4);
        }

        return null;
    }

    // Check for
    // X X
    // o X
    // X o
    // X X
    private MiniList<DotController> GetCandidateTypeCol3(int x, int y1, int y2, int y3, int y4) {
        if (matrix[x, y1].dot == matrix[x, y2].dot &&
            matrix[x, y1].dot == matrix[x, y4].dot) {
            // XXoX
            var col3 = MiniListCol(x, y1, y2, y4);
            if (y4 < height -1 && matrix[x, y1].dot == matrix[x, y4 + 1].dot) {
                // XXoXX
                // We only look for one single hint, so we got the XXoXX here first
                col3.Add(matrix[x, y4 + 1]);
            }
            return col3;
        }

        if (matrix[x, y1].dot == matrix[x, y3].dot &&
            matrix[x, y1].dot == matrix[x, y4].dot) {
            // XoXX
            return MiniListCol(x, y1, y3, y4);
        }

        return null;
    }

    private MiniList<DotController> MiniListRow(int y, int x1, int x2, int x3 = -1) {
        var list = new MiniList<DotController>(4); // 4, just in case we got the XXoXX hint
        list.Add(matrix[x1, y]);
        list.Add(matrix[x2, y]);
        if (x3 != -1) list.Add(matrix[x3, y]);
        return list;
    }

    private MiniList<DotController> MiniListCol(int x, int y1, int y2, int y3 = -1) {
        var list = new MiniList<DotController>(4); // 4, just in case we got the XXoXX hint
        list.Add(matrix[x, y1]);
        list.Add(matrix[x, y2]);
        if (y3 != -1) list.Add(matrix[x, y3]);
        return list;
    }

    // Check for oXX XXo   X X
    //           X     X XXo oXX
    private MiniList<DotController> GetCandidateTypeRowL(int y, int x1, int x2, int x3) {
        if (matrix[x1, y].dot == matrix[x2, y].dot) {
            // XXo
            if (y >= 1 && matrix[x1, y].dot == matrix[x3, y - 1].dot) {
                // XXo 
                //   X
                return MiniListRow(y, x1, x2).Add(matrix[x3, y - 1]);
            }

            if (y < height - 1 && matrix[x1, y].dot == matrix[x3, y + 1].dot) {
                // XXo 
                //   X
                return MiniListRow(y, x1, x2).Add(matrix[x3, y + 1]);
            }
        }

        if (matrix[x2, y].dot == matrix[x3, y].dot) {
            // oXX
            if (y >= 1 && matrix[x1, y - 1].dot == matrix[x2, y].dot) {
                // oXX 
                // X
                return MiniListRow(y, x2, x3).Add(matrix[x1, y - 1]);
            }

            if (y < height - 1 && matrix[x1, y + 1].dot == matrix[x2, y].dot) {
                // oXX 
                // X
                return MiniListRow(y, x2, x3).Add(matrix[x1, y + 1]);
            }
        }

        return null;
    }

    // Check for (y1 down to y3 up)
    // oX Xo X   X
    // X   X X   X
    // X   X oX Xo
    private MiniList<DotController> GetCandidateTypeColL(int x, int y1, int y2, int y3) {
        if (matrix[x, y1].dot == matrix[x, y2].dot) {
            // o
            // X
            // X
            if (x >= 1 && matrix[x, y1].dot == matrix[x - 1, y3].dot) {
                // Xo 
                //  X
                //  X
                return MiniListCol(x, y1, y2).Add(matrix[x - 1, y3]);
            }

            if (x < width - 1 && matrix[x, y1].dot == matrix[x + 1, y3].dot) {
                //  oX 
                //  X
                //  X
                return MiniListCol(x, y1, y2).Add(matrix[x + 1, y3]);
            }
        }

        if (matrix[x, y2].dot == matrix[x, y3].dot) {
            // X
            // X
            // o
            if (x >= 1 && matrix[x - 1, y1].dot == matrix[x, y2].dot) {
                //  X
                //  X
                // Xo
                return MiniListCol(x, y2, y3).Add(matrix[x - 1, y1]);
            }

            if (x < width - 1 && matrix[x + 1, y1].dot == matrix[x, y2].dot) {
                // X
                // X
                // oX
                return MiniListCol(x, y2, y3).Add(matrix[x + 1, y1]);
            }
        }

        return null;
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
        public DotController firstRow;
        public DotController firstCol;
        public bool[] dirtyCol;
        private DotController[,] matrix;
        public bool col4 = false;
        public bool col5 = false;
        public bool doubleCol = false;

        public bool row4 = false;
        public bool row5 = false;
        public bool doubleRow = false;

        public Matches(DotController[,] matrix, int width, int height) {
            this.matrix = matrix;
            matches = new MiniList<DotController>(width * height);
            dirtyCol = new bool[width];
        }

        public bool HasFirstRowWithDifferentType(DotType type) {
            return firstRow != null && firstRow.dotType.number != type.number;
        }

        public void SetFirstRow(DotController x) {
            firstRow = x;
        }

        public bool HasMatches() {
            return !matches.Empty;
        }

        public bool HasFirstColWithDifferentType(DotType type) {
            return firstCol != null && firstCol.dotType.number != type.number;
        }

        public void SetFirstCol(DotController x) {
            firstCol = x;
        }

        public void MarkToBeDestroyed(DotController dot) {
            if (matrix[dot.x, dot.y].destroyed) return;
            dirtyCol[dot.x] = true;
            dot.MarkToBeDestroyed();
            matches.Add(dot);
        }

        public bool HasDot(DotController dot) {
            return matches.Contains(dot);
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
                        if (matches.HasFirstRowWithDifferentType(matrix[x, y].dotType)) {
                            matches.doubleRow = true;
                        } else {
                            matches.SetFirstRow(matrix[x - 2, y]);
                        }

                        matches.MarkToBeDestroyed(matrix[x, y]);
                        matches.MarkToBeDestroyed(matrix[x - 1, y]);
                        matches.MarkToBeDestroyed(matrix[x - 2, y]);

                        bool match4 = x < width - 1 && matrix[x, y].dot == matrix[x + 1, y].dot;
                        bool match5 = x < width - 2 && matrix[x, y].dot == matrix[x + 2, y].dot;

                        matches.row4 = matches.row4 || match4;
                        matches.row5 = matches.row5 || match5;
                    }
                }

                if (y >= 2) {
                    // Check for col matches 3 to down
                    if (matrix[x, y].dot == matrix[x, y - 1].dot &&
                        matrix[x, y].dot == matrix[x, y - 2].dot) {
                        if (matches.HasFirstColWithDifferentType(matrix[x, y].dotType)) {
                            matches.doubleCol = true;
                        } else {
                            matches.SetFirstCol(matrix[x, y]);
                        }

                        matches.MarkToBeDestroyed(matrix[x, y]);
                        matches.MarkToBeDestroyed(matrix[x, y - 1]);
                        matches.MarkToBeDestroyed(matrix[x, y - 2]);

                        bool match4 = y < height - 1 && matrix[x, y].dot == matrix[x, y + 1].dot;
                        bool match5 = y < height - 2 && matrix[x, y].dot == matrix[x, y + 2].dot;

                        matches.col4 = matches.col4 || match4;
                        matches.col5 = matches.col5 || match5;
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

        SetStateDestroying();
        yield return new WaitForSeconds(0.07F);

        for (int i = 0; i < matches.matches.Count; i++) {
            matches.matches[i].NiceDestroy();
        }

        ShowScore(matches);

        yield return new WaitForSeconds(0.125F);

        // Check for destroyed dots and move down
        ReplaceDestroyed(matches);
        SetStateMoving();
    }

    private void ShowScore(Matches matches) {
        Debug.Log(matches.matches.Count + " points!");
        var text = matches.matches.Count + " points!";

        if (matches.row4) {
            Debug.Log("ROW4!");
            text = text + " (row 4)";
        }

        if (matches.row5) {
            Debug.Log("ROW5!!!");
            text = text + " (row 5)";
        }

        if (matches.doubleRow) {
            text = text + " Doble row!";
        }

        if (matches.col4) {
            text = text + " (col 4)";
        }

        if (matches.col5) {
            text = text + " (col 5)";
        }

        if (matches.doubleCol) {
            text = text + " Doble col!";
        }

        _textMeshPro.GetComponent<TMP_Text>().text = text;
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

        if (matches.firstRow != null) {
            // Avoid deadlock creating a good row that can be matched in the next move
            ForceRow(matches.firstRow.x);
        }

        if (matches.firstCol != null) {
            // Avoid deadlock creating a good column that can be matched in the next move
            ForceCol(matches.firstCol.x);
        }
    }

    private void ForceRow(int x) {
        int y = height - 1;
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

            matrix[x, y].SetDebugText("r");
            matrix[x + 1, y].SetDebugText("r");
            matrix[x + 2, y].SetDebugText(debugText);
        } else if (x >= 1) {
            // |A[caa]
            // |C ... 
            DotType dotA = matrix[x - 1, y].dotType;
            matrix[x + 1, y].SetType(dotA);
            matrix[x + 2, y].SetType(dotA);
            DotType dotC = matrix[x - 1, y - 1].dotType;
            var debugText = "R";
            if (dotC.number == dotA.number) {
                if (x >= 2) {
                    debugText = "RC";
                    // |CA[caa]
                    // | A ... 
                    dotC = matrix[x - 2, y].dotType;
                } else {
                    // |A[caa]|
                    // |A ... |
                    // |C ... |
                    debugText = "RCC";
                    dotC = matrix[x - 1, y - 2].dotType;
                }
            }

            matrix[x, y].SetType(dotC);

            matrix[x, y].SetDebugText(debugText);
            matrix[x + 1, y].SetDebugText("R");
            matrix[x + 2, y].SetDebugText("R");
        }
    }

    private void ForceCol(int x) {
        int y = height - 1;
        DotType dotA = matrix[x, y - 3].dotType;
        matrix[x, y].SetType(dotA);
        matrix[x, y - 1].SetType(dotA);
        DotType dotC = x == 0 ? matrix[x + 1, y - 3].dotType : matrix[x - 1, y - 3].dotType;
        var debugText = "d";
        if (dotC.number == dotA.number) {
            // | a
            // | a
            // | c
            // |AAA
            // | C
            dotC = matrix[x, y - 4].dotType;
            debugText = "dc";
        }

        matrix[x, y - 2].SetType(dotC);

        matrix[x, y].SetDebugText("d");
        matrix[x, y - 1].SetDebugText("d");
        matrix[x, y - 2].SetDebugText(debugText);
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
        dot.SetType(dotTypes[(dot.dotType.number + 1) % maxTypes]);
        ShowHints();
        SetStateUnknown();
    }
}