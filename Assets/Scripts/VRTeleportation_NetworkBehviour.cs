using kcp2k;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Telepathy;
using UnityEngine;

public class VRTeleportation_NetworkBehviour : MonoBehaviour
{
    public static VRTeleportation_NetworkBehviour Instance;

    public Action<byte[]> OnModelReceived;
    public Action<List<ModelInfo>> OnModelListReceived;


    [SerializeField] private int _networkPortTCP;
    [SerializeField] private int _operationsPerUpdateCount = 1000;
    [SerializeField] private string _applicationServerIp;

    private Client _clientTCP;
    private int _connectionTimeOutMs = 10000;
    private float _tcpKeepAliveNextTime;
    private float _tcpKeepAliveDelaySec = 30;



    private UserInfo _userInfo;


    public bool IsConnected { get { return _clientTCP.Connected; } }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            DestroyImmediate(gameObject);

        _clientTCP = new Client(1024);
        _clientTCP.ReceiveTimeout = 1000 * 60; // 1 minute


        _clientTCP.OnConnected = () => OnConnected();
        _clientTCP.OnData = (message) => OnDataReceived(message);
        _clientTCP.OnDisconnected = () => OnDisconnected();


        _userInfo = new UserInfo();
    }

    private async void Start()
    {
        await Connect();
    }

    private void Update()
    {
        _clientTCP.Tick(_operationsPerUpdateCount);


        if (_clientTCP.Connected)
        {
            if (Time.time > _tcpKeepAliveNextTime)
            {
                SendKeepAlive();

                _tcpKeepAliveNextTime += _tcpKeepAliveDelaySec;
            }
        }
    }

    private void OnApplicationQuit()
    {
        _clientTCP.Disconnect();
    }


    public async Task<bool> Connect()
    {
        _clientTCP.Connect(_applicationServerIp, _networkPortTCP, null);

        int timer = 0;
        while (!_clientTCP.Connected)
        {
            await Task.Delay(100);
            timer += 100;

            if (timer > _connectionTimeOutMs)
                return false;
        }

        _tcpKeepAliveNextTime = Time.time;
        return true;
    }

    private void SendKeepAlive()
    {
        SendNetworkMessage(new byte[] { (byte)MessageType.KEEP_ALIVE });
    }

    public void SendNetworkMessage(byte[] data)
    {
        if (!_clientTCP.Connected)
            return;

        if (!_clientTCP.Send(new ArraySegment<byte>(data)))
        {
            Debug.LogError($"Sending TCP error. data size: {data.Length}/{_clientTCP.MaxMessageSize}");
            throw new Exception();
        }
    }

    public void Disconnect()
    {
        if (_clientTCP.Connected)
            _clientTCP.Disconnect();
    }


    private void OnConnected()
    {

        var handshake = new List<byte>();

        handshake.Add((byte)MessageType.Handshake);


        var infoBytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(_userInfo));
        handshake.AddRange(infoBytes);

        SendNetworkMessage(handshake.ToArray());

        _tcpKeepAliveNextTime = Time.time;


        SendGetModelList();
    }

    private async void OnDisconnected()
    {

    }

    private void OnDataReceived(ArraySegment<byte> bufferSegment)
    {
        var arrayData = new byte[bufferSegment.Count];
        Buffer.BlockCopy(bufferSegment.Array, 0, arrayData, 0, arrayData.Length);

        int offset = 0;
        var messageType = (MessageType)arrayData[offset];
        offset += 1;
        Debug.Log($"msg: {messageType}");
        switch (messageType)
        {
            case MessageType.KEEP_ALIVE:
                break;
            case MessageType.Handshake:
                break;
            case MessageType.ModelsList:
                HandleModelList(arrayData, offset);
                break;
            case MessageType.ModelData:
                var lenght = BitConverter.ToInt32(arrayData, offset);
                offset += 4;
                var infoAsString = System.Text.Encoding.UTF8.GetString(arrayData, offset, lenght);
                offset += lenght;
                var info = JsonUtility.FromJson<ModelInfo>(infoAsString);

                var modelBytes = new byte[arrayData.Length - offset];
                Buffer.BlockCopy(arrayData, offset, modelBytes, 0, modelBytes.Length);

                OnModelReceived?.Invoke(modelBytes);
                break;
            default:
                throw new Exception($"Unhandled message type: {messageType}");
        }
    }


    private void HandleModelList(byte[] data, int offset)
    {
        var listAsString = System.Text.Encoding.UTF8.GetString(data, offset, data.Length - 1);
        var list = JsonConvert.DeserializeObject<ModelInfoList>(listAsString);


        OnModelListReceived?.Invoke(list.Models);
    }



    [ContextMenu("GetModelList")]
    public void SendGetModelList()
    {
        var buffer = new List<byte>();
        buffer.Add((byte)MessageType.GetModelsList);

        SendNetworkMessage(buffer.ToArray());
    }

    [ContextMenu("GetModel")]
    public async void GetModel(string name)
    {
        var buffer = new List<byte>();

        buffer.Add((byte)MessageType.GetModel);

        var nameData = System.Text.Encoding.UTF8.GetBytes(name);
        buffer.AddRange(BitConverter.GetBytes(nameData.Length));
        buffer.AddRange(nameData);

        SendNetworkMessage(buffer.ToArray());
    }


}

public enum MessageType : byte
{
    KEEP_ALIVE = 250,

    Handshake = 0,

    ModelData = 2,
    GetModelsList = 3,
    ModelsList = 4,
    ServerGotModel = 5,
    GetModel = 6

}

public class UserInfo
{
    public string Name;
    public string DeviceInfo;

    public UserInfo()
    {
        Name = $"User_{UnityEngine.Random.Range(1, 9999999)}";
        DeviceInfo = $"{Environment.OSVersion} || {Environment.MachineName}";
    }
}

public class ModelInfo
{
    public string ID;
    public string Name;
    public string CreationDate;
    public string LocalPath;

    private ModelInfo() { }

    public ModelInfo(string name)
    {
        Name = name;
        CreationDate = DateTime.Now.ToUniversalTime().ToString();

        var idString = $"{Name}{CreationDate}{UnityEngine.Random.Range(0.0001f, 9999.999f)}";
        var idStringAsBytes = System.Text.Encoding.UTF8.GetBytes(idString);
        var encoder = System.Security.Cryptography.MD5.Create();
        var idBytes = encoder.ComputeHash(idStringAsBytes);

        ID = Convert.ToBase64String(idBytes);
    }
}

public class ModelInfoList
{
    public int Count;
    public List<ModelInfo> Models = new List<ModelInfo>();
}






/// <summary>
/// ////////////////////////////////////////////////////
/// </summary>
