#define DEBUG

using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Routeguide;
using TMPro;
using UnityEngine;


public class RouteGuideUnityClient
{
    private readonly RouteGuide.RouteGuideClient _client;
    private readonly Channel _channel;
    private readonly string _server;
    private readonly RouteGuideUIHandler _myRouteGuideUiHandler;
    private string textBuffer;
    private bool isBusy;

    internal RouteGuideUnityClient(string host, string port, RouteGuideUIHandler inRouteGuideUIHandler)
    {
        _server = host + ":" + port;
        _channel = new Channel(_server, ChannelCredentials.Insecure);
        _client = new RouteGuide.RouteGuideClient(_channel);
        _myRouteGuideUiHandler = inRouteGuideUIHandler;
    }


    /// <summary>
    /// This method handles the task of calling the remote gRPC Service GetFeature, passing a Message Type of
    /// Point, and receiving back a single Message type of Feature (which contains a string name and its corresponding
    /// Point
    /// </summary>
    /// <param name="pointOfInterest">A single Routeguide Point (which contains a Lat/Long value)</param>
    /// <returns></returns>
    public async Task GetFeature(Routeguide.Point pointOfInterest)
    {
        Debug.Log("GetFeature Client latitude: " + pointOfInterest.Latitude +
                  ",  longitude: " + pointOfInterest.Longitude);

        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var returnVal = await _client.GetFeatureAsync(pointOfInterest, cancellationToken: cts.Token);
            _myRouteGuideUiHandler.AddTextToUi(returnVal.Name, false);
        }
        catch (RpcException e)
        {
            _myRouteGuideUiHandler.AddTextToUi("GetFeature Service is unavailable. " + e.Message, false);
        }

#if DEBUG
        Debug.Log("GetFeature Finished");
#endif
    }

    /// <summary>
    /// This method handles the task of calling the remote gRPC Service ListFeatures by passing a Message Type of
    /// Rectangle which contains (2) Points. The result is a gRPC response STREAM of Feature Message Types.
    /// </summary>
    /// <param name="areaOfInterest">A Routeguide Rectangle containing two Points</param>
    /// <returns></returns>
    public async Task ListFeatures(Routeguide.Rectangle areaOfInterest)
    {
        Debug.Log("ListFeatures Client Lo latitude: " + areaOfInterest.Lo.Latitude +
                  ",  Lo longitude: " + areaOfInterest.Lo.Longitude + "\n" +
                  ",  Hi latitude: " + areaOfInterest.Hi.Latitude +
                  ",  Hi longitude: " + areaOfInterest.Hi.Longitude);

        StringBuilder responseText = new StringBuilder();
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            //Sending and Receiving will be sequential - send first, then receive stream response second
            var response = _client.ListFeatures(areaOfInterest, cancellationToken: cts.Token);
            while (await response.ResponseStream.MoveNext())
            {
                var thisItemName = response.ResponseStream.Current.Name;
                if (!String.IsNullOrEmpty(thisItemName))
                {
                    _myRouteGuideUiHandler.AddTextToUi(thisItemName, false);
                }
            }
        }
        catch (RpcException e)
        {
            _myRouteGuideUiHandler.AddTextToUi("ListFeatures Service is unavailable. " + e.Message, false);
        }

#if DEBUG
        Debug.Log("async Task ListFeatures Finished");
#endif
    }


    /// <summary>
    /// This method handles the task of calling the remote gRPC Service RecordRoute by passing a STREAM of 
    /// Point Message Types. Upon completion of the asynchronous stream, the remote server calculates the distance
    /// between all the points and returns a single RouteSummary Message Type back. 
    /// </summary>
    /// <param name="pointsOfInterest">An array of Routeguide Points</param>
    /// <returns></returns>
    public async Task RecordRoute(Routeguide.Point[] pointsOfInterest)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            //Sending and Receiving will be sequential - send/stream all first, then receive
            var thisStream = _client.RecordRoute(cancellationToken: cts.Token);
            foreach (var t in pointsOfInterest)
            {
                await thisStream.RequestStream.WriteAsync(t);
            }

            await thisStream.RequestStream.CompleteAsync();

            RouteSummary summary = await thisStream.ResponseAsync;
            var myResultSummary = summary.ToString();
            if (!String.IsNullOrEmpty(myResultSummary))
            {
                _myRouteGuideUiHandler.AddTextToUi(myResultSummary, false);
            }
            else
            {
#if DEBUG
                Debug.Log("async Task RecordRoute empty");
#endif
            }
        }
        catch (RpcException e)
        {
            _myRouteGuideUiHandler.AddTextToUi("RecordRoute Service is unavailable. " + e.Message, false);
        }

#if DEBUG
        Debug.Log("async Task RecordRoute Finished");
#endif
    }

    /// <summary>
    /// This method handles the task of calling the remote gRPC BI-Directional Service RouteChat by passing a STREAM of 
    /// RouteNote Message Types. A response STREAM returns a series of accumulated RouteNote Message Types.  
    /// </summary>
    /// <param name="notesOfInterest"></param>
    /// <returns></returns>
    public async Task RouteChat(Routeguide.RouteNote[] notesOfInterest, bool streamLoadUI)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var thisStream = _client.RouteChat(cancellationToken: cts.Token);
            //Using a Task.Run(async ()...here as we essentially want (2) things to run in parallel, and
            //only return when both are complete.
            var responseReaderTask = Task.Run(async () =>
            {
                while (await thisStream.ResponseStream.MoveNext())
                {
                    //This AddText.. method is different, its capable of getting the UI updated from a different thread.
                    _myRouteGuideUiHandler.AddTextToUi(thisStream.ResponseStream.Current.Message, true);
                }

#if DEBUG
                Debug.Log("RouteChat RECEIVE messages complete");
#endif
            });

            foreach (RouteNote request in notesOfInterest)
            {
                await thisStream.RequestStream.WriteAsync(request);
            }
#if DEBUG
            Debug.Log("RouteChat SEND messages complete");
#endif

            await thisStream.RequestStream.CompleteAsync();
            await responseReaderTask;
        }
        catch (RpcException e)
        {
            _myRouteGuideUiHandler.AddTextToUi("RouteChat Service is unavailable. " + e.Message, false);
        }

#if DEBUG
        Debug.Log("async Task RouteChat Finished");
#endif
    }


    private void OnDisable()
    {
        _channel.ShutdownAsync().Wait();
    }
}