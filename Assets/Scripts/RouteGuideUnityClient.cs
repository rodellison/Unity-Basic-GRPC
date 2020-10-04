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
    private readonly TextMeshProUGUI myTextMeshProUGui;

    internal RouteGuideUnityClient(string host, string port, TextMeshProUGUI inTextMeshProUGui)
    {
        _server = host + ":" + port;
        _channel = new Channel(_server, ChannelCredentials.Insecure);
        _client = new RouteGuide.RouteGuideClient(_channel);
        myTextMeshProUGui = inTextMeshProUGui;
    }
    
    /// <summary>
    /// The AddTMPTextOnMainThread IEnumerator is a coroutine to be used (by way of another component -
    /// UnityMainThreadDispatcher), as the code to run on the main thread, which updates the Unity UI.
    /// </summary>
    /// <param name="textDataToAdd">Text data to have added to the TMP Text UI component</param>
    /// <returns></returns>
    private IEnumerator AddTMPTextOnMainThread(string textDataToAdd)
    {
        myTextMeshProUGui.SetText(textDataToAdd);
        yield return null;
    }
    /// <summary>
    /// AddTextToUI is the method that will handle calling the UnityMainThreadDispatcher, which enqueues
    /// coroutines that need to run on the main thread.
    /// This routine is needed as in most cases, the gRPC methods use TASKS, which do their asynchronous work on
    /// threads other than Unity's main thread.
    /// </summary>
    /// <param name="textData">Text data to provide to the Enqueued IEnumerator function</param>
    private void AddTextToUi(string textData)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(AddTMPTextOnMainThread(textData));
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
            AddTextToUi(returnVal.Name);
        }
        catch (RpcException e)
        {
            AddTextToUi("GetFeature Service is unavailable. " + e.Message);
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
    public async Task ListFeatures(Routeguide.Rectangle areaOfInterest, bool streamLoadUI)
    {
        Debug.Log("ListFeatures Client Lo latitude: " + areaOfInterest.Lo.Latitude +
                  ",  Lo longitude: " + areaOfInterest.Lo.Longitude + "\n" +
                  ",  Hi latitude: " + areaOfInterest.Hi.Latitude +
                  ",  Hi longitude: " + areaOfInterest.Hi.Longitude);

        StringBuilder responseText = new StringBuilder();
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = _client.ListFeatures(areaOfInterest, cancellationToken: cts.Token);
            while (await response.ResponseStream.MoveNext())
            {
                var thisItemName = response.ResponseStream.Current.Name;
                if (!String.IsNullOrEmpty(thisItemName))
                {
                    responseText.AppendLine(thisItemName);
                    //Updating the UI here as each new Message from the stream comes in is visually nice/responsive,
                    //but it does have a pretty dramatic impact on Unity app performance, resulting in reduced framerate while
                    //updates are streaming in. 
                    //The streamLoadUI bool option parm passed in controls whether this happens. 
                    if (streamLoadUI)
                        AddTextToUi(responseText.ToString());
                }
            }
            //Updating all the 'accumulated' Messages from the response stream is less responsive, but 
            //does allow the Unity app to maintain high framerate
            if (!streamLoadUI)
                AddTextToUi(responseText.ToString());
        }
        catch (RpcException e)
        {
            AddTextToUi("ListFeatures Service is unavailable. " + e.Message);
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
                AddTextToUi(myResultSummary);
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
            AddTextToUi("RecordRoute Service is unavailable. " + e.Message);
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
            var responseReaderTask = Task.Run(async () =>
            {
                StringBuilder responseText = new StringBuilder();
                while (await thisStream.ResponseStream.MoveNext())
                {
                    responseText.Append(thisStream.ResponseStream.Current.Message + "\n");
                    //Updating the UI here as each new Message from the stream comes in is visually nice/responsive,
                    //but it does have a pretty dramatic impact on Unity app performance, resulting in reduced framerate while
                    //updates are streaming in. 
                    //The streamLoadUI bool option parm passed in controls whether this happens. 
                    if (streamLoadUI)
                        AddTextToUi(responseText.ToString());
                }
                //Updating all the 'accumulated' Messages from the response stream is less responsive, but 
                //does allow the Unity app to maintain high framerate
                if (!streamLoadUI)
                    AddTextToUi(responseText.ToString());
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
            AddTextToUi("RouteChat Service is unavailable. " + e.Message);
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