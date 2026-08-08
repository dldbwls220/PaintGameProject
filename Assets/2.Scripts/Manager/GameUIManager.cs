using DefineStructure;
using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    static GameUIManager _uniqueinstance;
    public static GameUIManager _instance => _uniqueinstance;

    [Header("All UI References")]
    [SerializeField] EmptyInkUI _emptyinkUI;
    [SerializeField] CrosshairUI _crosshairUI;
    [SerializeField] StartUI _startUI;
    [SerializeField] TeamStatusUI _teamStatusUI;
    [SerializeField] TimerUI _timerUI;
    [SerializeField] BeatenInfo _beatenInfoUI;
    [SerializeField] InkTankUI _inkTankUI;

    [Header("Non class Obj")]
    [SerializeField] GameObject _wipeOut;
    [SerializeField] GameObject _countDown;
    [SerializeField] GameObject _readyGo;
    [SerializeField] GameObject _killLogContent;

    [Header("Kill Log")]
    [SerializeField] GameObject _killLogUIPrefab;
    [SerializeField] float _killLogLifetime = 5f;

    Queue<GameObject> _killLogQueue = new Queue<GameObject>();

    void Awake()
    {
        _uniqueinstance = this;
    }

    public void InitUI()
    {
        _teamStatusUI.SpawnPlayerStatusUI();

        _emptyinkUI.CloseWnd();
        _teamStatusUI.CloseWnd();
        _crosshairUI.CloseCrosshair();
        _teamStatusUI.CloseWnd();
        _timerUI.CloseWnd();
        _beatenInfoUI.CloseWnd();
        _readyGo.SetActive(false);
        _inkTankUI.CloseWnd();
        _wipeOut.SetActive(false);
        _countDown.SetActive(false);
    }

    public void CloseStartUI()
    {
        _startUI.CloseWnd();
    }

    public void AssignPlayerStatus(NetworkDictionary<PlayerRef, PlayerData> playerData, PlayerRef localPlayer)
    {
        _teamStatusUI.AssignPlayerStatus(playerData, localPlayer);
    }

    public void OpenAllGamePlayUI()
    {
        _teamStatusUI.OpenWnd();
        _crosshairUI.OpenCrosshair();
        _timerUI.OpenWnd();
        _teamStatusUI.DangerSign(true, false);
        _teamStatusUI.DangerSign(false, false);
    }

    // 로컬 플레이어(HasInputAuthority)가 스폰됐을 때 화면 전용(로컬) UI를 그 플레이어에 연결한다
    public void RegisterLocalPlayer(NetworkInklingMovement player)
    {
        player.SetCrosshairUI(_crosshairUI);
    }

    public void UpdateInkTank(float value)
    {
        _inkTankUI.UpdateInkTank(value);
    }

    public void OnOffInkTank(bool isOn)
    {
        if (isOn) _inkTankUI.OpenWnd();
        else _inkTankUI.CloseWnd();
    }

    public void OnOffEmptyInkUI(bool isOn)
    {
        if (isOn) _emptyinkUI.OpenWnd();
        else _emptyinkUI.CloseWnd();
    }

    public void SetInkUIPos(Vector3 worldPos)
    {
        _inkTankUI.SetInkUIPos(worldPos);
    }

    public void StartReadyUI()
    {
        _readyGo.SetActive(true);
    }

    public void SetTime(float time)
    {
        _timerUI.SetTime(time);
    }

    public void InstantiateKillLog(string victimName)
    {
        GameObject go = Instantiate(_killLogUIPrefab, _killLogContent.transform);
        KillLogUI kill = go.GetComponent<KillLogUI>();

        kill.KillLogText(victimName);

        Destroy(go, _killLogLifetime);

        if (_killLogQueue.Count < 2)
        {
            _killLogQueue.Enqueue(go);
        }
        else
        {
            GameObject destroyable = _killLogQueue.Dequeue();
            Destroy(destroyable);

            _killLogQueue.Enqueue(go);
        }
    }
}
