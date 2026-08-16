using System;
using UnityEngine;


[System.Serializable]
public class UserSessionData
{
    public readonly string UserID;
    public int ItemsFound;
    public DateTime LevelStartTime;
    public UserSessionData(){
        UserID=SystemInfo.deviceUniqueIdentifier;
        ItemsFound=0;
        LevelStartTime=DateTime.Now;
    }
}