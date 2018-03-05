using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour {

    #region Inspector settings
    public GameObject mazePiecePrefab;
    public int mazeX = 0;
    public int mazeY = 0;
    public int genSpeed = 0;
    public bool combineMeshes = true;
    #endregion

    #region Piece sizes
    private float _pieceWidth;
    private float _pieceHeight;
    private float _wallWidth;
    public float remainingPieceWidth = 0;
    public float halfRemainingPieceWidth = 0;

    public float pieceWidth
    {
        get { return _pieceWidth; }
        set
        {
            _pieceWidth = value;
            remainingPieceWidth = value - _wallWidth;
            halfRemainingPieceWidth = remainingPieceWidth * 0.5F;
        }
    }

    public float pieceHeight
    {
        get { return _pieceHeight; }
        set { _pieceHeight = value; }
    }

    public float wallWidth
    {
        get { return _wallWidth; }
        set
        {
            _wallWidth = value;
            remainingPieceWidth = _pieceWidth - value;
            halfRemainingPieceWidth = remainingPieceWidth * 0.5F;
        }
    }
    #endregion

    #region Transform references
    public Transform unvisited;
    public Transform visited;
    private Transform floor;
    #endregion

    #region Piece references
    public Transform[][] piecesArray;
    private Transform currentPiece;
    private Stack<Transform> piecesBeingLookedAt;
    #endregion

    #region Completion flags
    private bool pathComplete = false;
    private bool wallsFused = false;
    private bool wallMeshesCombined = false;
    #endregion

    void Awake()
    {
        unvisited = this.transform.Find("Unvisited");
        visited = this.transform.Find("Visited");
        floor = visited.Find("Floor");
        piecesBeingLookedAt = new Stack<Transform>();
        pieceWidth = 10;
        pieceHeight = 5;
        wallWidth = 1;
    }
    
    void Start()
    {
        InitialiseCameraPosition();
        ResizeAndPositionFloor();
        GenerateMazePieces();
        FindAllNeighbours();
        InitialiseCurrentPiece();
        StartCoroutine(BuildMaze());
    }

    #region Setup and piece creation
    private void InitialiseCameraPosition()
    {
        Camera.main.transform.position = new Vector3(
            mazeX * halfRemainingPieceWidth,
            System.Math.Max(mazeX, mazeY) * remainingPieceWidth,
            mazeY * -halfRemainingPieceWidth);
    }

    private void ResizeAndPositionFloor()
    {
        floor.parent = null;

        floor.localScale = new Vector3(
            mazeX * remainingPieceWidth + _wallWidth,
            _wallWidth,
            mazeY * remainingPieceWidth + _wallWidth);
        floor.localPosition = new Vector3(
            (mazeX - 1) * halfRemainingPieceWidth,
            (_pieceHeight + _wallWidth) * -0.5F,
            (mazeY - 1) * -halfRemainingPieceWidth);
        FixUVs(floor);
        floor.localRotation = Quaternion.identity;

        floor.parent = visited;
    }

    private void GenerateMazePieces()
    {
        piecesArray = new Transform[mazeX][];
        Walls newPieceScript;

        for (int i = 0; i < mazeX; i++)
        {
            piecesArray[i] = new Transform[mazeY];
        }

        for (int i = 0; i < mazeX; i++)
        {
            for (int j = 0; j < mazeY; j++)
            {
                if (mazePiecePrefab)
                {
                    GameObject newPiece = Instantiate(
                        mazePiecePrefab,
                        new Vector3(i * remainingPieceWidth, 0, j * -remainingPieceWidth),
                        Quaternion.identity);
                    newPiece.name = CreatePieceName(i, j);
                    newPieceScript = newPiece.GetComponent<Walls>();
                    newPieceScript.generator = this;
                    newPieceScript.Initialise(i, j, mazeX - 1, mazeY - 1);
                    piecesArray[i][j] = newPiece.transform;

                    if (unvisited)
                    {
                        newPiece.transform.parent = unvisited;
                    }
                }
            }

        }
    }

    private void FindAllNeighbours()
    {
        foreach (Walls walls in GetComponentsInChildren<Walls>())
        {
            walls.FindNeighbours();
        }
    }
    #endregion

    #region Maze path building and combining
    private void InitialiseCurrentPiece()
    {
        if (piecesArray.Length > 0 && piecesArray[0].Length > 0)
        {
            currentPiece = piecesArray[0][0];

            if (currentPiece)
            {
                currentPiece.parent = visited;
            }

        }
    }

    IEnumerator BuildMaze()
    {
        while (!pathComplete)
        {
            for (int i = 0; i < genSpeed; i++)
            {
                if (pathComplete)
                {
                    break;
                }
                else
                {
                    VisitNextPiece();
                }
            }

            yield return null;
        }

        FuseWalls();

        yield return null;

        if (combineMeshes)
        {
            CombineWallMeshes();
        }
    }

    private void VisitNextPiece()
    {
        if (unvisited.childCount > 0)
        {
            Transform[] unvisitedNeighbours = currentPiece.GetComponent<Walls>().GetUnvisitedNeighbours();

            if (unvisitedNeighbours.Length > 0)
            {
                piecesBeingLookedAt.Push(currentPiece);
                Transform newCurrentPiece = unvisitedNeighbours[Random.Range(0, unvisitedNeighbours.Length)];
                currentPiece.GetComponent<Walls>().JoinToNeighbour(newCurrentPiece);
                currentPiece = newCurrentPiece;
                newCurrentPiece.parent = visited;
            }
            else if (piecesBeingLookedAt.Count > 0)
            {
                currentPiece = piecesBeingLookedAt.Pop();
            }
        }
        else
        {
            pathComplete = true;
            Debug.Log("Path completed");
        }
    }

    #region Combining
    private void FuseWalls()
    {
        for (int i = 0; i < mazeX; i++)
        {
            for (int j = 0; j < mazeY; j++)
            {
                Walls currentWallPiece = piecesArray[i][j].GetComponent<Walls>();
                currentWallPiece.FuseAllWallsInLine();
                Destroy(currentWallPiece.gameObject);
            }
        }

        wallsFused = true;
        Debug.Log("Walls fused");
    }

    private void CombineWallMeshes()
    {
        MeshFilter[] meshFilters = visited.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = worldToLocal * meshFilters[i].transform.localToWorldMatrix;
            Destroy(meshFilters[i].gameObject);
        }

        transform.GetComponent<MeshFilter>().mesh = new Mesh();
        transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        wallMeshesCombined = true;
        Debug.Log("Meshes combined");
    }
    #endregion
    #endregion

    #region Helper functions
    public string CreatePieceName(int x, int y)
    {
        return x + ", " + y;
    }

    public void FixUVs(Transform inputTransform)
    {
        float extraScalingFactor = 0.1F;
        Mesh currentMesh = inputTransform.GetComponent<MeshFilter>().mesh;
        float scaledWidth = inputTransform.localScale.x * extraScalingFactor;
        float scaledHeight = inputTransform.localScale.y * extraScalingFactor;
        float scaledDepth = inputTransform.localScale.z * extraScalingFactor;
        Vector2[] newUVs = new Vector2[24];

        for (int i = 0; i < currentMesh.uv.Length; i++)
        {
            // Front and back
            newUVs[0] = newUVs[10] = new Vector2(0, 0);
            newUVs[1] = newUVs[11] = new Vector2(scaledWidth, 0);
            newUVs[2] = newUVs[6] = new Vector2(0, scaledHeight);
            newUVs[3] = newUVs[7] = new Vector2(scaledWidth, scaledHeight);

            // Top and bottom
            newUVs[4] = newUVs[13] = new Vector2(0, scaledDepth);
            newUVs[5] = newUVs[14] = new Vector2(scaledWidth, scaledDepth);
            newUVs[8] = newUVs[12] = new Vector2(0, 0);
            newUVs[9] = newUVs[15] = new Vector2(scaledWidth, 0);

            // Left and right
            newUVs[16] = newUVs[20] = new Vector2(0, 0);
            newUVs[17] = newUVs[21] = new Vector2(0, scaledHeight);
            newUVs[18] = newUVs[22] = new Vector2(scaledDepth, scaledHeight);
            newUVs[19] = newUVs[23] = new Vector2(scaledDepth, 0);

            currentMesh.uv = newUVs;
        }
    }
    #endregion
}
