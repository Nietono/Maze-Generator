using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CruddyCameraControls : MonoBehaviour {

    private MazeGenerator generator;
    private Vector3 previousMousePosition;
    private Vector3 midPoint;

	// Use this for initialization
	void Start () {
        previousMousePosition = Input.mousePosition;
        generator = Object.FindObjectOfType<MazeGenerator>();
        midPoint = new Vector3(generator.mazeX * 4.5F, 0, generator.mazeY * -4.5F);
    }
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButton(1))
        {
            if (Input.mousePosition.y > previousMousePosition.y)
            {
                this.transform.RotateAround(midPoint, Vector3.right, -1);
            }
            else if (Input.mousePosition.y < previousMousePosition.y)
            {
                this.transform.RotateAround(midPoint, Vector3.right, 1);
            }

            if (Input.mousePosition.x > previousMousePosition.x)
            {
                this.transform.RotateAround(midPoint, Vector3.up, 1);
            }
            else if (Input.mousePosition.x < previousMousePosition.x)
            {
                this.transform.RotateAround(midPoint, Vector3.up, -1);
            }
        }

        previousMousePosition = Input.mousePosition;
	}
}
