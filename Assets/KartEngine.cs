using UnityEngine;


public class KartEngine : MonoBehaviour
{
    [Header("Kart Settings")]
    [SerializeField] private KartSettings _kartSettings;

    [Header("RPM settings")]
    [SerializeField] private float _idleRpm = 1000f;
    [SerializeField] private float _revLimiterRpm = 7500f;

    [Header("Inertia & response")]
    [Tooltip("Скорость отклика газа (1/с).")]
    [SerializeField] private float _throttleResponse = 5f;

    [Header("Losses & load")]
    [Tooltip("Внутренние потери, Н*м/ rpm.")]
    [SerializeField] private float _engineFrictionCoeff = 0.02f;

    [Tooltip("Нагрузка от машины, Н*м / (м/с).")]
    [SerializeField] private float _loadTorqueCoeff = 5f;


    public float SmoothedThrottle { get; private set; }
    public float CurrentRpm { get; private set; }
    public float CurrentTorque { get; private set; }
    public float RevLimiterFactor { get; private set; } = 1f;

    public float DriveTorque { get; private set; }
    public float FrictionTorque { get; private set; }
    public float LoadTorque { get; private set; }
    public float NetTorque { get; private set; }
    public float ThrottleInput { get; set; }

    private float _invInertiaFactor;

    public float Simulate(float throttleInput, float forwardSpeed, float deltaTime)
    {
        float targetThrottle = Mathf.Clamp(throttleInput, -1f, 1f);
        SmoothedThrottle = Mathf.MoveTowards(SmoothedThrottle, targetThrottle, _throttleResponse * deltaTime);

        UpdateRevLimiterFactor();

        float maxTorqueAtRpm = _kartSettings.engineTorqueCurve.Evaluate(CurrentRpm);

        float effectiveThrottle = SmoothedThrottle * RevLimiterFactor;
        float driveTorque = maxTorqueAtRpm * effectiveThrottle;

        float frictionTorque = _engineFrictionCoeff * CurrentRpm;
        float loadTorque = _loadTorqueCoeff * Mathf.Abs(forwardSpeed);

        float netTorque = driveTorque - frictionTorque - loadTorque;

        float rpmDot = netTorque * _invInertiaFactor;
        CurrentRpm += rpmDot * deltaTime;

        if (CurrentRpm < _idleRpm) CurrentRpm = _idleRpm;
        if (CurrentRpm > _kartSettings.maxRpm) CurrentRpm = _kartSettings.maxRpm;

        // Сохраняем значения для телеметрии
        DriveTorque = driveTorque;
        FrictionTorque = frictionTorque;
        LoadTorque = loadTorque;
        NetTorque = netTorque;
        CurrentTorque = driveTorque;

        return CurrentTorque;
    }

    private void UpdateRevLimiterFactor()
    {
        if (CurrentRpm <= _revLimiterRpm)
        {
            RevLimiterFactor = 1f;
            return;
        }

        if (CurrentRpm >= _kartSettings.maxRpm)
        {
            RevLimiterFactor = 0f;
            return;
        }

        float t = (CurrentRpm - _revLimiterRpm) / (_kartSettings.maxRpm - _revLimiterRpm);
        RevLimiterFactor = 1f - t;
    }

    private void Start()
        {
            CurrentRpm = _idleRpm;
            _invInertiaFactor = 60f / (2f * Mathf.PI * Mathf.Max(_kartSettings.engineInertia, 0.0001f));
        }
}