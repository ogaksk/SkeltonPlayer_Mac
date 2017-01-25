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
    public FloorClipPlane EGetFloorPlane()  
    { 
        return _FloorPlane;
    }

    public List<JsonFrame> _jsonDatas = new List<JsonFrame>();
    public List<DBFrame> _dbDatas = new List<DBFrame>();

    private const string connectionString = "mongodb://localhost";
    private const string MongoDatabase = "skeletondb";

    public int cameraNumber = 1;


    void Start () 
    {

        if (_eBodies == null)
        {
            /*
            // usage:  collection.Find(query).SetSkip(0).SetLimit(1).Size();
            */
            var server = MongoServer.Create("mongodb://localhost");
            var db = server.GetDatabase( "skeletondb" );
            var collection = db.GetCollection( "skeleton" );
            var res = collection.Find(Query.EQ("camera", cameraNumber)).SetLimit(100);


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
                _dbDatas.Add(dbf);
            }

        }   
    }
    
    void Update () 
    {

        if (_dbDatas != null)
        {
            if (_FrameCount < _dbDatas.Count)
            {
                _eBodies = JsonConvert.DeserializeObject<EmitBody[]>(_dbDatas[_FrameCount].bodies);
                _EData = _eBodies;
                _FloorPlane = _dbDatas[_FrameCount].floorClipPlane;

                //Debug.Log( _jsonDatas[_FrameCount].timestamp); // アクセスできた
               
                long _time = 0;
                _time = _FrameCount != 0 ? 
                _dbDatas[_FrameCount].timestamp - _dbDatas[_FrameCount - 1].timestamp :
                _dbDatas[_FrameCount].timestamp - _dbDatas[0].timestamp;
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