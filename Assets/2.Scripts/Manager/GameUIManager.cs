using DefineStructure;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] GameObject _oneMinLeft;
    [SerializeField] GameObject _score;

    [Header("Kill Log")]
    [SerializeField] GameObject _killLogUIPrefab;
    [SerializeField] float _killLogLifetime = 5f;

    [Header("Finish Anim")]
    [SerializeField] Animation _finishAnim;
    [SerializeField] AnimationClip _fadeoutClip;

    [Header("SetColor")]
    [SerializeField] Image[] _colorImage;
    [SerializeField] TextMeshProUGUI _textColor;

    [Header("Score")]
    [SerializeField] TextMeshProUGUI _scoreText;

    Queue<GameObject> _killLogQueue = new Queue<GameObject>();

    void Awake()
    {
        _uniqueinstance = this;
    }

    public void InitUI()
    {
        _teamStatusUI.SpawnPlayerStatusUI();
        _emptyinkUI.InitEmptyInk();

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
        _oneMinLeft.SetActive(false);
        _score.SetActive(false);
    }

    public void SetColor(Color teamColor, Color enemyColor)
    {
        _inkTankUI.InitColor(teamColor);
        foreach (var item in _colorImage)
        {
            item.color = teamColor;
        }
        _textColor.color = teamColor;
        _crosshairUI.SetColor(enemyColor);
        _beatenInfoUI.SetColor(enemyColor);
        _emptyinkUI.SetWindowColor(teamColor);
    }

    public void CloseStartUI()
    {
        _startUI.CloseWnd();
    }

    public void OpenLastMinLeftWnd()
    {
        _oneMinLeft.SetActive(true);
    }

    public void OpenCountDownWnd()
    {
        _countDown.SetActive(true);
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
        _score.SetActive(true);
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

    public void SetScore(int score)
    {
        _scoreText.text = $"{score:0000}p";
    }

    public void StartFinishAnim()
    {
        _finishAnim.Play();
    }

    public void EndFinishAnim()
    {
        _finishAnim.clip = _fadeoutClip;
        _finishAnim.Play();
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

    public void OpenBeatenWnd(string killerName)
    {
        _beatenInfoUI.BeatenNameTxt(killerName);
        _beatenInfoUI.OpenWnd();
    }

    public void CloseBeatenWnd()
    {
        _beatenInfoUI.CloseWnd();
    }

    public void CloseUI()
    {
        _crosshairUI.CloseCrosshair();
    }
}
