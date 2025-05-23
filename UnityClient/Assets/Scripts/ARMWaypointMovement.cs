using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARMWaypointMovement : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 15f;
    private int currentIndex = 0;

    private Rigidbody rb;

    // 상태 플래그
    public string currentWaypointName = "Unknown";
    public string lastReachedWaypointName = "None";
    public bool hasPallet = false;
    private GameObject loadedPallet;

    // 팔레트 접촉용
    private GameObject palletInContact;
    private Color palletOriginalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (waypoints.Length > 0)
            currentWaypointName = waypoints[currentIndex].name;
    }

    void FixedUpdate()
    {
        if (!enabled || waypoints.Length == 0) return;

        // 목표 지점까지 직선으로만 이동 (회전 없이)
        Vector3 cur = transform.position;
        Vector3 tgt = new Vector3(
            waypoints[currentIndex].position.x,
            cur.y,
            waypoints[currentIndex].position.z
        );
        Vector3 dir = tgt - cur;

        rb.MovePosition(cur + dir.normalized * speed * Time.fixedDeltaTime);

        // 도착 판정
        if (dir.magnitude < 0.1f)
        {
            lastReachedWaypointName = currentWaypointName;
            currentIndex = (currentIndex + 1) % waypoints.Length;
            currentWaypointName = waypoints[currentIndex].name;

            // 0번 웨이포인트 도착 시 하역
            if (currentIndex == 0 && hasPallet)
                UnloadPallet();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (!enabled) return;

        if (col.gameObject.CompareTag("Wall"))
        {
            enabled = false;
            Debug.Log("벽 충돌 → 자동 모드 중지");
            return;
        }

        if (col.gameObject.CompareTag("Pallet") && !hasPallet)
        {
            // 접촉 시 녹색으로
            palletInContact = col.gameObject;
            var rend = palletInContact.GetComponent<Renderer>();
            if (rend != null)
            {
                palletOriginalColor = rend.material.color;
                rend.material.color = Color.green;
            }

            LoadPallet(palletInContact);
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.gameObject == palletInContact && !hasPallet)
        {
            // 접촉 해제 시 원래 색으로 복원
            var rend = palletInContact.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = palletOriginalColor;
            palletInContact = null;
        }
    }

    private void LoadPallet(GameObject pallet)
    {
        pallet.transform.SetParent(transform, true);
        loadedPallet = pallet;

        float yOffset = pallet.transform.localScale.y * 0.5f + 0.01f;
        pallet.transform.position = transform.position + Vector3.up * yOffset;
        pallet.transform.rotation = transform.rotation;

        var prb = pallet.GetComponent<Rigidbody>();
        if (prb != null)
        {
            prb.isKinematic = true;
            prb.useGravity = false;
        }

        hasPallet = true;
        Debug.Log("적재 완료");
    }

    public void ForceLoad(GameObject pallet)
    {
        if (hasPallet || pallet == null) return;

        pallet.transform.SetParent(transform, true);

        float yOffset = pallet.transform.localScale.y * 0.5f + 0.01f;
        pallet.transform.position = transform.position + Vector3.up * yOffset;
        pallet.transform.rotation = transform.rotation;

        var prb = pallet.GetComponent<Rigidbody>();
        if (prb != null)
        {
            prb.isKinematic = true;
            prb.useGravity = false;
        }

        hasPallet = true;
        loadedPallet = pallet;

        Debug.Log("수동 적재 완료");
    }

    public void ForceUnload()
    {
        if (!hasPallet || loadedPallet == null) return;

        loadedPallet.transform.SetParent(null, true);

        Vector3 drop = transform.position + transform.forward * 0.5f;
        drop.y = 0.15f;
        loadedPallet.transform.position = drop;

        var prb = loadedPallet.GetComponent<Rigidbody>();
        if (prb != null)
        {
            prb.isKinematic = false;
            prb.useGravity = true;
        }

        hasPallet = false;
        loadedPallet = null;
        Debug.Log("수동 하역 완료");
    }


    private void UnloadPallet()
    {
        if (loadedPallet == null) return;

        loadedPallet.transform.SetParent(null, true);

        Vector3 drop = transform.position + (waypoints[currentIndex].position - transform.position).normalized * 0.5f;
        drop.y = 0.15f;
        loadedPallet.transform.position = drop;

        var prb = loadedPallet.GetComponent<Rigidbody>();
        if (prb != null)
        {
            prb.isKinematic = false;
            prb.useGravity = true;
        }

        hasPallet = false;
        loadedPallet = null;

        // 자동 모드 종료 + 수동 모드 전환
        enabled = false;
        var manual = GetComponent<ARMMovement>();
        if (manual != null) manual.enabled = true;
        Debug.Log("하역 완료 → 자동 모드 종료, 수동 모드 전환");
    }

    public string GetStatusSummary()
    {
        var pos = $"({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})";
        var pallet = hasPallet ? "팔레트 있음" : "팔레트 없음";
        return $"최근 도착: {lastReachedWaypointName}, 다음 목표: {currentWaypointName}, 위치: {pos}, 속도: {speed}, {pallet}";
    }
}
