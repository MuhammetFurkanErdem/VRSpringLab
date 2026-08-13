using UnityEngine;

public class BillboardToPlayer : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool lockVerticalRotation = true;

    private void Start()
    {
        if (target == null && Camera.main != null)
            target = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 direction =
            target.position - transform.position;

        if (lockVerticalRotation)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation =
            Quaternion.LookRotation(-direction, Vector3.up);
    }
}