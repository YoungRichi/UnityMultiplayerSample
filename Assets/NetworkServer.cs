using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;

    public List<int> PlayerID = new List<int>();
    private List<Client> clients;
    private List<GameObject> players;

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        clients = new List<Client>();
        players = new List<GameObject>();

        InvokeRepeating("UpdateClients", 1, 1.0f / 60.0f);
    }

    //private void UpdateClients()
    //{
    //    foreach (NetworkConnection connection in m_Connections)
    //    {
    //        SendData(new UpdatedPlayer(clients), connection);
    //    }
    //}

    void SendToClient(string message, NetworkConnection c){
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c){
        m_Connections.Add(c);
        Debug.Log("Accepted a connection");


            //// Example to send a handshake message:
            HandshakeMsg m = new HandshakeMsg();
            m.player.id = c.InternalId.ToString();
            m.player.playerTag = 0;
            SendToClient(JsonUtility.ToJson(m), c);




    }

    void OnData(DataStreamReader stream, int i){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            Debug.Log("Handshake message received~");
            Debug.Log(recMsg);
            break;

            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            Debug.Log("Player update message received~");
            Debug.Log(recMsg);
      
            break;

            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            Debug.Log("Server update message received~");
            break;
            default:
            Debug.Log("SERVER ERROR: Unrecognized message received~");
            break;
        }
    }

    void OnDisconnect(int i){
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void Update ()
    {



        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {

                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c  != default(NetworkConnection))
        {            
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }


        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                    ////=------
                    //uint number = stream.ReadUInt();

                    //Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                    //number += 2;

                    //var writer = m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i]);
                    //writer.WriteUInt(number);
                    //m_Driver.EndSend(writer);

                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }

        //Read PlayerPosition on Server
        
    }

    ////================================================
    //private void SendData(object data, NetworkConnection c)
    //{
    //    if (c == default(NetworkConnection))
    //        return;
    //    var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
    //    string jsonString = JsonUtility.ToJson(data);
    //    NativeArray<byte> sendBytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(jsonString), Allocator.Temp);
    //    writer.WriteBytes(sendBytes);
    //    m_Driver.EndSend(writer);
    //}
    //private void SendData(object data, int connectionIndex)
    //{
    //    if (connectionIndex < 0)
    //        return;

    //    SendData(data, m_Connections[connectionIndex]);
    //}

    //private void SendData(object data, Client client)
    //{
    //    SendData(data, FindMatchingConnection(client.id));
    //}
}