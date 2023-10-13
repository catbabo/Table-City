using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Photon.Pun;

public class UIManager : PunManagerBase
{
    private struct UIData
    {
        public string name;
        public UnityEngine.Object ui;
        public GameObject obj;
    }

    private PhotonView _pv;

    #region Object_UI
    /// <summary> 취소 버튼 오브젝트 </summary>
    [SerializeField] private GameObject _Object_CancelButton;

    /// <summary> 포인트 선택 버튼 오브젝트 </summary>
    [SerializeField] private GameObject _Object_PointButton;


    #endregion

    #region Sprite
    /// <summary> 포인트 선택 스프라이트 </summary>
    [SerializeField] private Sprite _Sprite_Check;

    /// <summary> 포인트 선택 불가 스프라이트 </summary>
    [SerializeField] private Sprite _Sprite_X;
    #endregion

    [SerializeField]
    //private List<UnityEngine.Object> _uis = new List<UnityEngine.Object>();
    private Dictionary<Type, List<UIData>> _uis = new Dictionary<Type, List<UIData>>();
    public override void Init()
    {
        _uis.Add(typeof(TMP_Text), new List<UIData>());
    }

    public void ShowPannel(string name)
    {
        foreach (UIData ui in _uis[typeof(Pannel)])
        {
            ui.obj.SetActive(false);
        }
        GetUI<GameObject>(name).SetActive(true);
    }

    public void ShowUIOnPopup(Define.PopupState target)
    {
        switch (target)
        {

            /// <summary> 캔슬과 포인트 선택 버튼을 교체 </summary>
            /// <param name="_on">true : 포인트 선택 버튼으로 교체, false : 캔슬 버튼으로 교체</param>
            case Define.PopupState.MaxPlayer:
                //_Object_PointButton.SetActive(true);
                //_Object_CancelButton.SetActive(false);
                break;
        }
    }

    public void CloseUIOnPopup(Define.PopupState target)
    {
        switch (target)
        {

            /// <summary> 캔슬과 포인트 선택 버튼을 교체 </summary>
            /// <param name="_on">true : 포인트 선택 버튼으로 교체, false : 캔슬 버튼으로 교체</param>
            case Define.PopupState.MaxPlayer:
                //_Object_PointButton.SetActive(false);
                //_Object_CancelButton.SetActive(true);
                break;
        }
    }

    public void CloseUI(string name)
    {
        GetUI<GameObject>(name).SetActive(false);
    }

    public void AddUI<T>(string target) where T : UnityEngine.Object
    {
        if(!_uis.ContainsKey(typeof(T)))
        {
            _uis.Add(typeof(T), new List<UIData>());
        }

        if(IsContainUI<T>(target))
        {
            Debug.LogError("It's aleady In List");
            return;
        }
            
        SetUI<T>(target);
    }

    public void SetUI<T>(string target) where T : UnityEngine.Object
    {
        UIData ui;

        ui.obj = Utill.Find(target);
        if (ui.obj == null)
            Debug.LogError("No Object");

        ui.ui = ui.obj.GetComponent<T>();
        if (ui.ui)
            Debug.LogError("No Component");

        ui.name = target;

        _uis[typeof(T)].Add(ui);
    }

    private bool IsContainUI<T>(string target) where T : UnityEngine.Object
    {
        if (!HasKey<T>())
        {
            Debug.LogError("No UI");
            return false;
        }

        if (GetUIData<T>(target).obj == null)
        {
            Debug.LogError("No UI");
            return false;
        }

        return true;
    }

    private bool HasKey<T>() where T : UnityEngine.Object
    {
        if (!_uis.ContainsKey(typeof(T)))
        {
            Debug.LogError("No Key");
            return false;
        }

        return true;
    }

    private UIData GetUIData<T>(string target) where T : UnityEngine.Object
    {
        for (int i = 0; i < _uis.Count; i++)
        {
            if(_uis[typeof(T)][i].name.Equals(target))
                return _uis[typeof(T)][i];
        }

        Debug.LogError("No UI");
        return new UIData();
    }

    public T GetUI<T>(string name) where T : UnityEngine.Object
    {
        return GetUIData<T>(name).ui as T;
    }


    public GameObject GetUIObject<T>(string name) where T : UnityEngine.Object
    {
        return GetUIData<T>(name).obj;
    }

    public void UIActive<T>(string name, bool active) where T : UnityEngine.Object
    {
        GetUIObject<T>(name).SetActive(active);
    }

    public void SetText(string name, string text)
    {
        GetUI<TMP_Text>(name).text = text;
    }

    public void SetImageColor(string name, Color color)
    {
        GetUI<Image>(name).color = color;
    }


    #region Button
    /// <summary>
    /// 타이틀 화면에서 메인 화면으로 이동 및 마스터 서버 연결
    /// '타이틀 화면의 START버튼과 연결되어 있음'
    /// </summary>
    public void Button_Start()
    {
        Managers._network.Connect();
        ShowPannel(Define.UI.lobby);
    }

    /// <summary>
    /// 방 이름과 닉네임을 NetworkManager에 전달하고 방을 입장하거나 생성
    /// '메인 화면의 JOIN버튼과 연결되어 있음
    /// </summary>
    public void Button_CreateOrJoin()
    {
        if (GetUI<TMP_InputField>(Define.UI.roomCodeField).text.Length <= 0 ||
            GetUI<TMP_InputField>(Define.UI.nickNameField).text.Length <= 0)
        {
            Managers._lobby.SetPopup(Define.PopupState.Warning);
            return;
        }

        Managers._network.SetRoomCode(GetUI<TMP_InputField>(Define.UI.roomCodeField).text);
        Managers._network.SetNickName(GetUI<TMP_InputField>(Define.UI.nickNameField).text);

        Managers._network.JoinLobby();
    }

    // 방 입장시 실행
    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공!");
        Managers._lobby.SetPopup(Define.PopupState.Wait);
        _pv.RPC("JoinPlayer", RpcTarget.All);
    }

    Define.PopupState _PopupState;
    /// <summary>
    /// 팝업창 종료 다른 플레이어를 기다리는 중이라면 Discnnect 후 종료
    /// '팝업창의 Cancel버튼과 연결되어 있음
    /// </summary>
    public void Button_Cancel()
    {
        if (_PopupState == Define.PopupState.Wait) { Managers._network.LeaveRoom(); }
        CloseUI(Define.UI.match);
    }

    private bool _Selected = false;
    /// <summary>
    /// 선택한 위치를 네트워크 매니저에 저장
    /// 메인 화면의 A, B 버튼에 연결되어 있음
    /// </summary>
    /// <param name="_A">true : 포인트 A 선택, false : 포인트 B 선택</param>
    public void Button_Point(bool _A)
    {
        Managers._lobby.SetPopupInfo(Define.PopupState.MaxPlayer, "Choose your tools", "You choice : " + (_A ? "Wood" : "Stone"));
        Managers._lobby.SetPopup(Define.PopupState.MaxPlayer);

        Managers._network.SetPlayerSpawnPoint(_A);

        _Selected = true;

        _pv.RPC("SelectPoint", RpcTarget.All, _A);
    }
    #endregion
}

public class TestUIManager : ManagerBase
{
    Dictionary<string, GameObject> ui = new Dictionary<string, GameObject>();
    Stack<GameObject> order = new Stack<GameObject>();

    public override void Init()
    {
        if(ui.Count == 0) ResourceLoad(ui, "2.Prefab/2.UI");

        /*테스트 부분
        ButtonData[] test = new ButtonData[2];
        test[0].text = "Yes";
        test[0].action = () => T();
        test[1].text = "No";
        test[1].action = () => DeletePopup();

        UsePopup("선택해", test,transform.position+new Vector3(0,0,800),Quaternion.identity);
        */

    }
    private void ResourceLoad(Dictionary<string, GameObject> dictionary, string filePath)
    {

        GameObject[] gameObject = Resources.LoadAll<GameObject>(filePath);

        foreach (GameObject oneGameObject in gameObject)
        {
            dictionary.Add(oneGameObject.name, oneGameObject);
        }
    }
    public void UsePopup(String text, ButtonData[] buttonData, Vector3 pos, Quaternion rotation)
    {
        GameObject canvas = Instantiate(ui["Canvas"], pos, rotation);
        GameObject popupGameObject = Instantiate(ui["Popup"], canvas.transform);
        Popup popupScript = popupGameObject.GetComponent<Popup>();
        popupScript.text.text = text;

        foreach (ButtonData OnebuttonData in buttonData)
        {
            GameObject button = Instantiate(ui["Button"], popupScript.buttonAll);
            button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = OnebuttonData.text;
            button.GetComponent<Button>().onClick.AddListener(() => OnebuttonData.action());
        }

        order.Push(canvas);
    }
    public void DeletePopup()
    {
        GameObject set =  order.Pop();
        Destroy(set);
    }
    /*테스트 부분
    public void T()
    {
        ButtonData[] test = new ButtonData[2];
        test[0].text = "Yes";
        test[0].action = () => T();
        test[1].text = "No";
        test[1].action = () => DeletePopup();

        UsePopup("선택해", test, transform.position + new Vector3(0, 0, 800), Quaternion.identity);
    }
    */
}

public struct ButtonData
{
    public string text { get; set; }
    public Action action { get; set; }
}
