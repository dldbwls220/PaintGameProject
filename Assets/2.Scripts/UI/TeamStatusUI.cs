using UnityEngine;
using UnityEngine.UI;

public class TeamStatusUI : MonoBehaviour
{
    [Header("Danger Mark")]
    [SerializeField] GameObject _dangerMyTeam;
    [SerializeField] GameObject _dangerEnemyTeam;
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

    public void DangerSign(bool myteam, bool isDanger)
    {
        if (isDanger)
        {
            if (myteam)
            {
                _dangerMyTeam.SetActive(true);
            }
            else
            {
                _dangerEnemyTeam.SetActive(true);
            }
        }
        else
        {
            if (myteam)
            {
                _dangerMyTeam.SetActive(false);
            }
            else
            {
                _dangerEnemyTeam.SetActive(false);
            }
        }
       
    }
}
