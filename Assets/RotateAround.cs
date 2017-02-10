using UnityEngine;
using System.Collections;

public class RotateAround : MonoBehaviour {
  
  public Vector3 RotationPivot = new Vector3(0, 100, 0);
  public float RotationCoef;

	// Use this for initialization
	void Start () {
    transform.RotateAround(RotationPivot, transform.right, 50);
    transform.RotateAround(RotationPivot, transform.up, 50);
	
	}
	
	// Update is called once per frame
	void Update () {
    //transform.RotateAround(RotationPivot, transform.up, 10);
	
	}
}
