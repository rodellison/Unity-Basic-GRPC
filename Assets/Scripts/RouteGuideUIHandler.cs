﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Routeguide;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

public class RouteGuideUIHandler : MonoBehaviour
{
    [SerializeField] private string gRpcHost = "127.0.0.1";

    [SerializeField] private string gRpcPort = "10000";

    private RouteGuideUnityClient _routeGuideClient;

    [SerializeField] private GameObject _textPrefab;
    [SerializeField] private GameObject _contentParent;


    // Callback signature
    public delegate void UICallback(string message, bool async);

    // Event declaration
    public event UICallback OnUICallback;

    /// <summary>
    /// The AddTMPTextOnMainThread IEnumerator is a coroutine to be used (by way of another component -
    /// UnityMainThreadDispatcher), as the code to run on the main thread, which updates the Unity UI.
    /// </summary>
    /// <param name="textDataToAdd">Text data to have added to the TMP Text UI component</param>
    /// <returns></returns>
    private IEnumerator AddTMPTextOnMainThread(string textDataToAdd)
    {
        var newTMPText = GameObject.Instantiate(_textPrefab, _contentParent.transform);
        newTMPText.GetComponent<TextMeshProUGUI>().SetText(textDataToAdd);
        yield return null;
    }

    /// <summary>
    /// AddTextToUI is the method that will handle updating the UI.
    /// </summary>
    /// <param name="textData">Text data to provide to the Enqueued IEnumerator function</param>
    /// <param name="async">bool value to instruct whether to use the Thread Dispatcher to update UI</param>
    public void AddTextToUi(string textData, bool async)
    {
        if (async)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(AddTMPTextOnMainThread(textData));
        }
        else
        {
            var newTMPText = GameObject.Instantiate(_textPrefab, _contentParent.transform);
            newTMPText.GetComponent<TextMeshProUGUI>().SetText(textData);
        }
    }

    private void Awake()
    {
#if UNITY_EDITOR   //do this to ensure Debug oriented logging doesn't occur when running outside of editor...
        Debug.unityLogger.logEnabled = true;
#else
        Debug.unityLogger.logEnabled = false;
#endif
    }

    private void Start()
    {
        _routeGuideClient = new RouteGuideUnityClient(gRpcHost, gRpcPort, this);
        OnUICallback += AddTextToUi;
    }

    /// <summary>
    /// Method to clear out previous TMP Text controls from the Content parent
    /// </summary>
    private void ClearTMPTextChildren()
    {
        foreach (Transform child in _contentParent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// This method front ends a gRPC client method which makes a call to the remote gRPC server, passing a POINT
    /// Message type, and subsequently having the server response be the Feature Point's Name.
    /// Essentially - Client single REQUEST/Server single RESPONSE
    /// </summary>
    public async void GetSingleFeature()
    {
        ClearTMPTextChildren();
        var pointOfInterest = new Routeguide.Point {Latitude = 409146138, Longitude = -746188906};
        await _routeGuideClient.GetFeature(pointOfInterest);

        Debug.Log("GetSingleFeature Finished");
    }

    /// <summary>
    /// This method front ends a gRPC client method which makes a call to the remote gRPC server, passing a SET of
    /// Two POINTs within a Rectangle Message Type. 
    /// The response from the gRPC server is an asynchronous STREAM of Feature Message types
    /// Essentially - Single Client REQUEST/Server-side asynchronous response STREAM
    /// </summary>
    public async void GetMultipleFeatures()
    {
        ClearTMPTextChildren();

        var pointOfInterestLo = new Routeguide.Point {Latitude = 400000000, Longitude = -750000000};
        var pointOfInterestHi = new Routeguide.Point {Latitude = 420000000, Longitude = -730000000};

        var areaOfInterest = new Routeguide.Rectangle
        {
            Lo = pointOfInterestLo,
            Hi = pointOfInterestHi,
        };

        await _routeGuideClient.ListFeatures(areaOfInterest);

        Debug.Log("GetMultipleFeatures Finished");
    }

    /// This method front ends a gRPC client method which makes a call to the remote gRPC server, passing a SET of
    /// two or more randomly generated POINTs as an asynchronous STREAM. 
    /// The response from the gRPC server is calculated summary (string) of the distance between all the
    /// points.
    /// Essentially - Client-side asynchronous STREAM/Server single Response
    /// </summary>
    public async void GetPointsRouteSummary()
    {
        ClearTMPTextChildren();

        var pointCount = UnityEngine.Random.Range(1, 100) + 1; // Traverse at least two points
        var points = new Routeguide.Point[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            points[i] = randomPointOfInterest();
        }

        Debug.Log("GetPointsRouteSummary traversing " + points.Length.ToString() + " points");

        await _routeGuideClient.RecordRoute(points);
        Debug.Log("GetPointsRouteSummary Finished");
    }

    /// <summary>
    /// This method front ends a gRPC client method which makes a call to the remote gRPC server, passing
    /// a SET of RouteNote Message types over an asynchronous STREAM.
    /// SIMULTANEOUSLY, The response from the gRPC server is an asynchronous STREAM of the collected Route Notes.
    /// Essentially - BI-DIRECTIONAL Client and Server STREAMING
    /// </summary>
    public async void RunRouteChat()
    {
        ClearTMPTextChildren();

        //Create bunch of Notes (each note contains a Point, and a Name), that will be 
        //sent over via a STREAM
        RouteNote[] notes = new RouteNote[]
        {
            new RouteNote
            {
                Location = new Point {Latitude = 0, Longitude = 1},
                Message = "First message",
            },
            new RouteNote
            {
                Location = new Point {Latitude = 0, Longitude = 2},
                Message = "Second message",
            },
            new RouteNote
            {
                Location = new Point {Latitude = 0, Longitude = 3},
                Message = "Third message",
            },
            new RouteNote
            {
                Location = new Point {Latitude = 0, Longitude = 4},
                Message = "Fourth message",
            },
            new RouteNote
            {
                Location = new Point {Latitude = 0, Longitude = 5},
                Message = "Fifth message",
            },
            new RouteNote
            {
                Location = new Point {Latitude = 0, Longitude = 6},
                Message = "Sixth message",
            }
        };

        await _routeGuideClient.RouteChat(notes);
        Debug.Log("RunRouteChat Finished");
    }


    private Routeguide.Point randomPointOfInterest()
    {
        var lat = (UnityEngine.Random.Range(1, 180) - 90) * 1e7;
        var lon = (UnityEngine.Random.Range(1, 360) - 180) * 1e7;
        var randomPointOfInterest = new Routeguide.Point
            {Latitude = Convert.ToInt32(lat), Longitude = Convert.ToInt32(lon)};
        return randomPointOfInterest;
    }
}