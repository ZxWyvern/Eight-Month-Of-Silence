using UnityEngine;

public class IdleHeadBobRotation : MonoBehaviour
{
    [Header("Rotation Sway")]
    [SerializeField] private float speed = 0.8f;
    [SerializeField] private float pitchAmount = 0.5f; // nunduk-dongak (derajat)
    [SerializeField] private float rollAmount = 0.6f;  // miring kiri-kanan (derajat)
    [SerializeField] private float yawAmount = 0.2f;   // geleng dikit kiri-kanan (derajat)

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 4f;

    private Quaternion defaultRot;
    private float timer;

    private float seedPitch, seedRoll, seedYaw;

    void Start()
    {
        defaultRot = transform.localRotation;

        seedPitch = Random.Range(0f, 100f);
        seedRoll = Random.Range(0f, 100f);
        seedYaw = Random.Range(0f, 100f);
    }

    void Update()
    {
        timer += Time.deltaTime;

        float pitch = Mathf.Sin((timer + seedPitch) * speed) * pitchAmount;
        float roll = Mathf.Sin((timer + seedRoll) * speed * 0.7f) * rollAmount;
        float yaw = Mathf.Sin((timer + seedYaw) * speed * 0.5f) * yawAmount;

        Quaternion targetRot = defaultRot * Quaternion.Euler(pitch, yaw, roll);

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}