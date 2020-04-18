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
    private NativeList<NetworkConnection> m_Connections;

    public GameObject playerObject;
    //public List<GameObject> players = new List<GameObject>();
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();


    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 9000;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 9000");
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void Update()
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
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection");
            GameObject temp = Instantiate(playerObject, transform.position, transform.rotation);
            players.Add(0, temp);
        }

        // It will be used in case any Data event was received.
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);

            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    //Spawn a player.
                    //GameObject temp = Instantiate(playerObject, transform.position, transform.rotation);
                    //players.Add(i, temp);

                    Debug.Log("Added a player.");
                }



                if (cmd == NetworkEvent.Type.Data)
                {
                    ////Try to read a uint from the stream and output what we have received:
                    //uint number = stream.ReadUInt();

                    //Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                    //number += 2;

                    //// To send anything with the NetworkDriver we need a instance of a DataStreamWriter.
                    ////  You get a DataStreamWriter when you start sending a message by calling BeginSend.
                    //var writer = m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i]);
                    //writer.WriteUInt(number);
                    //m_Driver.EndSend(writer);

                    //Read Position from Clients.
                    ReadData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                }
            }
        }
    }


    public void ReadData(DataStreamReader stream, int i)
    {
        // Get bytes from the client.
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);

        // Get string from the client. 
        string msg = Encoding.ASCII.GetString(bytes.ToArray());
        Debug.Log("Recieving: " + msg);

        // Split the string into an array.
        string[] splitData = msg.Split('|');

        // Reading the first set of data.
        switch (splitData[0])
        {
            case "MOVE":
                Move(splitData[1], splitData[2], players[i]);
                break;
        }
    }


    void Move(string x, string y, GameObject obj)
    {
        float xMov = float.Parse(x);
        float yMov = float.Parse(y);
        obj.transform.Translate(xMov, 0.0f, yMov);
    }

}