using System;
using TMPro;
using UnityEngine;
using Routeguide;
using UnityEngine.Serialization;

public class RouteGuideUIHandler : MonoBehaviour
{
    [FormerlySerializedAs("gRPC_Host")] public string gRpcHost = "127.0.0.1";
    [FormerlySerializedAs("gRPC_Port")] public string gRpcPort = "10000";

    [SerializeField] private TextMeshProUGUI myTextMeshProUGui;
    private RouteGuideUnityClient _routeGuideClient;

    private void Start()
    {
        //Allowing the RouteGuideUnityClient the ability to send output to the Text component directly, by passing
        //a reference to the TMP as input.
        _routeGuideClient = new RouteGuideUnityClient(gRpcHost, gRpcPort, myTextMeshProUGui);
    }
    
    /// <summary>
    /// This method front ends a gRPC client method which makes a call to the remote gRPC server, passing a POINT
    /// Message type, and subsequently having the server response be the Feature Point's Name.
    /// Essentially - Client single REQUEST/Server single RESPONSE
    /// </summary>
    public async void GetSingleFeature()
    {

        var pointOfInterest = new Routeguide.Point {Latitude = 409146138, Longitude = -746188906};
        await _routeGuideClient.GetFeature(pointOfInterest);

        Debug.Log("GetSingleFeature Finished");
    }

    /// <summary>
    /// This method front ends a gRPC client method which makes a call to the remote gRPC server, passing a SET of
    /// Two POINTs within a Rectangle Message Type. 
    /// The response from the gRPC server is an asynchronous STREAM of Feature Message types
    /// Essentially - Single Client REQUEST/Server-side asyncrhonous response STREAM
    /// </summary>
    /// <param name="streamLoadUI">Parameter provided by the UI to designate whether the loading of streamed response
    /// data should be loaded AS-IT-RETURNS, or, whether to collect and load it in one shot.</param>
    public async void GetMultipleFeatures(bool streamLoadUI)
    {
        var pointOfInterestLo = new Routeguide.Point {Latitude = 400000000, Longitude = -750000000};
        var pointOfInterestHi = new Routeguide.Point {Latitude = 420000000, Longitude = -730000000};

        var areaOfInterest = new Routeguide.Rectangle
        {
            Lo = pointOfInterestLo,
            Hi = pointOfInterestHi,
        };

        await _routeGuideClient.ListFeatures(areaOfInterest, streamLoadUI);

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
    /// <param name="streamLoadUI">Parameter provided by the UI to designate whether the loading of streamed response
    /// data should be loaded AS-IT-RETURNS, or, whether to collect and load it in one shot.</param>
    public async void RunRouteChat(bool streamLoadUI)
    {
 
       
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

        await _routeGuideClient.RouteChat(notes, streamLoadUI);
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