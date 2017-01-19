using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class UserModel
{
    public int UserID { get; set; }
    public string Username { get; set; }
}

public class EmitBody
{
    public bool IsTracked { get; set; }
    public ulong TrackingId { get;  set; }
    // public Dictionary<JointType, Windows.Kinect.Joint> Joints { get; set; } // toggle under line. 
    public Dictionary<EJointType, EJoint> Joints { get; set; }
}

public enum EJointType 
 {
    SpineBase = 0,
    SpineMid,
    Neck,
    Head,
    ShoulderLeft,
    ElbowLeft,
    WristLeft,
    HandLeft,
    ShoulderRight,
    ElbowRight,
    WristRight,
    HandRight,
    HipLeft,
    KneeLeft,
    AnkleLeft,
    FootLeft, 
    HipRight,
    KneeRight,
    AnkleRight,
    FootRight,
    SpineShoulder,
    HandTipLeft,
    ThumbLeft,
    HandTipRight,
    ThumbRight
}

public enum ETrackingState
{
    Inferred = 0,
    NotTracked,
    Tracked
}

public struct EJoint 
{
    public EJointType JointType { get; set; }
    public EPosition Position { get; set; }
    public ETrackingState TrackingState { get; set; }
}

public struct EPosition
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class JsonFrame 
{
    public uint timestamp { get; set; }
    public string bodies { get; set; }
}

public class DBFrame 
{
    public long timestamp { get; set; }
    public int camera { get; set; }
    public string bodies { get; set; }
}

