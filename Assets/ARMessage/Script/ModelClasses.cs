using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ModelClasses : MonoBehaviour
{
    // Start is called before the first frame update    
    void Start()
    {
        
    }
}

[Serializable]
public class ar_message
{
    public locationsClass locations;
    public loggedinUsersClass users;
}



[Serializable]
public class locationsClass
{
    public locationElement[] user_locations;
}


[Serializable]
public class locationElement
{
    public string username;
    public string lat;
    public string lng;
}


[Serializable]
public class loggedinUsersClass
{
    public loggedinusersElement[] loggedinusers;
}

[Serializable]
public class loggedinusersElement
{
    public string username;
    public string isloggedin;
}

[Serializable]
public class gps_Instance
{
    public string lat;
    public string lng;
}
