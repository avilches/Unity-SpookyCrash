﻿using System;
using System.Collections.Generic;
using UnityEngine;
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
        CreateBoard();
    }

    public void HIDEALL() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                matrix[x, y].gameObject.SetActive(false);
            }
        }

    }

    public void SHOWALL() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                matrix[x, y].gameObject.SetActive(true);
            }
        }

    }

    public void CreateBoard() {
        for (int xx = 0; xx < 1000; xx++) {
//            Debug.Log("------------------------------ Create board");
            ClearHints();
            DestroyBoard();
            matrix = new DotController[width, height];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    CreateNewDot(x, y);
                }
            }
            ShowHints();
            if (hints == 0) {
                Debug.Log("Retry " + xx);
                EnsureNoDeadLocks();
                break;
            }
        }
    }

    private void EnsureNoDeadLocks() {
        Debug.Log("Hints "+hints);
        int p = 0;
        while (++p < 1000) {
            ClearHints();
            int x = Random.Range(0, width - 2);
            int y = Random.Range(1, height - 2);
            DotType backup = matrix[x + 1, y].dotType;
            matrix[x + 1, y].SetType(matrix[x, y].dotType);
            Debug.Log("Setting "+(x+1)+","+y+" as a copy from "+x+","+y);
            var matches = CalculateMatches().Count;
            ShowHints();
            int newHints = hints;
            ClearHints();
            if (matches > 0) {
                // rollback
                Debug.Log("Hints "+newHints+" but matches "+matches+"! Rollback");
                matrix[x + 1, y].SetType(backup);
            } else if (newHints >= 3) {
                Debug.Log("Hints "+newHints+ "! goodbye!");
                break;
            } else {
                Debug.Log("Hints still "+newHints+", changing more...");
            }
        }
        ClearHints();
        ShowHints();
    }

    private void DestroyBoard() {
        if (matrix != null) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    Destroy(matrix[x, y].gameObject);
                    matrix[x, y] = null;
                }
            }
        }
    }

    private DotType GetRandomType() {
        int p = Random.Range(0, dotTypes.Length - 1);
        return dotTypes[p];
    }

    private DotType GetRandomTypeExcept(HashSet<int> nop) {
        if (nop.Count == 0) {
            return GetRandomType();
        }

        int p = Random.Range(0, dotTypes.Length - (1 + nop.Count));
        for (int xx = 0; xx < dotTypes.Length; xx++) {
            if (nop.Contains(xx)) {
                p++;
            } else if (p == xx) {
                break;
            }
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

    private void CreateNewDot(int x, int y) {
        GameObject o = Instantiate(dotPrefab, new Vector3(x, y, 0F), Quaternion.identity);
        DotController dot = o.GetComponent<DotController>();
        var nop = FindForbiddenColorsForPosition(x, y);
        dot.Configure(this, x, y, GetRandomTypeExcept(nop));
        matrix[x, y] = dot;
        stopped = false;
    }

    private HashSet<int> FindForbiddenColorsForPosition(int x, int y) {
        HashSet<int> nop = new HashSet<int>();
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
        var calculateMatchesFor = CalculateMatchesFor(x, y);
//        var calculateMatches = CalculateMatches();
//        foreach (DotController candidate in calculateMatchesFor) {
//            if (!calculateMatches.Contains(candidate)) {
//                Debug.Log("CalculateMatchesFor is not working well");                
//            }
//        }
        if (calculateMatchesFor.Count == 0) {
            // Bad move! Set a rollback after the move
            rollbackOne = dot;
            rollbackOther = other;
        } else {
            rollbackOne = rollbackOther = null;
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
        var matches = CalculateMatches();
        if (matches.Count > 0) {
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

    private void ShowHints() {
        hints = 0;
        if (hints == maxHints) return;
        for (int x = 0; x < width - 2; x++) {
            for (int y = 0; y < height; y++) {
                if (matrix[x, y].dot == matrix[x + 1, y].dot &&
                    matrix[x, y].dot != matrix[x + 2, y].dot) {
                    hints += CheckLastDotRow(x + 2, y, matrix[x, y].dot) ? 1 : 0;
                    if (hints == maxHints) return;
                } else if (matrix[x, y].dot != matrix[x + 1, y].dot &&
                           matrix[x + 1, y].dot == matrix[x + 2, y].dot) {
                    hints += CheckFirstDotRow(x, y, matrix[x + 2, y].dot) ? 1 : 0;
                    if (hints == maxHints) return;
                }
            }
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height - 2; y++) {
                if (matrix[x, y].dot == matrix[x, y + 1].dot &&
                    matrix[x, y].dot != matrix[x, y + 2].dot) {
                    hints += CheckFirstDotColumn(x, y + 2, matrix[x, y].dot) ? 1 : 0;
                    if (hints == maxHints) return;
                } else if (matrix[x, y].dot != matrix[x, y + 1].dot &&
                           matrix[x, y + 1].dot == matrix[x, y + 2].dot) {
                    hints += CheckLastDotColumn(x, y, matrix[x, y + 2].dot) ? 1 : 0;
                    if (hints == maxHints) return;
                }
            }
        }
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


    private HashSet<DotController> CalculateMatchesFor(int x, int y) {
        HashSet<DotController> rowMatches = new HashSet<DotController>();
        int p = matrix[x, y].dot;
        // To the right
        for (int xx = x + 1; xx < width; xx++) {
            if (matrix[xx, y].dot == p) {
                rowMatches.Add(matrix[xx, y]);
            } else {
                break;
            } 
        }
        // To the left
        for (int xx = x - 1; xx >= 0; xx--) {
            if (matrix[xx, y].dot == p) {
                rowMatches.Add(matrix[xx, y]);
            } else {
                break;
            }
        }

        HashSet<DotController> colMatches = new HashSet<DotController>();
        // Up
        for (int yy = y + 1; yy < height; yy++) {
            if (matrix[x, yy].dot == p) {
                colMatches.Add(matrix[x, yy]);
            } else {
                break;
            }
        }
        // Down
        for (int yy = y - 1; yy >= 0; yy--) {
            if (matrix[x, yy].dot == p) {
                colMatches.Add(matrix[x, yy]);
            } else {
                break;
            }
        }

        if (rowMatches.Count < 2) {
            if (colMatches.Count < 2) {
                return new HashSet<DotController>();
            }
            // colMatches >= 2
            return colMatches;
        }
        // rowMatches >= 2
        if (colMatches.Count < 2) {
            return rowMatches;
        }
        // rowMatches >= 2 && colMatches >= 2
        foreach (var colMatch in colMatches) {
            rowMatches.Add(colMatch);
        }
        return rowMatches;

    }

    private HashSet<DotController> CalculateMatches() {
        HashSet<DotController> matches = new HashSet<DotController>();
        // Horizontal matches
        for (int x = 0; x < width - 2; x++) {
            for (int y = 0; y < height; y++) {
                if (matrix[x, y].dot == matrix[x + 1, y].dot &&
                    matrix[x, y].dot == matrix[x + 2, y].dot) {
                    matches.Add(matrix[x, y]);
                    matches.Add(matrix[x + 1, y]);
                    matches.Add(matrix[x + 2, y]);
                }
            }
        }

        // Vertical matches
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height - 2; y++) {
                if (matrix[x, y].dot == matrix[x, y + 1].dot &&
                    matrix[x, y].dot == matrix[x, y + 2].dot) {
                    matches.Add(matrix[x, y]);
                    matches.Add(matrix[x, y + 1]);
                    matches.Add(matrix[x, y + 2]);
                }
            }
        }

        return matches;
    }

    private IEnumerator<YieldInstruction> DestroyMatchesAndFill(HashSet<DotController> matches) {
        if (matches.Count == 0) {
            yield break;
        }

        destroying = true;
        yield return new WaitForSeconds(0.25F);
        foreach (DotController match in matches) {
            match.NiceDestroy();
        }

        yield return new WaitForSeconds(0.25F);

        // Check for destroyed dots and move down
        for (int x = 0; x < width; x++) {
            // Reorganize the column
            List<DotController> columnAlive = new List<DotController>();
            List<DotController> columnDestroyed = new List<DotController>();
            for (int y = 0; y < height; y++) {
                if (matrix[x, y].destroyed) {
                    columnDestroyed.Add(matrix[x, y]);
                } else {
                    columnAlive.Add(matrix[x, y]);
                }
            }

            // Move them to fill the holes
            for (int yy = 0; yy < columnAlive.Count; yy++) {
                matrix[x, yy] = columnAlive[yy];
                matrix[x, yy].FallTo(x, yy);
            }

            // Add more on top
            for (int yyy = 0; yyy < columnDestroyed.Count; yyy++) {
                RecyleDot(columnDestroyed[yyy], x, yyy+columnAlive.Count, columnDestroyed.Count);
            }
        }

        destroying = false;
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
}