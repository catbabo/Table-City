using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
	#region Singleton
	public static RoomManager room = null;

	private void Awake()
	{
		if(room == null)
		{
			room = this;
		}
	}
	#endregion

	#region SpawnPoint + New Idea
	// New Idea
	// 1. A위치와 B위치를 골라서 방 입장 * 현재 적용중 *
	// 2. 스타트 자원을 선택하여 위치 선정
	// 3. 1번과 2번을 합친 원하는 위치를 정하고 스타트 자원을 원하는 위치에서 시작 ( 하지만 자원 채집과 공장의 위치는 고정이기 때문에 불가능 할것이라 판단 )

	/// <summary> 플레이어 소환 위치 A </summary>
	public Transform _PlayerPointA { get; private set; }
	/// <summary> 플레이어 소환 위치 B </summary>
	public Transform _PlayerPointB { get; private set; }
	
	/// <summary> 플레이어 작업대 소환 위치 A </summary>
	public Transform _WorkbenchPointA { get; private set; }
	/// <summary> 플레이어 작업대 소환 위치 B </summary>
	public Transform _WorkbenchPointB { get; private set; }
	#endregion

	/// <summary> 포톤 뷰 </summary>
	private PhotonView _pv;

	/// <summary> 박스 컨트롤러 </summary>
	private InputBoxController _inputBoxController;

	/// <summary> 트럭 이동할 위치 </summary>
	private Vector3 _targetPos;

	private void Start()
	{
		_pv = gameObject.GetComponent<PhotonView>();
		InitSapwnPoint();
	}

	/// <summary> 소환 위치 초기화 </summary>
	private void InitSapwnPoint()
	{
		Transform spawnPoint = GameObject.Find("#SpawnPoint").transform;

		_PlayerPointA = spawnPoint.Find("Spawn_Player").Find("Point_A");
		_PlayerPointB = spawnPoint.Find("Spawn_Player").Find("Point_B");

		_WorkbenchPointA = spawnPoint.Find("Spawn_Workbench").Find("Point_A");
		_WorkbenchPointB = spawnPoint.Find("Spawn_Workbench").Find("Point_B");

		NetworkManager.Net.SpawnPlayer();
	}

	// 플레이어가 방에서 나간다면 실행
	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		Debug.Log(otherPlayer.NickName + " 나감.");
		NetworkManager.Net.LeaveRoom();
		NetworkManager.Net.SetForceOut(true);
		PhotonNetwork.LoadLevel("MainLobby");
	}

	/// <summary> 오브젝트 소환 동기화 실행 </summary>
	/// <param name="_type">소환할 오브젝트의 타입</param>
	/// <param name="_objName">소환할 오브젝트의 이름</param>
	/// <param name="_spawnPoint">소환할 오브젝트의 위치</param>
	/// <param name="_spawnAngle">소환할 오브잭트의 각도</param>
	public void SyncSpawnObejct(Define.prefabType _type, string _objName, Vector3 _spawnPoint, Quaternion _spawnAngle)
	{
		if(Define.prefabType.effect == _type)
		{
			_pv.RPC("SpawnEffect", RpcTarget.All,_objName, _spawnPoint, _spawnAngle);
		}
	}

	/// <summary> 아이템 데이터 동기화 실행 </summary>
	/// <param name="_inputBoxC">이동할 데이터가 들어가 있는 박스 컨트롤러</param>
	/// <param name="_factoryType">데이터를 이동할 공장 타입</param>
	public void SyncItemData(InputBoxController _inputBoxC, Define.AssetData _factoryType)
	{
		_inputBoxController = _inputBoxC;
		_pv.RPC("SetItemData", RpcTarget.All, _factoryType);
	}

	/// <summary> 트럭이 이동할 공장 위치 동기화 실행 </summary>
	/// <param name="_pos">공장의 position</param>
	public void SyncTargetPosition(Vector3 _pos) { _pv.RPC("SetTargetPosition", RpcTarget.All, _pos); }

	/// <summary> 공장 아이템 동기화 실행 </summary>
	/// <param name="_factoryType">공장 타입</param>
	/// <param name="i">값</param>
	public void SyncFactoryItem(Define.AssetData _factoryType, Define.AssetData i) { _pv.RPC("AddFactoryItem", RpcTarget.All, _factoryType, i); }

	/// <summary> 공장 포지션 받아오기 </summary>
	/// <returns>공장의 position</returns>
	public Vector3 GetTargetPosition() { return _targetPos; }

	/// <summary> 이펙트 동기화 </summary>
	/// <param name="_objName">생성할 오브젝트 이름</param>
	/// <param name="_spawnPoint">생성할 오브젝트 위치</param>
	/// <param name="_spawnAngle">생성할 오브젝트 각도</param>
	[PunRPC]
	private void SpawnEffect(string _objName, Vector3 _spawnPoint, Quaternion _spawnAngle) { GameObject _object = Managers.instantiate.UsePoolingObject(Define.prefabType.effect + _objName, _spawnPoint, _spawnAngle); }

	/// <summary> 아이템 데이터 동기화 </summary>
	/// <param name="_factoryType">데이터를 이동할 공장 타입</param>
	[PunRPC]
	private void SetItemData(Define.AssetData _factoryType) { _inputBoxController.SendItem(_factoryType); }

	/// <summary> 트럭이 이동할 공장 위치 동기화 </summary>
	/// <param name="_pos">공장 position</param>
	[PunRPC]
	private void SetTargetPosition(Vector3 _pos) { _targetPos = _pos; }

	/// <summary> 공장 아이템 동기화 </summary>
	/// <param name="_factoryType">공장 타입</param>
	/// <param name="i">값</param>
	[PunRPC]
	private void AddFactoryItem(Define.AssetData _factoryType, Define.AssetData i)
	{
		Managers.system.InputFactoryItem(_factoryType, i, _inputBoxController.asset[(int)i]);
	}
	
}
