using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands{
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        PLAYER_INPUT
    }

    [System.Serializable]
    public class Player
    {
        public string id;
        [System.Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color;
        public Vector3 position;
        public Quaternion rotation;

        public Player()
        {
            id = "-1";
        }
        public Player(Client c)
        {
            id = c.id;
            color = c.color;
            position = c.position;
            rotation = c.rotation;
        }
        public override string ToString()
        {
            string result = "Player : \n";
            result += "id : " + id + "\n";
            result += "R : " + color.R.ToString() + ", ";
            result += "G : " + color.G.ToString() + ", ";
            result += "B : " + color.B.ToString() + "\n";
            result += "position : " + position.ToString() + "\n";
            result += "rotation : " + rotation.ToString() + "\n";

            return result;
        }
    }

    public class Client : Player
    {
        public float interval;
        public override string ToString()
        {
            string result = base.ToString();
            result += "interval : " + interval + "\n";
            return result;
        }
    }




    [System.Serializable]
    public class NetworkHeader{
        public Commands cmd;
    }
    //-----

    [System.Serializable]
    public class UpdatedPlayer : NetworkHeader
    {
        public Player[] update;
        public UpdatedPlayer(System.Collections.Generic.List<Client> clients)
        {
            cmd = Commands.PLAYER_UPDATE;
            update = new Player[clients.Count];
            for (int i = 0; i < clients.Count; i++)
            {
                update[i] = new Player(clients[i]);
            }
        }
    }



    //--------------
    [System.Serializable]
    public class HandshakeMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg(){      // Constructor
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
    
    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public PlayerUpdateMsg(){      // Constructor
            cmd = Commands.PLAYER_UPDATE;
            player = new NetworkObjects.NetworkPlayer();
        }
    };

    public class PlayerInputMsg:NetworkHeader{
        public Input myInput;
        public PlayerInputMsg(){
            cmd = Commands.PLAYER_INPUT;
            myInput = new Input();
        }
    }
    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader{
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg(){      // Constructor
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }
} 

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject{
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject{
        public Color cubeColor;
        public Vector3 cubPos;
        public int playerTag;

        public NetworkPlayer(){
            cubeColor = new Color();
        }
    }
}
