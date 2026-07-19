using UnityEngine;
using UnityEngine.UI;

public class TeamStatusUI : MonoBehaviour
{
    [Header("Danger Mark")]
    [SerializeField] Image _dangerMyTeam;
    [SerializeField] Image _dangerEnemyTeam;
    [Header("Team Setting")]
    [SerializeField] GameObject _myTeamBox;
    [SerializeField] GameObject _enemyTeamBox;

    GameObject _playerStatus;


    public void OpenWnd()
    {
        gameObject.SetActive(true);
    }

    public void SetTeam()
    {
        //나중에 GameUIManager에서 현제 모든 플레이어의 정보를 받아와 플레이어 정보 아이콘을 생성

        _playerStatus = Resources.Load("UI/PlayerStaus") as GameObject;
    }

    public void CloseWnd()
    {
        gameObject.SetActive(false);
    }
}
