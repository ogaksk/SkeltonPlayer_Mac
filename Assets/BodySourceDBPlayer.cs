using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
// using MongoDB.Driver.Linq;

class ThreadInfo
{
    public int threadIndex;
    
}


public class BodySourceDBPlayer : MonoBehaviour 
{
    // private int _FrameCount = 0;
    public System.IO.FileStream _File;  
    private EmitBody[] _EData = null;
    public EmitBody[] _eBodies  { get; set; }
    public FloorClipPlane _FloorPlane;
    public EmitBody[] EGetData()  
    { 
        return _EData;
    }

    public FloorClipPlane FloorClipPlane
    {
        get;
        set;
    }

    public float CameraAngle
    {
        get;
        set;
    }

    public List<JsonFrame> _jsonDatas = new List<JsonFrame>();
    public List<DBFrame> _dbDatas;
    public List<DBFrame> _buffer;

    public string DataId;
    private Thread _backthread;
    private int maxThreads = 10; // set your max threads here
    static readonly object _countLock = new object();
    static int _threadCount = 0;
    static bool closingApp = false;


    private const string connectionString = "mongodb://localhost";
    private const string MongoDatabase = "skeletondb";

    public int cameraNumber = 1;
    private int _maxFrameSize = 0;
    private MongoDB.Driver.MongoCollection<MongoDB.Bson.BsonDocument> _dbcollection;
    private int Turn = 0;
    

    private List<long> FrameTimeList;
    private List<int> FrameList;
    private int currentFirstFrame = 0;
    private int timeLength = 20000;

    private int ThisNowFrameIndex = 0;
    private int ThisNowFrame = 0;
    private int ThisNowTime = 0;

    private bool debugButton = false;
    

    void Start () 
    {
        Time.captureFramerate = 25;
        if (_eBodies == null)
        {
            /*
            // usage:  collection.Find(query).SetSkip(0).SetLimit(1).Size();
            */
            var server = MongoServer.Create("mongodb://localhost");
            var db = server.GetDatabase( "skeletondb" );
            _dbcollection = db.GetCollection( "skeleton" );
            _maxFrameSize = _dbcollection.Find(Query.And(Query.EQ("camera", cameraNumber), Query.EQ("id", DataId)) ).Size();
            QueingDB(0);
        }   
    }
    
    void Update () 
    {
        
        if (_dbDatas != null)
        {
            var _FrameCount = Clock.Counter;
             
            ThisNowFrameIndex =  FrameList.FindIndex (x => x.Equals(_FrameCount) );
            ThisNowFrame = ThisNowFrameIndex == -1 ? -1 : FrameList[ThisNowFrameIndex];
            if ( ThisNowFrameIndex != -1 )
            {
                _eBodies = JsonConvert.DeserializeObject<EmitBody[]>(_dbDatas[ThisNowFrameIndex].bodies);  
                ThisNowTime = (int)(_dbDatas[ThisNowFrameIndex].timestamp * 0.001f);
                _EData = _eBodies;
                FloorClipPlane = _dbDatas[ThisNowFrameIndex].floorClipPlane;
                CameraAngle = getCameraAngle(FloorClipPlane);
            }

            QueingDB(_FrameCount);
        }  

        if (Input.GetKeyDown(KeyCode.Space)) 
        {
           debugButton = true;
        }

        
        
        
    }
    
    void OnApplicationQuit()
    {

        if (_File != null)
        {
            _File.Close();
        }
        
    }


    private static float getCameraAngle (FloorClipPlane _floor) 
    {
        double cameraAngleRadians = System.Math.Atan(_floor.Z / _floor.Y); 
        // return System.Math.Cos(cameraAngleRadians); 
         return (float)(cameraAngleRadians * 180 / System.Math.PI);
    }


    private void FetchDB(System.Object state) 
    {
        // Constrain the number of worker threads, loop here until less than maxthreads are running
        while (!closingApp)
        {
            // Prevent other threads from changing this under us
            lock (_countLock)
            {
                if (_threadCount < maxThreads && !closingApp)
                {
                    // Start processing
                    _threadCount++;
                    break;
                }
            }
            Thread.Sleep(50);
        }

         if (closingApp) return;

        _buffer = new List<DBFrame>();
        var query = Query.And(
            Query.EQ("camera", cameraNumber), 
            Query.EQ("id", DataId), 
            Query.GTE( "timestamp", Turn * timeLength),
            Query.LTE( "timestamp", (Turn + 1) * timeLength )
            );

        var res = _dbcollection.Find(query);

        foreach (var item in res)
        {
            // values.Add(item);
            DBFrame dbf = new DBFrame();  
            dbf.timestamp =  System.Convert.ToInt64(item["timestamp"]);
            dbf.camera = (int)item["camera"];
            dbf.floorClipPlane = JsonConvert.DeserializeObject<FloorClipPlane>(
                MongoDB.Bson.BsonExtensionMethods.ToJson(item["floorclipplane"])
            );
            dbf.bodies = MongoDB.Bson.BsonExtensionMethods.ToJson(item["data"]);
            _buffer.Add(dbf);
        }

        refreshTimeList();
        // decrease thread counter, so other threads can start
        _threadCount--;

        
    }

    private void QueingDB(int counter)
    {
        if (counter == 0) 
        {
            Debug.Log("que start");
            _dbDatas = new List<DBFrame>();
            /* fetch db */
            System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(FetchDB));
            System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(2000));
            _dbDatas = _buffer;
            convertTimeToFrame();
            Turn += 1;
            return;
        }

        if (debugButton == true) 
        {
            var durrationTime = 400000;
            Clock.Counter = TimeToFrame(durrationTime);
            Turn =  TimeToTurn(durrationTime);
            debugButton =  false;
        }

        if ( counter % ( NextRefleshFrame() + (cameraNumber*30) ) == 0 
            && counter % NextSetFrame() != 0 ) 
        {
            Debug.Log("----------------------------------buffering.----------------------------------."+cameraNumber+"..----------------------------------" );

            /* fetch db */
            System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(FetchDB));
            return;
        }

        if (counter % NextSetFrame() != 0)
        {
            return;
        } else {
            _dbDatas = new List<DBFrame>();
            Debug.Log("!!!!!!!!!!!!!set!!!!!!!!!!!!-."+cameraNumber+"..-!");
            _dbDatas = _buffer;
            convertTimeToFrame();
            Turn += 1;
            if (_backthread != null)
            {
                Debug.Log("thead delete");
                _backthread.Abort();
                _backthread = null;
            }
            return;
        }

    }

    private void refreshTimeList ()
    {
        Debug.Log("----------------------------------time list refresh start----------"+cameraNumber+"------------------------");
        FrameTimeList = new List<long>();
        _buffer.ForEach((val) => {
            long a = val.timestamp;
            FrameTimeList.Add(val.timestamp);
        });
        Debug.Log("last time is"+  FrameTimeList[FrameTimeList.Count - 1]);
    }

    private void convertTimeToFrame ()
    {
        Debug.Log("convert start-."+cameraNumber+"..-");
        FrameList = new List<int>();
        float fps = 30f;
            
        Debug.Log("FrameTimeList.Count- is."+FrameTimeList.Count+"..."+cameraNumber+"..--");
        for (int i = 0; i < FrameTimeList.Count; ++i) 
        {
            
            int frame = (int)System.Math.Round(FrameTimeList[i] / fps, System.MidpointRounding.AwayFromZero);
            FrameList.Add(frame);
        }
        Debugger.List(FrameList);
        currentFirstFrame = FrameList[0];

    }

    private int NextRefleshFrame ()
    {
        return (int)((( (Turn-1) * timeLength) + (timeLength / 2)) / 30f); // きたねぇ

    }

    private int NextSetFrame ()
    {
        return  (int)(((Turn) * timeLength) / 30f);
    }

    private int TimeToFrame (int time)
    {
         return  (int)(time / 30f);
    }

    private int TimeToTurn (int time) 
    {
         return  (int)System.Math.Floor((double)(time / timeLength));
    }

    void OnGUI () 
    {
        // テキストフィールドを表示する
        //GUI.TextField(new Rect(10, 10, 300, 20), _FrameCount.ToString());
        GUI.TextField(new Rect( 40 *cameraNumber, 10, 40, 20), ThisNowTime.ToString() );
        GUI.TextField(new Rect( 40 *cameraNumber, 30, 40, 30), Clock.Counter.ToString() );
    }


    void OnDestroy()
    {
        closingApp = true;
    }
    
}



/*

            PropertyInfo[] infoArray = _testbody.GetType().GetProperties();
            foreach (PropertyInfo info in infoArray)
            {
                Debug.Log(info.Name + ": " + info.GetValue(_testbody,null));
            }

*/