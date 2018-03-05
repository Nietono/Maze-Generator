using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Walls : MonoBehaviour {

    #region Wall objects
    public GameObject wallN;
    public GameObject wallE;
    public GameObject wallS;
    public GameObject wallW;
    #endregion

    #region Piece position
    private int myX = 0;
    private int myY = 0;
    private int maxX = 0;
    private int maxY = 0;
    #endregion

    #region Neighbouring pieces
    public Transform neighbourN;
    public Transform neighbourE;
    public Transform neighbourS;
    public Transform neighbourW;

    private enum WallDirections{North, South, East, West, None};
    #endregion

    public MazeGenerator generator;

    #region Setup
    public void Initialise(int x, int y, int newMaxX, int newMaxY)
    {
        myX = x;
        myY = y;
        maxX = newMaxX;
        maxY = newMaxY;

        FixChildUVs();
    }

    public void FixChildUVs()
    {
        foreach(Transform child in transform)
        {
            generator.FixUVs(child);
        }
    }

    public void FindNeighbours()
    {
        if (myY > 0)
        {
            neighbourN = generator.piecesArray[myX][myY - 1];
        }

        if (myX < maxX)
        {
            neighbourE = generator.piecesArray[myX + 1][myY];
        }

        if (myY < maxY)
        {
            neighbourS = generator.piecesArray[myX][myY + 1];
        }

        if (myX > 0)
        {
            neighbourW = generator.piecesArray[myX - 1][myY];
        }
    }
    #endregion

    #region Path building
    public Transform[] GetUnvisitedNeighbours()
    {
        List<Transform> unvisitedList = new List<Transform>();

        if (neighbourN && neighbourN.parent == generator.unvisited)
        {
            unvisitedList.Add(neighbourN);
        }

        if (neighbourE && neighbourE.parent == generator.unvisited)
        {
            unvisitedList.Add(neighbourE);
        }

        if (neighbourS && neighbourS.parent == generator.unvisited)
        {
            unvisitedList.Add(neighbourS);
        }

        if (neighbourW && neighbourW.parent == generator.unvisited)
        {
            unvisitedList.Add(neighbourW);
        }

        return unvisitedList.ToArray();
    }

    public void JoinToNeighbour(Transform neighbour)
    {
        if (neighbour == neighbourN)
        {
            Destroy(wallN);
            Destroy(neighbour.GetComponent<Walls>().wallS);
        }
        else if (neighbour == neighbourE)
        {
            Destroy(wallE);
            Destroy(neighbour.GetComponent<Walls>().wallW);
        }
        else if (neighbour == neighbourS)
        {
            Destroy(wallS);
            Destroy(neighbour.GetComponent<Walls>().wallN);
        }
        else if (neighbour == neighbourW)
        {
            Destroy(wallW);
            Destroy(neighbour.GetComponent<Walls>().wallE);
        }
    }
    #endregion

    #region Combining
    public void FuseAllWallsInLine()
    {
        FuseWallsInLine(wallN);
        FuseWallsInLine(wallE);
        FuseWallsInLine(wallS);
        FuseWallsInLine(wallW);
    }

    public void FuseWallsInLine(GameObject inputWall)
    {
        WallDirections wallDirection = WallObjectToDirection(inputWall);

        if (wallDirection != WallDirections.None)
        {
            Transform neighbourTransform;
            Walls currentWallsPiece = null;
            GameObject targetWall = null;
            bool isNorthOrSouth;
            int wallLength = 1;

            isNorthOrSouth = (wallDirection == WallDirections.North || wallDirection == WallDirections.South);
            neighbourTransform = isNorthOrSouth ? neighbourE : neighbourS;

            if (neighbourTransform)
            {
                currentWallsPiece = neighbourTransform.GetComponent<Walls>();
                targetWall = WallFromDirection(currentWallsPiece, wallDirection);
            }

            while (currentWallsPiece && targetWall)
            {
                wallLength++;
                Destroy(targetWall);

                if (isNorthOrSouth && currentWallsPiece.neighbourE)
                {
                    currentWallsPiece = currentWallsPiece.neighbourE.GetComponent<Walls>();
                }
                else if (!isNorthOrSouth && currentWallsPiece.neighbourS)
                {
                    currentWallsPiece = currentWallsPiece.neighbourS.GetComponent<Walls>();
                }
                else
                {
                    currentWallsPiece = null;
                }

                targetWall = WallFromDirection(currentWallsPiece, wallDirection);
            }

            if (wallLength > 1)
            {
                FixFusedWall(inputWall, wallLength, isNorthOrSouth);
            }

            inputWall.transform.parent = transform.parent;
        }
    }

    private WallDirections WallObjectToDirection(GameObject inputWall)
    {
        if (!inputWall)
        {
            return WallDirections.None;
        }

        if (inputWall == wallN)
        {
            return WallDirections.North;
        }
        else if (inputWall == wallE)
        {
            return WallDirections.East;
        }
        else if (inputWall == wallS)
        {
            return WallDirections.South;
        }
        else if (inputWall == wallW)
        {
            return WallDirections.West;
        }

        return WallDirections.None;
    }

    private GameObject WallFromDirection(Walls wallScript, WallDirections wallDirection)
    {
        if (!wallScript)
        {
            return null;
        }

        switch (wallDirection)
        {
            case WallDirections.North:
                return wallScript.wallN;
            case WallDirections.East:
                return wallScript.wallE;
            case WallDirections.South:
                return wallScript.wallS;
            case WallDirections.West:
                return wallScript.wallW;
            default:
                return null;
        }
    }

    private void FixFusedWall(GameObject fusedWall, int wallLength, bool isNorthOrSouth)
    {
        Vector3 oldScale = fusedWall.transform.localScale;
        Vector3 oldPosition = fusedWall.transform.localPosition;
        Vector3 newScale, newPosition;

        if (isNorthOrSouth)
        {
            newScale = new Vector3(
                (oldScale.x - 1) * wallLength + generator.wallWidth,
                oldScale.y,
                oldScale.z);
            newPosition = new Vector3(
                oldPosition.x + generator.halfRemainingPieceWidth * (wallLength - 1),
                oldPosition.y,
                oldPosition.z);
        }
        else
        {
            newScale = new Vector3(oldScale.x,
                oldScale.y,
                (oldScale.z - 1) * wallLength + generator.wallWidth);
            newPosition = new Vector3(
                oldPosition.x,
                oldPosition.y,
                oldPosition.z - generator.halfRemainingPieceWidth * (wallLength - 1));
        }

        fusedWall.transform.localScale = newScale;
        fusedWall.transform.localPosition = newPosition;
        generator.FixUVs(fusedWall.transform);
    }
    #endregion
}
