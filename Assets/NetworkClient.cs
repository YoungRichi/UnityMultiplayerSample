using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public GameObject PlayerCube;
    private GameObject playerInstance;

    public HandshakeMsg m;
    int Tag;

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);


    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");

        //// Example to send a handshake message:
        m = new HandshakeMsg();
        m.player.id = m_Connection.InternalId.ToString();
        SendToServer(JsonUtility.ToJson(m));

        Debug.Log("Spawning Cube.");
        playerInstance = Instantiate(PlayerCube, new Vector3(0, 0, 3), Quaternion.identity);
        m.player.cubPos = playerInstance.transform.position;
        m.player.playerTag = Tag;
        SendToServer(JsonUtility.ToJson(m.player.playerTag));

        Tag++;
        //m.player.cubPos = m_Connection.InternalId.ToString();
        //Debug.Log(playerInstance.transform.position);

        //SendToServer(JsonUtility.ToJson(m.player.cubPos));


        // float.par
        //Debug.Log(m.player.id);
        //Debug.Log(m.player.cubPos);

        //ServerUpdateMsg s = new ServerUpdateMsg();
        //Debug.Log(s.players.Count);
        //SendToServer(JsonUtility.ToJson(s.players.Count));
        //SendToServer(JsonUtility.ToJson(m));
        //SendToServer(JsonUtility.ToJson(s));



    }

    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            Debug.Log("Handshake message received!!!!!!!");
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            Debug.Log("Server update message received!");
            break;
            default:
            Debug.Log("Unrecognized message received!");
            break;
        }
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
        if (playerInstance)
        {
            Destroy(playerInstance);
        }

    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    public NetworkObjects.NetworkPlayer myCube;
    void Update()
    {
        //HandshakeMsg playerInput = new HandshakeMsg();
        //playerInput.player.cubPos = playerInstance.transform.position;
        if (playerInstance)
        {
            m.player.cubPos = playerInstance.transform.position;
        }


        if (Input.GetKey("d") || Input.GetKeyDown(KeyCode.RightArrow))
        {
            // Debug.Log(playerInstance.transform.position);
            SendToServer(JsonUtility.ToJson(m.player.cubPos));
            Debug.Log("Sending to Server: " + m.player.cubPos);

        }
        if (Input.GetKey("w") || Input.GetKeyDown(KeyCode.UpArrow))
        {
            SendToServer(JsonUtility.ToJson(m.player.cubPos));
            Debug.Log("Sending to Server: " + m.player.cubPos);

        }
        if (Input.GetKey("s") || Input.GetKeyDown(KeyCode.DownArrow))
        {
            SendToServer(JsonUtility.ToJson(m.player.cubPos));
            Debug.Log("Sending to Server: " + m.player.cubPos);
        }
        if (Input.GetKey("a") || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SendToServer(JsonUtility.ToJson(m.player.cubPos));
            Debug.Log("Sending to Server: " + m.player.cubPos);
        }

        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}