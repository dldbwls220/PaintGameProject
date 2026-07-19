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

    [Header("Non class Obj")]
    [SerializeField] GameObject _inkTank;
    [SerializeField] GameObject _wipeOut;
    [SerializeField] GameObject _countDown;

    void Awake()
    {
        _uniqueinstance = this;
    }

    public void InitUI()
    {
        _emptyinkUI.CloseWnd();
        _teamStatusUI.CloseWnd();
        _crosshairUI.CloseCrosshair();
        _teamStatusUI.CloseWnd();
        _timerUI.CloseWnd();
        _beatenInfoUI.CloseWnd();
        _inkTank.SetActive(false);
        _wipeOut.SetActive(false);
        _countDown.SetActive(false);
    }

    // 로컬 플레이어(HasInputAuthority)가 스폰됐을 때 화면 전용(로컬) UI를 그 플레이어에 연결한다
    public void RegisterLocalPlayer(NetworkInklingMovement player)
    {
        player.SetCrosshairUI(_crosshairUI);
    }
}
