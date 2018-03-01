using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour {

    public GameObject mazePiecePrefab;
    public int mazeX = 0;
    public int mazeY = 0;
    public int genSpeed = 0;
    public bool combineMeshes = true;

    public static Transform unvisited;
    public static Transform visited;

    private Transform floor;

    public static Transform[][] piecesArray;

    private Transform currentPiece;
    private Stack<Transform> piecesBeingLookedAt;

    float timeStart;

    private bool pathComplete = false;
    private bool wallsFused = false;
    private bool wallMeshesCombined = false;

    private void Awake()
    {
        unvisited = this.transform.Find("Unvisited");
        visited = this.transform.Find("Visited");
        floor = visited.Find("Floor");
        piecesBeingLookedAt = new Stack<Transform>();
    }

    // Use this for initialization
    void Start ()
    {
        ResizeAndPositionFloor();
        GenerateMazePieces();
        FindAllNeighbours();
        InitialiseCurrentPiece();
        StartCoroutine(BuildMaze());

        Camera.main.transform.position = new Vector3(mazeX * 4.5F, System.Math.Max(mazeX, mazeY) * 9, mazeY * -4.5F);
    }

    // Update is called once per frame
    /*void Update ()
    {
        if (pathComplete)
        {
            if (!wallsFused)
            {
                FuseWalls();
            }
            else if (!wallMeshesCombined)
            {
                CombineWallMeshes();
            }
        }
        else
        {
            for (int i = 0; i < genSpeed; i++)
            {
                {
                    VisitNextPiece();
                }
            }
        }
    }*/

    private void ResizeAndPositionFloor()
    {
        floor.localScale = new Vector3(mazeX * 9 + 1, 1, mazeY * 9 + 1);
        floor.localPosition = new Vector3((mazeX - 1) * 4.5F, -3, (mazeY - 1) * -4.5F);
        FixUVs(floor);
    }

    IEnumerator BuildMaze()
    {
        while (!pathComplete)
        {
            for (int i = 0; i < genSpeed; i++)
            {
                {
                    VisitNextPiece();
                }
            }

            yield return null;
        }

        FuseWalls();

        yield return null;

        if(combineMeshes)
        {
            CombineWallMeshes();
        }
    }

    private void GenerateMazePieces()
    {
        piecesArray = new Transform[mazeX][];

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
                    GameObject newPiece = Instantiate(mazePiecePrefab, new Vector3(i * 9, 0, j * -9), Quaternion.identity);
                    newPiece.name = CreatePieceName(i, j);
                    newPiece.GetComponent<Walls>().Initialise(i, j, mazeX - 1, mazeY - 1);
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

    private void InitialiseCurrentPiece()
    {
        if (unvisited)
        {
            currentPiece = piecesArray[0][0];

            if (currentPiece)
            {
                currentPiece.parent = visited;
            }

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

    private void FuseWalls()
    {
        for (int i = 0; i < mazeX; i++)
        {
            for (int j = 0; j < mazeY; j++)
            {
                Walls currentWallPiece = piecesArray[i][j].GetComponent<Walls>();
                currentWallPiece.FuseNorthWallsEastward();
                currentWallPiece.FuseEastWallsSouthward();
                currentWallPiece.FuseSouthWallsEastward();
                currentWallPiece.FuseWestWallsSouthward();
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

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            //meshFilters[i].gameObject.SetActive(false);
            Destroy(meshFilters[i].gameObject);
        }

        transform.GetComponent<MeshFilter>().mesh = new Mesh();
        transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        transform.gameObject.SetActive(true);

        wallMeshesCombined = true;
        Debug.Log("Meshes combined");
    }

    public static string CreatePieceName(int x, int y)
    {
        return x + ", " + y;
    }

    public static void FixUVs(Transform inputTransform)
    {
        Mesh currentMesh = inputTransform.GetComponent<MeshFilter>().mesh;
        float currentWidth = inputTransform.localScale.x * 0.1f;
        float currentHeight = inputTransform.localScale.y * 0.1f;
        float currentDepth = inputTransform.localScale.z * 0.1f;
        Vector2[] newUVs = new Vector2[24];

        for (int i = 0; i < currentMesh.uv.Length; i++)
        {
            //Front
            newUVs[2] = new Vector2(0, currentHeight);
            newUVs[3] = new Vector2(currentWidth, currentHeight);
            newUVs[0] = new Vector2(0, 0);
            newUVs[1] = new Vector2(currentWidth, 0);

            //Back
            newUVs[7] = new Vector2(0, 0);
            newUVs[6] = new Vector2(currentWidth, 0);
            newUVs[11] = new Vector2(0, currentHeight);
            newUVs[10] = new Vector2(currentWidth, currentHeight);

            //Left
            newUVs[19] = new Vector2(currentDepth, 0);
            newUVs[17] = new Vector2(0, currentHeight);
            newUVs[16] = new Vector2(0, 0);
            newUVs[18] = new Vector2(currentDepth, currentHeight);

            //Right
            newUVs[23] = new Vector2(currentDepth, 0);
            newUVs[21] = new Vector2(0, currentHeight);
            newUVs[20] = new Vector2(0, 0);
            newUVs[22] = new Vector2(currentDepth, currentHeight);

            //Top
            newUVs[4] = new Vector2(currentWidth, 0);
            newUVs[5] = new Vector2(0, 0);
            newUVs[8] = new Vector2(currentWidth, currentDepth);
            newUVs[9] = new Vector2(0, currentDepth);

            //Bottom
            newUVs[13] = new Vector2(currentWidth, 0);
            newUVs[14] = new Vector2(0, 0);
            newUVs[12] = new Vector2(currentWidth, currentDepth);
            newUVs[15] = new Vector2(0, currentDepth);

            currentMesh.uv = newUVs;
        }
    }
}
