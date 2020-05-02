using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
public class MainAppManager : MonoBehaviour
{

    DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    protected bool isFirebaseInitialized = false;


    [Header("****** EDITOR ONLY ********")]
    public double EditorLat;
    public double EditorLng;
    //public GameObject CameraObj;

    public bool IsUserSelected = false;
    public Transform UserGroupParent;

    DatabaseReference Location_reference;
    DatabaseReference Users_reference;
    public GameObject[] UsersMarker;

    [Header("****** SELECTED USER ********")]
    public locationsClass locationsClassInstance;
    public string SelectedUser;
    public string LocationJsonStart;
    public string LocationJsonEnd;
    void Start()
    {
        #if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
        #endif

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.Log("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }


    private void Update()
    {
        #if UNITY_EDITOR
            FinalLat = EditorLat;
            FinalLong = EditorLng;
        #endif

        #if !UNITY_EDITOR
            FinalLat = Input.location.lastData.latitude;
            FinalLong = Input.location.lastData.longitude;
        #endif
    }
    public void GetAvaialbleUsers()
    {
        if(!IsUserSelected)
        {
            IsUserSelected = true;
            Users_reference.ChildAdded += HandleChildAdded;
            Users_reference.ChildChanged += HandleChildChanged;
        }
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        foreach (var childSnapshot in args.Snapshot.Children)
        {
            if (childSnapshot.Child("username") == null
            || childSnapshot.Child("username").Value == null)
            {
                Debug.LogError("Bad data in sample.  Did you forget to call SetEditorDatabaseUrl with your project id?");
                break;
            }
            else
            {
                //Debug.Log("#### username : " + childSnapshot.Child("username").Value.ToString());
                string username = childSnapshot.Child("username").Value.ToString();
                string isloggedin = childSnapshot.Child("isloggedin").Value.ToString();
                if(isloggedin=="no")
                {
                    foreach (Transform Userchild in UserGroupParent)
                    {
                        if (Userchild.name == username)
                        {
                            Userchild.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        foreach (var childSnapshot in args.Snapshot.Children)
        {
            if (childSnapshot.Child("username") == null
            || childSnapshot.Child("username").Value == null)
            {
                Debug.LogError("Bad data in sample.  Did you forget to call SetEditorDatabaseUrl with your project id?");
                break;
            }
            else
            {
                //Debug.Log("#### username : " + childSnapshot.Child("username").Value.ToString() +" STATUS :"
                    //+ childSnapshot.Child("isloggedin").Value.ToString());

            }
        }
    }
    //public bool IsLocationFound;

    IEnumerator GetLocation()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield break;

        // Start service before querying location
        Input.location.Start();

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            print("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            print("Unable to determine device location");
            yield break;
        }
        else
        {
            GetAvaialbleUsers();
            //CalculateDistanceFromEachLocation();
        }
    }

    double FinalLat;
    double FinalLong;

    protected virtual void InitializeFirebase()
    {
        FirebaseApp app = FirebaseApp.DefaultInstance;
        isFirebaseInitialized = true;
        Users_reference = FirebaseDatabase.DefaultInstance.GetReference("users");
        Location_reference= FirebaseDatabase.DefaultInstance.GetReference("locations");
        InitGPSData();
    }

    void InitGPSData()
    {
        #if UNITY_EDITOR
            //CalculateDistanceFromEachLocation();
            Vector2 _Location = new Vector2((float)EditorLat, (float)EditorLng);
            GPSEncoder.SetLocalOrigin(_Location);
            GetAvaialbleUsers();
        #endif

        #if !UNITY_EDITOR
            StartCoroutine(GetLocation());
        #endif
    }
    public void OnUserSelected(string selected_user)
    {
        SelectedUser = selected_user;
        int userIndex = int.Parse(selected_user.Substring(4));
        userIndex = userIndex - 1;
        Users_reference.Child("loggedinusers").Child(userIndex.ToString()).Child("isloggedin").SetValueAsync("yes");
        Location_reference.ChildChanged += HandleChildLocationChanged;
        //Location_reference.ChildAdded += HandleChildLocationAdded;
        Invoke("UpdateLocationToServer", 0.5f);
        UserGroupParent.gameObject.SetActive(false);
        Invoke("SetUsersMarkerLocations", 1);
    }

   

    void UpdateLocationToServer()
    {
        int userIndex = int.Parse(SelectedUser.Substring(4));
        userIndex = userIndex - 1;
        Location_reference.Child("user_locations").Child(userIndex.ToString()).Child("lat").SetValueAsync(FinalLat.ToString());
        Location_reference.Child("user_locations").Child(userIndex.ToString()).Child("lng").SetValueAsync(FinalLong.ToString());
    }

    void HandleChildLocationChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        //print("Snapshot : " + args.Snapshot.GetRawJsonValue());
        string json = LocationJsonStart + args.Snapshot.GetRawJsonValue() + LocationJsonEnd;
        //print("json : " + json);
        locationsClassInstance=JsonUtility.FromJson<locationsClass>(json);
    }
    
    void HandleChildLocationAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        foreach (var childSnapshot in args.Snapshot.Children)
        {
            //Debug.Log("#### HandleChildLocationAdded : " + childSnapshot.Key);
        }
    }

    public void SetUsersMarkerLocations()
    {
        for (int i = 0; i < locationsClassInstance.user_locations.Length; i++)
        {
            Vector2 _Location = new Vector2((float)FinalLat, (float)FinalLong);
            GPSEncoder.SetLocalOrigin(_Location);

            double Dis = DistanceTo(FinalLat, FinalLong,double.Parse(locationsClassInstance.user_locations[i].lat),
                double.Parse( locationsClassInstance.user_locations[i].lng));
            Vector3 Pos = GPSEncoder.GPSToUCS(float.Parse(locationsClassInstance.user_locations[i].lat),
                float.Parse(locationsClassInstance.user_locations[i].lng));
            UsersMarker[i].transform.localPosition = Pos;

            if (Dis < 1)
            {
                UsersMarker[i].SetActive(true);
            }
            else
            {
                UsersMarker[i].SetActive(false);
            }
        }
        Invoke("SetUsersMarkerLocations", 1f);
    }

    public static double DistanceTo(double lat1, double lon1, double lat2, double lon2, char unit = 'K')
    {
        double rlat1 = Math.PI * lat1 / 180;
        double rlat2 = Math.PI * lat2 / 180;
        double theta = lon1 - lon2;
        double rtheta = Math.PI * theta / 180;
        double dist =
        Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) *
        Math.Cos(rlat2) * Math.Cos(rtheta);
        dist = Math.Acos(dist);
        dist = dist * 180 / Math.PI;
        dist = dist * 60 * 1.1515;

        switch (unit)
        {
            case 'K': //Kilometers -> default
                return dist * 1.609344;
            case 'N': //Nautical Miles 
                return dist * 0.8684;
            case 'M': //Miles
                return dist;
        }

        return dist;
    }
}
