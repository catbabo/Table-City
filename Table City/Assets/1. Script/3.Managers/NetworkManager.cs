using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using PN = Photon.Pun.PhotonNetwork;

[RequireComponent(typeof(PhotonView))]
public class NetworkManager : PunManagerBase
{
	private string _gameVersion = "1";
	
	public string _roomCode;
    public string _nickName;
    public string _otherNickName;

    private bool _jobA;
    private bool _isSideA;

	public bool _forceOut { get; private set; } = false;
	private PhotonView _mainPv;

	private RoomPanel _room;

	private bool _masterReady, _clientReady;
	private int _readyPlayerCount;
	private bool _isMaster;
	private bool _masterJobSelect, _clientJobSelect;

    public override void Init()
	{
        _mainPv = GetComponent<PhotonView>();
        // 게임 버전 설정 ( 버전이 같은 사람끼리만 매칭이 가능함 )
        PN.GameVersion = _gameVersion;
        // 초당 패키지를 전송하는 횟수
        PN.SendRate = 60;
        // 초당 OnPhotonSerialize를 실행하는 횟수
        PN.SerializationRate = 30;
        // PhotonNetwork.LoadLevel을 사용하였을 때 모든 참가자를 동일한 레벨로 이동하게 하는지의 여부
        PN.AutomaticallySyncScene = true;

        InitEvent();
		Connect();
    }

    private void InitEvent()
    {
        Managers.Event.AddJobButton(JobButton);
        Managers.Event.AddReadyButton(ReadyButton);
        Managers.Event.AddAllReady(AllReady);
        Managers.Event.AddLeaveButton(LeaveButton);
    }

    private void Connect() { PN.ConnectUsingSettings(); }

    public PhotonView GetPhotonView() { return _mainPv; }

	public void SetNickName(string _name)
    {
        _nickName = _name;
        //Managers.localPlayer.SetNickName(_name);
    }

	public void SetRoomCode(string _code) { _roomCode = _code; }

	public override void OnConnectedToMaster()
	{
		PN.LocalPlayer.NickName = _nickName;
		JoinLobby();
	}

	public void JoinLobby() { PN.JoinLobby(); }

	public override void OnJoinedLobby() { }

	public void JoinOrCreate()
	{
        PN.JoinOrCreateRoom(_roomCode, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
	}

	public override void OnCreatedRoom()
    {
        OnRoom();
        _isSideA = true;
        _room.OnCreateRoom();
    }

	public override void OnJoinedRoom() { OnRoom(); }

    private void OnRoom()
    {
        _isMaster = PN.IsMasterClient;
        _readyPlayerCount = 0;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
		_mainPv.RPC("SyncRoomData", RpcTarget.Others, _masterReady, _isSideA);
		_room.OnPlayerEnteredRoom();
    }

	[PunRPC]
	private void SyncRoomData(bool masterReady, bool isSideA)
	{
        Debug.Log("sync");
		_masterReady = masterReady;
        _isSideA = !isSideA;

        if (_masterReady)
		    _readyPlayerCount = 1;

        _room.OnDataSync();
	}

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {

		if (_isMaster)
        {
			if(_clientReady)
                _readyPlayerCount--;

            _clientReady = false;
        }
		else
        {
            if (_masterReady)
                _readyPlayerCount--;

            _masterReady = _clientReady;
			_clientReady = false;
			_isMaster = IsMaster();
        }
		_room.OnPlayerLeftRoom();
    }

    public override void OnLeftRoom()
	{
		_masterReady = false;
		_clientReady = false;
		_readyPlayerCount = 0;
        _isMaster = PN.IsMasterClient;
    }

	public void DisConnect()
	{
		if (IsConnected() )
		{ PN.Disconnect(); }
    }

    public bool IsConnected() { return PN.IsConnected; }

    public bool IsInRoom() { return PN.InRoom; }

    public override void OnDisconnected(DisconnectCause cause) { Debug.Log("서버 연결 해제\n"+cause); }

    public void OutRoom_GoMain()
    {
        PN.LeaveRoom();
        //Managers.Scene.LoadScene(Define.Panel.Lobby);
    }

    public void SyncSpawnObejct(Define.prefabType _type, string _objName, Vector3 _spawnPoint, Quaternion _spawnAngle, Define.AssetData _assetType)
    {
        if (Define.prefabType.effect == _type)
        {
            _mainPv.RPC("SpawnEffect", RpcTarget.All, _objName, _spawnPoint, _spawnAngle, _assetType);
        }
    }

    [PunRPC]
    private void SpawnEffect(string _objName, Vector3 _spawnPoint, Quaternion _spawnAngle, Define.AssetData _assetType)
    {
        GameObject _object = Managers.Instance.UsePoolingObject(Define.prefabType.effect + _objName, _spawnPoint, _spawnAngle);
        if (_objName == "truck")
        {
            _object.GetComponent<Throw>().SetTargetPosition(Managers.Asset.GetTargetPosition((int)_assetType));
        }
    }

    public void OnMatchRoom()
    {
		if(IsCanCreateRoom())
			JoinOrCreate();
    }

    private void ReadyButton(bool isReady)
    {
		if (!isReady)
        {
            if (IsMaster())
            {
                _masterReady = true;
            }
            else
            {
                _clientReady = true;
            }
			_readyPlayerCount++;
            _mainPv.RPC("ReadyOtherPlayer", RpcTarget.Others);
		}

		_mainPv.RPC("UpdateReadyPopup", RpcTarget.All);
    }

    private void LeaveButton()
    {
        //Managers.player.Destroy();
        PN.LeaveRoom();
    }

    [PunRPC]
    private void UpdateReadyPopup() { _room.UpdateReadyPopup(); }

    [PunRPC]
	private void ReadyOtherPlayer()
	{
		if(IsMaster())
		{
			_clientReady = true;
		}
		else
		{
			_masterReady = true;
		}
		_readyPlayerCount++;
    }

    public bool IsCanStartInGame()
    {
        return (_masterReady && _clientReady) || (_readyPlayerCount == PN.CurrentRoom.PlayerCount);
    }

    public bool IsSolo() { return (1 == PN.CurrentRoom.PlayerCount); }

	public void ReadyMention()
	{
		_mainPv.RPC("PleaseReady", RpcTarget.Others);
	}

    [PunRPC]
    private void PleaseReady() { _room.ReadyMention(); }

    private void AllReady(bool isSolo)
    {
        LockRoom();
        if (isSolo)
        {
            _room.InGameStart();
        }
		else
        {
            _mainPv.RPC("ShowJobButton", RpcTarget.All);
        }
    }

	private void LockRoom()
    {
        PN.CurrentRoom.IsOpen = false;
        PN.CurrentRoom.IsVisible = false;
    }

    [PunRPC]
    private void ShowJobButton()
    {
        _room.ShowJobButton();
    }

    private void JobButton(bool _A)
    {
        SetPlayerJob(_A);
		_mainPv.RPC("SetSelectSync", RpcTarget.All, IsMaster());
		_room.JobButton(_A);
    }

	public void InGame()
    {
        bool isCanInGame = (IsOnSelectJob() && IsMaster());
        if (isCanInGame)
        {
            _mainPv.RPC("InGameStart", RpcTarget.All);
        }
    }

	public void SelectJobSync(bool _A)
	{
		_mainPv.RPC("SelectJob", RpcTarget.All, _A);
	}

    [PunRPC]
    private void SelectJob(bool a)
    {
        _room.SelectJob(a);
    }

    [PunRPC]
    private void SetSelectSync(bool isMaster)
    {
		if(isMaster)
		{
			_masterJobSelect = true;
		}
		else
        {
            _clientJobSelect = true;
		}

    }

    public void SetPlayerJob(bool job) { _jobA = job; }

    public bool IsPlayerTeamA() { return _jobA; }
    
    public bool IsSideA() { return _isSideA; }


    [PunRPC]
    private void InGameStart()
    {
		_room.InGameStart();
    }

    public bool IsCanCreateRoom()
	{
		return (_nickName != "" && _roomCode != "");
    }

    public string GetJoinRoomPlayerCount()
	{
		return "Player : " + _readyPlayerCount + " / " + PN.CurrentRoom.PlayerCount;
	}

	public bool IsFullPlayers()
	{
		return (PN.CurrentRoom.PlayerCount == PN.CurrentRoom.MaxPlayers);
	}

	public bool IsMaster()
    {
        return PN.IsMasterClient;
	}

	public bool IsOnSelectJob()
    {
        return (_masterJobSelect && _clientJobSelect);
	}

    public void SetRoomPanel(RoomPanel room)
    {
        _room = room;
    }
}
