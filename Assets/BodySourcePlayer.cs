using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;

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


public class BodySourcePlayer : MonoBehaviour 
{
    private int _FrameCount = 0;
    public System.IO.FileStream _File;  
    private EmitBody[] _EData = null;
    public string _Path =  System.IO.Directory.GetCurrentDirectory() + "//SerializationDummy.json";  
    public EmitBody[] _eBodies  { get; set; }
    public EmitBody[] EGetData()  
    { 
        return _EData;
    }
    public List<JsonFrame> _jsonDatas = new List<JsonFrame>();



    void Start () 
    {

        if (_eBodies == null)
        {
            string json = File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + "//SerializationOverview24721533.json");
            JArray[] fetchedData = JsonConvert.DeserializeObject<JArray[]>(json);

            foreach (var data in fetchedData.Select((v, i) => new { v, i })) 
            {
                JsonFrame jsf = new JsonFrame();
                jsf.timestamp = (uint)data.v[0];
                jsf.bodies =  Newtonsoft.Json.JsonConvert.SerializeObject(data.v[1]);
                _jsonDatas.Insert(data.i, jsf);
            }
        }   
    }
    
    void Update () 
    {

        if (_jsonDatas != null)
        {
            if (_FrameCount < _jsonDatas.Count)
            {
                _eBodies = JsonConvert.DeserializeObject<EmitBody[]>(_jsonDatas[_FrameCount].bodies);
                _EData = _eBodies;

                //Debug.Log( _jsonDatas[_FrameCount].timestamp); // アクセスできた
               
                uint _time = 0;
                _time = _FrameCount != 0 ? 
                _jsonDatas[_FrameCount].timestamp - _jsonDatas[_FrameCount - 1].timestamp :
                _jsonDatas[_FrameCount].timestamp - _jsonDatas[0].timestamp;
                System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(_time));
                _FrameCount += 1;
            }    
            else 
            {
                _FrameCount = 0;
            }
        }   
    }
    
    void OnApplicationQuit()
    {

        if (_File != null)
        {
            _File.Close();
        }
        
    }
    
}



/*

            PropertyInfo[] infoArray = _testbody.GetType().GetProperties();
            foreach (PropertyInfo info in infoArray)
            {
                Debug.Log(info.Name + ": " + info.GetValue(_testbody,null));
            }

*/