
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody2D targetRb;

    [Header("Follow")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector2 targetOffset = new Vector2(0f, 0.2f);

    [Header("Dead Zone")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(0.3f, 0.2f);

    [Header("Look Ahead")]
    [SerializeField] private float lookAheadTime = 0.12f;
    [SerializeField] private float maxLookAhead = 0.8f;
    [SerializeField] private float lookAheadSmoothTime = 0.2f;

    [Header("Vertical Look")]
    [SerializeField] private float verticalLookMultiplier = 0.03f;
    [SerializeField] private float maxVerticalLook = 0.3f;

    [Header("Room Bounds")]

    [Header("Camera Shake")]
    [SerializeField] private float shakeFrequency = 25f;

    private Camera cam;

    private Vector3 smoothVelocity;

    private float currentLookAhead;
    private float lookAheadVelocity;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;

    private float originalZ;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        originalZ = transform.position.z;

        if (target != null && targetRb == null)
            targetRb = target.GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        UpdateCamera();
    }

    private void UpdateCamera()
    {
        Vector2 targetVelocity = Vector2.zero;

        if (targetRb != null)
            targetVelocity = targetRb.velocity;

        // Горизонтальное упреждение камеры.
        float desiredLookAhead = Mathf.Clamp(
            targetVelocity.x * lookAheadTime,
            -maxLookAhead,
            maxLookAhead
        );

        currentLookAhead = Mathf.SmoothDamp(
            currentLookAhead,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );

        // Небольшое вертикальное упреждение.
        float verticalLook = Mathf.Clamp(
            targetVelocity.y * verticalLookMultiplier,
            -maxVerticalLook,
            maxVerticalLook
        );

        Vector2 targetPosition = new Vector2(
            target.position.x + targetOffset.x + currentLookAhead,
            target.position.y + targetOffset.y + verticalLook
        );

        // Dead Zone: камера не реагирует на каждое движение персонажа.
        float desiredX = GetDeadZonePosition(
            targetPosition.x,
            transform.position.x,
            deadZoneSize.x
        );

        float desiredY = GetDeadZonePosition(
            targetPosition.y,
            transform.position.y,
            deadZoneSize.y
        );

        Vector3 desiredPosition = new Vector3(
            desiredX,
            desiredY,
            originalZ
        );


        // Плавное следование.
        Vector3 nextPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref smoothVelocity,
            smoothTime
        );

        // Тряска камеры добавляется после основного движения.
        Vector3 shakeOffset = GetShakeOffset();

        transform.position = nextPosition + shakeOffset;
    }

    private float GetDeadZonePosition(
        float targetPosition,
        float cameraPosition,
        float deadZone
    )
    {
        float difference = targetPosition - cameraPosition;

        if (Mathf.Abs(difference) <= deadZone)
            return cameraPosition;

        return targetPosition - Mathf.Sign(difference) * deadZone;
    }

    // Вызывается из других скриптов.
    public void ShakeCamera(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f)
            return Vector3.zero;

        shakeTimer -= Time.deltaTime;

        float strength = shakeMagnitude *
            (shakeTimer / shakeDuration);

        float time = Time.time * shakeFrequency;

        return new Vector3(
            Mathf.PerlinNoise(time, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, time) - 0.5f,
            0f
        ) * strength;
    }
}
