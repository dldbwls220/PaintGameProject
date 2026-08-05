using System.Collections.Generic;
using DefineStructure;
using Fusion;
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

    readonly List<PlayerStatusUI> _myTeamSlots = new List<PlayerStatusUI>();
    readonly List<PlayerStatusUI> _enemyTeamSlots = new List<PlayerStatusUI>();

    public void OpenWnd()
    {
        gameObject.SetActive(true);
    }

    public void SpawnPlayerStatusUI()
    {
        GameObject prefab = Resources.Load("UI/PlayerStatus") as GameObject;

        for (int i = 0; i < 4; i++)
        {
            _myTeamSlots.Add(Instantiate(prefab, _myTeamBox.transform).GetComponent<PlayerStatusUI>());
            _enemyTeamSlots.Add(Instantiate(prefab, _enemyTeamBox.transform).GetComponent<PlayerStatusUI>());
        }
    }

    // 로컬 플레이어 기준 같은 팀은 _myTeamSlots(왼쪽), 다른 팀은 _enemyTeamSlots(오른쪽)에 배치한다.
    // 로컬 플레이어 본인은 _myTeamSlots의 맨 앞(왼쪽)에 고정 배치한다.
    public void AssignPlayerStatus(NetworkDictionary<PlayerRef, PlayerData> playerData, PlayerRef localPlayer)
    {
        if (!playerData.TryGet(localPlayer, out PlayerData localData)) return;
        if (_myTeamSlots.Count == 0) return;

        int myTeamSlotIndex = 0;
        int enemyTeamSlotIndex = 0;

        ApplySlot(_myTeamSlots[myTeamSlotIndex++], localData);

        foreach (var kv in playerData)
        {
            if (kv.Key == localPlayer) continue;

            PlayerData data = kv.Value;
            if (data._teamIndex == localData._teamIndex)
            {
                if (myTeamSlotIndex >= _myTeamSlots.Count) continue;
                ApplySlot(_myTeamSlots[myTeamSlotIndex++], data);
            }
            else
            {
                if (enemyTeamSlotIndex >= _enemyTeamSlots.Count) continue;
                ApplySlot(_enemyTeamSlots[enemyTeamSlotIndex++], data);
            }
        }

        // 로비 정원(8명)보다 실제로 들어온 인원이 적어 아직 아무도 배정되지 않은 남은 슬롯은 미접속 표시로 채운다
        for (; myTeamSlotIndex < _myTeamSlots.Count; myTeamSlotIndex++)
            _myTeamSlots[myTeamSlotIndex].PlayerDisconnected();

        for (; enemyTeamSlotIndex < _enemyTeamSlots.Count; enemyTeamSlotIndex++)
            _enemyTeamSlots[enemyTeamSlotIndex].PlayerDisconnected();
    }

    
    void ApplySlot(PlayerStatusUI slot, PlayerData data)
    {
        slot.InitPlayerInfo(null, data._isConnected, data._teamColor);

        if (data._isAlive)
            slot.PlayerRespawn();
        else
            slot.PlayerDead();
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
