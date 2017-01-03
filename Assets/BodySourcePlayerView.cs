using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using Newtonsoft.Json;
using System.Linq;

public class BodySourcePlayerView : MonoBehaviour 
{
    public Material BoneMaterial;
    public GameObject BodySourcePlayer;
    
    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourcePlayer _BodyManager;

    public string _Path =  System.IO.Directory.GetCurrentDirectory() + "//SerializationConfirm.json";

    public int RotationCoef = 0;
    private Vector3 RotationPivot = new Vector3(0, 1, 0);
    private Vector3 groundPosition;
    
    private Dictionary<EJointType, EJointType> _BoneMap = new Dictionary<EJointType, EJointType>()
    {
        { EJointType.FootLeft, EJointType.AnkleLeft },
        { EJointType.AnkleLeft, EJointType.KneeLeft },
        { EJointType.KneeLeft, EJointType.HipLeft },
        { EJointType.HipLeft, EJointType.SpineBase },
        
        { EJointType.FootRight, EJointType.AnkleRight },
        { EJointType.AnkleRight, EJointType.KneeRight },
        { EJointType.KneeRight, EJointType.HipRight },
        { EJointType.HipRight, EJointType.SpineBase },
        
        { EJointType.HandTipLeft, EJointType.HandLeft },
        { EJointType.ThumbLeft, EJointType.HandLeft },
        { EJointType.HandLeft, EJointType.WristLeft },
        { EJointType.WristLeft, EJointType.ElbowLeft },
        { EJointType.ElbowLeft, EJointType.ShoulderLeft },
        { EJointType.ShoulderLeft, EJointType.SpineShoulder },
        
        { EJointType.HandTipRight, EJointType.HandRight },
        { EJointType.ThumbRight, EJointType.HandRight },
        { EJointType.HandRight, EJointType.WristRight },
        { EJointType.WristRight, EJointType.ElbowRight },
        { EJointType.ElbowRight, EJointType.ShoulderRight },
        { EJointType.ShoulderRight, EJointType.SpineShoulder },
        
        { EJointType.SpineBase, EJointType.SpineMid },
        { EJointType.SpineMid, EJointType.SpineShoulder },
        { EJointType.SpineShoulder, EJointType.Neck },
        { EJointType.Neck, EJointType.Head },
    };
    
    void Update () 
    {
        if (BodySourcePlayer == null)
        {
            return;
        }
        _BodyManager = BodySourcePlayer.GetComponent<BodySourcePlayer>();
        if (_BodyManager == null)
        {
            return;
        }
        
        EmitBody[] data = _BodyManager.EGetData();
        if (data == null)
        {
            return;
        }

        List<ulong> trackedIds = new List<ulong>();
        groundPosition =  transform.position;

        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
              }
                
            if(body.IsTracked)
            {   
                trackedIds.Add (body.TrackingId);
            }
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
            }
            
            if(body.IsTracked)
            {
                if(!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }
                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }
    }
    
    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);
        
        for (EJointType jt = EJointType.SpineBase; jt <= EJointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            LineRenderer lr = jointObj.AddComponent<LineRenderer>();
            lr.SetVertexCount(2);
            lr.material = BoneMaterial;
            lr.SetWidth(0.05f, 0.05f);
            
            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;
        }
        
        return body;
    }
    
    private void RefreshBodyObject(EmitBody body, GameObject bodyObject)
    {
        for (EJointType jt = EJointType.SpineBase; jt <= EJointType.ThumbRight; jt++)
        {
            EJoint sourceJoint = body.Joints[jt];
            EJoint? targetJoint = null;
            
            if(_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }
            
            Transform jointObj = bodyObject.transform.FindChild(jt.ToString());
            jointObj.localPosition = GetVector3FromJoint(sourceJoint, groundPosition);
            jointObj.RotateAround(RotationPivot, transform.up, RotationCoef);
            
            LineRenderer lr = jointObj.GetComponent<LineRenderer>();
            if(targetJoint.HasValue)
            {
                lr.SetPosition(0, jointObj.localPosition);

                Vector3 endpoint = RotateAroundPoint(GetVector3FromJoint(targetJoint.Value, groundPosition), RotationPivot, Quaternion.Euler(0, RotationCoef, 0));
                lr.SetPosition(1, endpoint);
                lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
            }
            else
            {
                lr.enabled = false;
            }
        }
    }
    
    private static Color GetColorForState(ETrackingState state)
    {
        switch (state)
        {
        case ETrackingState.Tracked:
            return Color.green;

        case ETrackingState.Inferred:
            return Color.red;

        default:
            return Color.black;
        }
    }
    
    private static Vector3 GetVector3FromJoint(EJoint joint,  Vector3 groundPosition)
    {
        return new Vector3(
            (joint.Position.X * 10) + groundPosition.x, 
            (joint.Position.Y * 10) + groundPosition.y, 
            (joint.Position.Z * 10) + groundPosition.z
        );
    }
    
    private static Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle) 
    {
     return angle * ( point - pivot) + pivot;
    }
}
