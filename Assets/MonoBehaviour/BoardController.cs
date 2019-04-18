using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardController : MonoBehaviour {
    public GameObject[] dotPrefabs;
    public int width;
    public int height;
    public DotController[,] matrix;
    [NonSerialized] private bool stopped = false;
    [NonSerialized] private bool destroying = false;

    // Start is called before the first frame update
    void Start() {
        CreateBoard();
    }

    public void CreateBoard() {
        DestroyBoard();
        matrix = new DotController[width, height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                CreateNewDot(x, y, true);
            }
        }
    }

    private void DestroyBoard() {
        if (matrix != null) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    matrix[x, y].Destroy();
                }
            }
        }
    }


    private void CreateNewDot(int x, int y, bool noMatch) {
        int p = 0;
        if (noMatch) {
            HashSet<int> nop = new HashSet<int>();
            // Previous two dots in the same row are equals?
            if (x >= 2 && matrix[x - 1, y].dot == matrix[x - 2, y].dot) {
                nop.Add(matrix[x - 1, y].dot);
            }
            // Previous two dots in the same column are equals?
            if (y >= 2 && matrix[x, y - 1].dot == matrix[x, y - 2].dot) {
                nop.Add(matrix[x, y - 1].dot);
            }

            if (nop.Count > 0) {
                p = Random.Range(0, dotPrefabs.Length - (1 + nop.Count));
                for (int xx = 0; xx < dotPrefabs.Length; xx++) {
                    if (nop.Contains(xx)) {
                        p++;
                    } else if (p == xx) {
                        break;
                    }
                }
            } else {
                p = Random.Range(0, dotPrefabs.Length - 1);
            }
        } else {
            p = Random.Range(0, dotPrefabs.Length - 1);
        }


        GameObject o = Instantiate(dotPrefabs[p], new Vector3(x + Random.Range(-7, 7), y + height, 0F),
            Quaternion.identity);
        o.transform.parent = gameObject.transform;
        matrix[x, y] = o.GetComponent<DotController>();
        matrix[x, y].FallTo(x, y);
        matrix[x, y].dot = p;
        matrix[x, y].board = this;
        stopped = false;
    }

    public void UserMoves(DotController dot, int x, int y) {
        if (!stopped || destroying ||
            x < 0 || x >= width || y < 0 || y >= height) {
            return;
        }

        DotController other = matrix[x, y];
        SwapDots(dot, other);
        if (CalculateMatches().Count == 0) {
            comebackOne = dot;
            comebackOther = other;
        } else {
            comebackOne = comebackOther = null;
        }
    }

    private void SwapDots(DotController dot, DotController other) {
        stopped = false;
        matrix[(int) dot.target.x, (int) dot.target.y] = other;
        matrix[(int) other.target.x, (int) other.target.y] = dot;
        // Save before call to SetTarget, or we will loose the original position
        int tempX = (int) dot.target.x;
        int tempY = (int) dot.target.y;
        dot.SetTarget(other.target.x, other.target.y);
        other.SetTarget(tempX, tempY);
    }

    private DotController comebackOne;
    private DotController comebackOther;


    // Update is called once per frame
    void Update() {
        if (!stopped && !destroying) {
            stopped = CalculateStopped();
            // Debug.Log(stopped);
            if (stopped) {
                if (comebackOne != null) {
                    SwapDots(comebackOne, comebackOther);
                    comebackOne = comebackOther = null;
                } else {
                    StartCoroutine(DestroyMatchesAndFill(CalculateMatches()));
                }
            }
        }
    }

    private HashSet<DotController> CalculateMatches() {
        HashSet<DotController> matches = new HashSet<DotController>();
        // Horizontal matches
        for (int x = 0; x < width - 2; x++) {
            for (int y = 0; y < height; y++) {
                if (matrix[x, y].dot == matrix[x + 1, y].dot &&
                    matrix[x, y].dot == matrix[x + 2, y].dot) {
                    matrix[x, y].destroyed = true;
                    matrix[x + 1, y].destroyed = true;
                    matrix[x + 2, y].destroyed = true;
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
                    matrix[x, y].destroyed = true;
                    matrix[x, y + 1].destroyed = true;
                    matrix[x, y + 2].destroyed = true;
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
        foreach (DotController match in matches) {
            match.Destroy();
        }

        yield return new WaitForSeconds(0.4F);

        // Check for destroyed and move
        for (int x = 0; x < width; x++) {
            // Get only non destroyed dots
            List<DotController> column = new List<DotController>();
            for (int y = 0; y < height; y++) {
                if (!matrix[x, y].destroyed) {
                    column.Add(matrix[x, y]);
                }
            }

            // Move them to fill the holes
            for (int yy = 0; yy < column.Count; yy++) {
                matrix[x, yy] = column[yy];
                matrix[x, yy].FallTo(x, yy);
            }

            // Add more on top
            for (int yyy = column.Count; yyy < height; yyy++) {
                CreateNewDot(x, yyy, false);
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