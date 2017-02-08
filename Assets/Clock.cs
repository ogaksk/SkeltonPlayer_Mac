using UnityEngine;
using System.Collections;

public class Clock : MonoBehaviour {

  public static int Counter;

  private int click = 0;

	// Use this for initialization
	void Start () {
    Application.targetFrameRate = 24;
    Counter = 0;
	}
	
	// Update is called once per frame
	void Update () {
    // if (click % Fps == 0) 
    // {
    //   Counter += 1;
    //   click = 0;
    // }
    Counter += 1;
	}
}
