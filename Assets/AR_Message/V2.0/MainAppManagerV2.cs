using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using UnityEngine.UI;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
public class MainAppManagerV2 : MonoBehaviour
{
    DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    protected bool isFirebaseInitialized = false;

    public GameObject FindingLocationScreen;
    public GameObject GPS_DisabledScreen;
    public GameObject HomeScreen;
    public InputField username_input;
    double FinalLat;
    double FinalLong;

    [Header("****** EDITOR ONLY ********")]
    public double EditorLat;
    public double EditorLng;

    DatabaseReference userslocation_reference;
    public gps_Instance gps_envelope;
    public GameObject userElement;
    //[Header("****** SELECTED USER ********")]
    //public string LocationJsonStart;
    //public string LocationJsonEnd;

    void Start()
    {
        #if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
        #endif

        HideAllScreens();
        FindingLocationScreen.SetActive(true);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
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

    void HideAllScreens()
    {
        HomeScreen.SetActive(false);
        FindingLocationScreen.SetActive(false);
        GPS_DisabledScreen.SetActive(false);
    }

    protected virtual void InitializeFirebase()
    {
        FirebaseApp app = FirebaseApp.DefaultInstance;
        isFirebaseInitialized = true;
        userslocation_reference = FirebaseDatabase.DefaultInstance.GetReference("users_location");
        InitGPSData();
    }

    void InitGPSData()
    {
        #if UNITY_EDITOR
            HideAllScreens();
            HomeScreen.SetActive(true);
            Vector2 _Location = new Vector2((float)EditorLat, (float)EditorLng);
            GPSEncoder.SetLocalOrigin(_Location);
            FinalLat = EditorLat;
            FinalLong = EditorLng;
            StartListner();
        #endif

        #if !UNITY_EDITOR
                StartCoroutine(GetLocation());
        #endif
    }

    IEnumerator GetLocation()
    {
        if (!Input.location.isEnabledByUser)
        {
            HideAllScreens();
            GPS_DisabledScreen.SetActive(true);
            yield break;
        }

        Input.location.Start();
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            Debug.LogError("Timed out");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            StartCoroutine(GetLocation());
            yield break;
        }
        else
        {
            HideAllScreens();
            HomeScreen.SetActive(true);
            StartListner();
        }
    }

    void StartListner()
    {
        userslocation_reference.ChildChanged += HandleChild_usersLocation_Changed;
        userslocation_reference.ChildAdded += HandleChild_usersLocation_Added;
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

        gps_envelope.lat=FinalLat.ToString();
        gps_envelope.lng = FinalLong.ToString();

        Vector2 _Location = new Vector2((float)FinalLat, (float)FinalLong);
        GPSEncoder.SetLocalOrigin(_Location);

    }

    public void OnClickGoButton()
    {
        if(username_input.text!="" && username_input.text!=null)
        {
            string jsonString = JsonUtility.ToJson(gps_envelope);
            userslocation_reference.Child(username_input.text).SetRawJsonValueAsync(jsonString);
            HideAllScreens();
        }

    }

    void HandleChild_usersLocation_Changed(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        string json =  args.Snapshot.GetRawJsonValue();
        print(" CHANGED json : " + args.Snapshot.Key);
        if (args.Snapshot.Key!= username_input.text)
        {
            GameObject tmp = GameObject.Find(args.Snapshot.Key);
            if (tmp)
            {
                float tmpLat = float.Parse(args.Snapshot.Child("lat").Value.ToString());
                float tmpLng = float.Parse(args.Snapshot.Child("lng").Value.ToString());
                print("lat : " + tmpLat);
                print("lng : " + tmpLng);
                Vector3 Pos = GPSEncoder.GPSToUCS(tmpLat, tmpLng);
                tmp.transform.localPosition = Pos;
                double Dis = DistanceTo(FinalLat, FinalLong, tmpLat, tmpLng);
                if (Dis < 1)
                {
                    tmp.SetActive(true);
                }
                else
                {
                    tmp.SetActive(false);
                }
            }
        }
        else
        {
            GameObject tmp = GameObject.Find(args.Snapshot.Key);
            if (tmp)
            {
                tmp.SetActive(false);
            }
        }
    }

    void HandleChild_usersLocation_Added(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        string json =  args.Snapshot.GetRawJsonValue() ;
        if (args.Snapshot.Key!=username_input.text)
        {
            print("ADDED json : " + json);
            GameObject tmp = Instantiate(userElement);
            tmp.name = args.Snapshot.Key;
            float tmpLat = float.Parse(args.Snapshot.Child("lat").Value.ToString());
            float tmpLng = float.Parse(args.Snapshot.Child("lng").Value.ToString());
            Vector3 Pos = GPSEncoder.GPSToUCS(tmpLat, tmpLng);
            tmp.transform.localPosition = Pos;
            double Dis = DistanceTo(FinalLat, FinalLong, tmpLat, tmpLng);
            if (Dis < 1)
            {
                tmp.SetActive(true);
            }
            else
            {
                tmp.SetActive(false);
            }
        }
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
