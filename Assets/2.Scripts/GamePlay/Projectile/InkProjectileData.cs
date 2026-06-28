using Fusion;
using UnityEngine;

public struct InkProjectileData : INetworkStruct
{
    public Vector3 Position;     // 발사 시작 위치
    public Vector3 Velocity;     // 발사 초기 속도
    public int FireTick;         // 발사된 틱 (포물선 시간 계산 기준)
    public NetworkBool IsFinished;
    public Vector3 ImpactPosition;
    public Vector3 ImpactNormal;
    public float PaintRadius;
}
