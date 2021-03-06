# Unity Basic gRPC

This Unity 2019.4 project is a simple learning project to integrate Unity3D visuals
with backend gRPC APIs using (**Google Protocol buffers**). 

Inspired by this [medium.com article](https://medium.com/@shadabambat1/basic-client-server-communication-using-unity-grpc-f4a3c2cf819c), 
this project's purpose is to show a Unity3D Client that coincides with the [gRPC Basics tutorial](https://grpc.io/docs/languages/csharp/basics/), which defines a gRPC/Protobuf service
which covers [all of the gRPC service type calls](https://grpc.io/docs/what-is-grpc/core-concepts/):
1. Simple RPC
2. Client-side Streaming RPC
3. Server-side Streaming RPC
4. Bi-directional (Client and Server) Streaming RPC

The [gRPC Basics tutorial](https://grpc.io/docs/languages/csharp/basics/) contains information on cloning 
and setting up the core Routeguide service and is a pre-requisite for using this Unity3D project.
The article also explains how to generate the gRPC client and server interface classes and code 
from the .proto service definition.

**Important:**

1. To use gRPC in Unity, you need the plugins available in this [gRPC build archive](https://packages.grpc.io/archive/2019/11/6950e15882f28e43685e948a7e5227bfcef398cd-6d642d6c-a6fc-4897-a612-62b0a3c9026b/index.xml)
2. You'll also need to install the matching protoc compile and it's plugins as well.
![protoc unity programs and plugins](protoc-unity-setup.png)


This Unity Basic GRPC project already has the protoc generated client classes in it's Assets/Scripts directory.
They were generated from the gRPC Basic tutorial code project's **.proto** file using this command:

<pre>
<code>
     protoc 
     --csharp_out={directory to place output .cs message type file} 
     --grcp_out={directory to place output .cs grpc file} 
     --plugin=protoc-gen-grcp={directory_your_grpc_csharp_plugin_is_installed}/grpc_csharp_plugin(.exe if windows) 
     {directory containing the .proto file}/route_guide.proto
</code> 
</pre>



The Unity3D scene contains (2) UIs, one is Screenspace Overlay, the other UI for presentation of RPC data is Worldspace.
The Screenspace Overlay Canvas has buttons that invoke the (4) example functions:
- GetFeature
- ListFeatures
- RecordRoute
- RouteChat

2 additional buttons (UI Events) pass bool values to specific RPC methods to specify whether to perform updates 
to the Scrollview TMP Text control as each message is streamed back, instead of updating the
UI in one-shot. 

Note: This project makes use of the UnityMainThreadDispatcher.cs script from the github
 repository:
 https://github.com/PimDeWitte/UnityMainThreadDispatcher
  
It's use comes from the fact that Unity UI updates need to occur in the main rendering thread.
The gRPC service invocations run within Tasks, each performing their activity on a separate thread. The 
**UnityMainThreadDispatcher** assists in taking the separate thread RPC server output, and 
enqueuing it to run on the main UI thread. 


# To Run:

![protoc unity programs and plugins](UnityBasicGRPC.png)

1. In a separate terminal or console window, start the grpc basic tutorial server 
   
   ---BE AWARE that the gRPC Basic Tutorial project, by default, is setup such that the RouteGuide**Server** listens only for 
   client connections using LOCALHOST address. So.. if you setup a server on one machine in your network and run the Unity 
   client on another - your connection may not work.  To resolve, you need to adjust the RouteGuideServer's 
   C# ServerPort/(golang) net.Listen value, etc.. to use a value like "0.0.0.0:10000", instead of "localhost:10000" so that connections can be accepted from anywhere. 
2. Start the Unity3D editor and load the Unity Basic GRPC project
3. Locate the (ScriptController) gameobject in the hierarchy window and in the inspector, 
locate it's attached RouteGuideUIHandler script.
4. Adjust the IP and Port variables as necessary. 
5. Run the Unity app.  