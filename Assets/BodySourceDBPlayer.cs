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


public class BodySourceDBPlayer : MonoBehaviour 
{
    private int _FrameCount = 0;
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
    private Thread _backthread;

    private const string connectionString = "mongodb://localhost";
    private const string MongoDatabase = "skeletondb";

    public int cameraNumber = 1;
    private int _maxFrameSize = 0;
    private MongoDB.Driver.MongoCollection<MongoDB.Bson.BsonDocument> _dbcollection;
    private int _fetchesPitch = 100;


    void Start () 
    {
        if (_eBodies == null)
        {
            /*
            // usage:  collection.Find(query).SetSkip(0).SetLimit(1).Size();
            */
            var server = MongoServer.Create("mongodb://localhost");
            var db = server.GetDatabase( "skeletondb" );
            _dbcollection = db.GetCollection( "skeleton" );
            _maxFrameSize = _dbcollection.Find(Query.EQ("camera", cameraNumber)).Size();
            QueingDB(cameraNumber, _FrameCount, _fetchesPitch);
            System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(2000));
        }   
    }
    
    void Update () 
    {

        if (_dbDatas != null)
        {
            if (_FrameCount < _maxFrameSize)
            {
                var currentFrame = _FrameCount % _fetchesPitch;
                
                _eBodies = JsonConvert.DeserializeObject<EmitBody[]>(_dbDatas[currentFrame].bodies);
                _EData = _eBodies;
                FloorClipPlane = _dbDatas[currentFrame].floorClipPlane;
                CameraAngle = getCameraAngle(FloorClipPlane);
               
               
                long _time = 0;
                _time = currentFrame != 0 ? 
                _dbDatas[currentFrame].timestamp - _dbDatas[currentFrame - 1].timestamp :
                _dbDatas[currentFrame].timestamp - _dbDatas[0].timestamp;
                System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(_time));
                _FrameCount += 1;
            }    
            else 
            {
                _FrameCount = 0;
            }

            QueingDB(1, _FrameCount, _fetchesPitch);

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

    private void FetchDB(int cameraNum, int skip, int limit) 
    {
        _buffer = new List<DBFrame>();
        _backthread = new Thread(() => {
            var res = _dbcollection.Find(Query.EQ("camera", cameraNum)).SetSkip(skip).SetLimit(limit);

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
        });
        _backthread.Start();
    }

    private void QueingDB(int cameraNum, int counter, int period)
    {

        if (counter == 0) 
        {
            _dbDatas = new List<DBFrame>();
            Debug.Log("start");
            FetchDB(cameraNum, 0, period);
            _dbDatas = _buffer;
            return;
        }
        if (counter % (period / 2) == 0 && counter % period != 0) 
        {
            FetchDB(cameraNum, 100, 100);
            return;
        }

        if (counter % period != 0)
        {
            return;
        } else {
            _dbDatas = new List<DBFrame>();
            Debug.Log("set");
            _dbDatas = _buffer;
            if (_backthread != null)
            {
                Debug.Log("thead delete");
                _backthread.Abort();
                _backthread = null;
            }
            return;
        }

    }


    void OnGUI () 
    {
        // テキストフィールドを表示する
        GUI.TextField(new Rect(10, 10, 300, 20), _FrameCount.ToString());
    }
    
}



/*

            PropertyInfo[] infoArray = _testbody.GetType().GetProperties();
            foreach (PropertyInfo info in infoArray)
            {
                Debug.Log(info.Name + ": " + info.GetValue(_testbody,null));
            }

*/