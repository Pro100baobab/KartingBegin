using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Kart Settings")]
    [SerializeField] private KartSettings _kartSettings;


    [Header("Engine & drivetrain")]
    [SerializeField] private KartEngine _engine;
    //[SerializeField] private float _gearRatio = 8f;
    [SerializeField] private float _drivetrainEfficiency = 0.9f;

    // Шасси + распределение веса
    [Header("Physics")]
    [SerializeField] private float _gravity = 9.81f;

    [Header("Wheel attachment points")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private Transform _rearLeftWheel;
    [SerializeField] private Transform _rearRightWheel;

    [Header("Weight distribution")]
    [Range(0f, 1f)]

    /* 
       Распределяем вес по осям через frontAxleShare:
        W_f​=frontShare⋅W,W_r​=(1−frontShare)⋅W 

       Нормальная сила на каждое колесо:   
        N_{FL}​=N_{FR}​=\frac{W_f}{2}​​,N_{RL}​=N_{RR}​=\frac{W_r}{2}
    */
    [SerializeField] private float _frontAxleShare = 0.5f;


    // Ручной тормоз
    [Header("Handbrake")]
    [SerializeField] private float _handbrakeForce = 2000f;
    [SerializeField] private float _handbrakeLateralMultiplier = 2f;
    [SerializeField] private float _handbrakeDriftAngle = 30f;
    private bool _isHandbrakeActive = false;

    private Rigidbody _rb;

    private float _frontLeftNormalForce;
    private float _frontRightNormalForce;
    private float _rearLeftNormalForce;
    private float _rearRightNormalForce;


    //Управление через New Input System + поворот передних колёс
    [Header("Steering")]
    //[SerializeField] private float _maxSteerAngle = 30f;

    private Quaternion _frontLeftInitialLocalRot;
    private Quaternion _frontRightInitialLocalRot;

    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference _moveActionRef;
    [SerializeField] private InputActionReference _handbrakeActionRef; // ручной тормоз

    private float _throttleInput; // -1..1
    private float _steerInput;    // -1..1




    /// <summary>
    /// Простейший двигатель (момент = const) + сопротивление качению
    /// Момент двигателя постоянен: M=const
    /// Сила на ведущем колесе: F_x​=\frac{M⋅throttle​}{r}
    /// Сопротивление качению: F_{roll}​=−k_{roll}​⋅v_{long}​
    /// Добавляем поля для двигателя
    /// </summary>

    //[Header("Engine")]
    //
    //[SerializeField] private float _engineTorque = 400f; // Н*м
    //[SerializeField] private float _wheelRadius = 0.3f;  // м
    //[SerializeField] private float _maxSpeed = 20f;      // м/с

    //[Header("Rolling resistance")]
    //[SerializeField] private float _rollingResistance = 0.5f;


    /// <summary>
    /// Шины: боковая сила + фрикционный круг
    /// Разложение скорости точки колеса:   v_{long}​=v\hat{f}​,v_{lat​}=v⋅\hat{r}
    /// Боковая сила шины (линейная модель):    F_y​=−C_α​⋅v_{lat}​
    /// Фрикционный круг (ограничение сцепления):   \sqrt{F_x^2​+F_y^2}​≤μN
    /// Добавляем поля для сил колёс
    /// </summary>

    //[Header("Tyre friction")]
    //[SerializeField] private float _frictionCoefficient = 3.0f; // μ
    //[SerializeField] private float _lateralStiffness = 1000f;     // Cα



    // Телеметрия
    [Header("Telemetry")]
    [SerializeField] private bool _showTelemetry = true;
    [SerializeField] private GUIStyle _telemetryStyle;

    // Переменные для вычисления скорости скольжения
    private float _rearSlipRatio;
    private float _lateralSlipAngle;

    private float _totalRearFx;        // Суммарная продольная сила задней оси
    private float _totalFrontFy;       // Суммарная боковая сила передней оси

    // v_lat для каждого колеса
    private float _vLatFL, _vLatFR, _vLatRL, _vLatRR;

    // Fx, Fy для каждого колеса
    private float _fxFL, _fyFL, _fxFR, _fyFR, _fxRL, _fyRL, _fxRR, _fyRR;

    // Slip ratio для каждого колеса
    private float _slipRatioFL, _slipRatioFR, _slipRatioRL, _slipRatioRR;

    // Метод для обработки шин
    private void ApplyWheelForces(Transform wheel, float normalForce, bool isDriven)
    {
        if (wheel == null || _rb == null) return;

        Vector3 wheelPos = wheel.position;

        Vector3 wheelForward = wheel.forward;
        Vector3 wheelRight = wheel.right;

        Vector3 kartForward = transform.forward;

        Vector3 v = _rb.GetPointVelocity(wheelPos);

        float vLong = Vector3.Dot(v, kartForward);
        float vLat = Vector3.Dot(v, wheelRight);

        float Fx = 0f;
        float Fy = 0f;

        // 1) продольная сила от двигателя — только задняя ось
        if (isDriven && !_isHandbrakeActive)
        {
            Vector3 bodyForward = transform.forward;
            float speedAlongForward = Vector3.Dot(_rb.linearVelocity, bodyForward);

            float engineTorque = _engine.Simulate(
                _throttleInput,
                speedAlongForward,
                Time.fixedDeltaTime
            );


            float totalWheelTorque = engineTorque * _kartSettings.gearRatio * _drivetrainEfficiency;
            float wheelTorque = totalWheelTorque * 0.5f;
            Fx += wheelTorque / _kartSettings.wheelRadius;

            /*if (!(_throttleInput > 0f && speedAlongForward > _maxSpeed))
            {
                float driveTorque = _engineTorque * _throttleInput;
                Fx += driveTorque / _wheelRadius;
            }*/
        }

        // 2) сопротивление качению
        Fx += - _kartSettings.rollingResistance * vLong;

        // 3) боковая сила
        // Боковая сила - используем разные значения для передней/задней осей
        float lateralStiffness = isDriven ? _kartSettings.rearLateralStiffness : _kartSettings.frontLateralStiffness;
        Fy += -lateralStiffness * vLat;

        // 5) Ручной тормоз - применяем тормозную силу к задним колесам
        if (_isHandbrakeActive && isDriven)
        {
            // Сила ручного тормоза
            float handbrakeFx = -Mathf.Sign(vLong) * _handbrakeForce;
            Fx += handbrakeFx;

            // Уменьшаем боковую силу для заноса
            Fy *= (1f - _handbrakeLateralMultiplier * Mathf.Clamp01(Mathf.Abs(_handbrakeForce) / 1000f));

            // Регулируем угол поворота для контролируемого заноса
            if (Mathf.Abs(_steerInput) > 0.1f)
            {
                float driftSteer = _steerInput * _handbrakeDriftAngle / _kartSettings.maxSteerAngle;
                wheelForward = Quaternion.Euler(0f, driftSteer, 0f) * kartForward;
            }
        }

        // 4) фрикционный круг
        float frictionLimit = _kartSettings.frictionCoefficient * normalForce; // μ * N
        float forceLength = Mathf.Sqrt(Fx * Fx + Fy * Fy);

        if (forceLength > frictionLimit && forceLength > 1e-6f)
        {
            float scale = frictionLimit / forceLength;
            Fx *= scale;
            Fy *= scale;
        }

        // 5) мировая сила
        Vector3 force = kartForward * Fx + wheelRight * Fy;

        _rb.AddForceAtPosition(force, wheelPos, ForceMode.Force);


        // Сохраняем данные о скольжении для телеметрии
        if (isDriven)
        {
            _rearSlipRatio = Mathf.Abs(vLong) > 0.1f ? Mathf.Abs(Fx / (normalForce * _kartSettings.frictionCoefficient)) : 0f;

            // Вычисляем угол скольжения
            Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
            _lateralSlipAngle = Mathf.Atan2(localVelocity.x, Mathf.Abs(localVelocity.z)) * Mathf.Rad2Deg;
        }

        StoreWheelTelemetryData(wheel, vLat, Fx, Fy, normalForce, isDriven);
    }

    /// <summary>
    /// Метод для применения ручного тормоза отдельно
    /// </summary>
    private void ApplyHandbrakeForces()
    {
        if (!_isHandbrakeActive) return;

        // Применяем дополнительную тормозную силу к задним колесам
        Vector3 brakeForce = -_rb.linearVelocity.normalized * _handbrakeForce * Time.fixedDeltaTime;

        if (_rearLeftWheel != null)
            _rb.AddForceAtPosition(brakeForce * 0.5f, _rearLeftWheel.position, ForceMode.Impulse);

        if (_rearRightWheel != null)
            _rb.AddForceAtPosition(brakeForce * 0.5f, _rearRightWheel.position, ForceMode.Impulse);
    }


    /// <summary>
    /// Добавляем метод для расчета изначальных данных для распределения веса.
    /// </summary>
    private void ComputeStaticWheelLoads()
    {
        float mass = _rb.mass;
        float totalWeight = mass * _gravity;

        float frontWeight = totalWeight * _frontAxleShare;
        float rearWeight = totalWeight * (1f - _frontAxleShare);

        _frontLeftNormalForce = frontWeight * 0.5f;
        _frontRightNormalForce = frontWeight * 0.5f;

        _rearLeftNormalForce = rearWeight * 0.5f;
        _rearRightNormalForce = rearWeight * 0.5f;
    }


    private void Initialize()
    {
        if (_frontLeftWheel != null)
            _frontLeftInitialLocalRot = _frontLeftWheel.localRotation;

        if (_frontRightWheel != null)
            _frontRightInitialLocalRot = _frontRightWheel.localRotation;
    }


    /// <summary>
    /// Для получения данных о нажатии с клавиатуры пропишем метод ReadInput
    /// </summary>
    private void ReadInput()
    {
        Vector2 move = _moveActionRef.action.ReadValue<Vector2>();
        _steerInput = Mathf.Clamp(move.x, -1f, 1f);
        _throttleInput = Mathf.Clamp(move.y, -1f, 1f);


        // Чтение ввода ручного тормоза
        if (_handbrakeActionRef != null && _handbrakeActionRef.action != null)
        {
            _isHandbrakeActive = _handbrakeActionRef.action.ReadValue<float>() > 0.5f;
        }
    }

    /// <summary>
    /// Для поворота колёс меняем их поворот по оси Y. Используем для этого метод эйлера
    /// </summary>
    private void RotateFrontWheels()
    {
        float steerAngle = _kartSettings.maxSteerAngle * _steerInput;
        Quaternion steerRotation = Quaternion.Euler(0f, steerAngle, 0f);

        // Корректировка угла при ручном тормозе для дрифта
        if (_isHandbrakeActive && Mathf.Abs(_rb.linearVelocity.magnitude) > 2f)
        {
            steerAngle *= 1.5f; // Увеличиваем отзывчивость руля
        }


        if (_frontLeftWheel != null)
            _frontLeftWheel.localRotation = _frontLeftInitialLocalRot * steerRotation;

        if (_frontRightWheel != null)
            _frontRightWheel.localRotation = _frontRightInitialLocalRot * steerRotation;
    }

    #region unused 
    /// <summary>
    /// Добавляем тягу только на задние колёса
    /// </summary>
    private void ApplyDriveForceToRearWheels()
    {
        Vector3 bodyForward = transform.forward;
        float speedAlongForward = Vector3.Dot(_rb.linearVelocity, bodyForward);

        // ограничение максимальной скорости вперёд
        if (_throttleInput > 0f /*&& speedAlongForward > _maxSpeed*/)
            return;

        //float driveTorque = _engineTorque * _throttleInput;
        //float driveForce = driveTorque / _wheelRadius; // F = M / r

        // делим на два задних колеса
        //Vector3 forcePerWheel = bodyForward * (driveForce * 0.5f);

        //if (_rearLeftWheel != null)
        //    _rb.AddForceAtPosition(forcePerWheel, _rearLeftWheel.position, ForceMode.Force);

        //if (_rearRightWheel != null)
        //    _rb.AddForceAtPosition(forcePerWheel, _rearRightWheel.position, ForceMode.Force);
    }
    #endregion


    /// <summary>
    /// Включим Actions в методе Enable и также пропишем отключение
    /// </summary>
    private void OnEnable()
    {
        if (_moveActionRef != null && _moveActionRef.action != null)
            _moveActionRef.action.Enable();

        if (_handbrakeActionRef != null && _handbrakeActionRef.action != null)
            _handbrakeActionRef.action.Enable();
    }

    private void OnDisable()
    {
        if (_moveActionRef != null && _moveActionRef.action != null)
            _moveActionRef.action.Disable();

        if (_handbrakeActionRef != null && _handbrakeActionRef.action != null)
            _handbrakeActionRef.action.Disable();
    }


    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        // Применяем массу из ScriptableObject
        if (_kartSettings != null)
        {
            _rb.mass = _kartSettings.mass;
        }

        ComputeStaticWheelLoads();
        Initialize();

        // Инициализация стиля телеметрии, если не задан
        if (_telemetryStyle == null)
        {
            _telemetryStyle = new GUIStyle();
            _telemetryStyle.normal.textColor = Color.green;
            _telemetryStyle.fontSize = 14;
            _telemetryStyle.fontStyle = FontStyle.Bold;
        }
    }

    void Update()
    {
        ReadInput();
        RotateFrontWheels();
    }

    //private void FixedUpdate() => ApplyDriveForceToRearWheels();
    private void FixedUpdate()
    {
        // Сбрасываем суммарные силы перед расчетом
        _totalRearFx = 0f;
        _totalFrontFy = 0f;

        ApplyWheelForces(_frontLeftWheel, _frontLeftNormalForce, isDriven: false);
        ApplyWheelForces(_frontRightWheel, _frontRightNormalForce, isDriven: false);

        ApplyWheelForces(_rearLeftWheel, _rearLeftNormalForce, isDriven: true);
        ApplyWheelForces(_rearRightWheel, _rearRightNormalForce, isDriven: true);


        ApplyHandbrakeForces();
    }



    /// <summary>
    /// Отображение телеметрии на экране
    /// </summary>
    private void OnGUI()
    {
        if (!_showTelemetry || _engine == null) return;

        float yPos = 10f;
        float lineHeight = 25f;

        // Блок 1: Основная информация (левая колонка)
        GUI.Label(new Rect(10, yPos, 400, 30), $"Скорость: {_rb.linearVelocity.magnitude:F1} м/с ({_rb.linearVelocity.magnitude * 3.6f:F1} км/ч)", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 400, 30), $"Обороты: {_engine.CurrentRpm:F0} RPM", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 400, 30), $"Момент: {_engine.CurrentTorque:F1} Н·м", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 400, 30), $"Газ: {_engine.SmoothedThrottle * 100:F0}%", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 400, 30), $"Руль: {_steerInput * 100:F0}%", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 400, 30), $"Ручной тормоз: {(_isHandbrakeActive ? "ВКЛ" : "ВЫКЛ")}", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 400, 30), $"Угол скольжения: {_lateralSlipAngle:F1}°", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(10, yPos, 400, 30), $"Лимитер: {_engine.RevLimiterFactor * 100:F0}%", _telemetryStyle);
        yPos += lineHeight;

        // Блок 2: Силы и скольжение (средняя колонка)
        float midX = Screen.width / 3;
        yPos = 10f;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"СИЛЫ И СКОЛЬЖЕНИЕ:", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"Задняя ось Fx: {_totalRearFx:F1} Н", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"Передняя ось Fy: {_totalFrontFy:F1} Н", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"Задний Slip Ratio: {_rearSlipRatio * 100:F1}%", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"v_lat знаки:", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"  FL: {(_vLatFL > 0 ? "+" : "-")}  FR: {(_vLatFR > 0 ? "+" : "-")}", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"  RL: {(_vLatRL > 0 ? "+" : "-")}  RR: {(_vLatRR > 0 ? "+" : "-")}", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"Slip Ratio колес:", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"  FL: {_slipRatioFL * 100:F0}%  FR: {_slipRatioFR * 100:F0}%", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(midX, yPos, 400, 30), $"  RL: {_slipRatioRL * 100:F0}%  RR: {_slipRatioRR * 100:F0}%", _telemetryStyle);

        // Блок 3: Детали двигателя (правая колонка)
        float rightX = 2 * Screen.width / 3;
        yPos = 10f;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"ДЕТАЛИ ДВИГАТЕЛЯ:", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"Момент двиг.: {_engine.DriveTorque:F1} Н·м", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"Потери: {_engine.FrictionTorque:F1} Н·м", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"Нагрузка: {_engine.LoadTorque:F1} Н·м", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"Итог. момент: {_engine.NetTorque:F1} Н·м", _telemetryStyle);
        yPos += lineHeight;

        // Дополнительная информация о силах на колесах (опционально)
        GUI.Label(new Rect(rightX, yPos, 300, 30), $"Fx колес RL/RR:", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"  {_fxRL:F0} / {_fxRR:F0} Н", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"Fy колес FL/FR:", _telemetryStyle);
        yPos += lineHeight;

        GUI.Label(new Rect(rightX, yPos, 300, 30), $"  {_fyFL:F0} / {_fyFR:F0} Н", _telemetryStyle);
    }


    /// <summary>
    /// Сохраняет данные колеса для телеметрии
    /// </summary>
    private void StoreWheelTelemetryData(Transform wheel, float vLat, float Fx, float Fy, float normalForce, bool isDriven)
    {
        // Определяем, какое это колесо
        bool isFrontLeft = wheel == _frontLeftWheel;
        bool isFrontRight = wheel == _frontRightWheel;
        bool isRearLeft = wheel == _rearLeftWheel;
        bool isRearRight = wheel == _rearRightWheel;

        // Сохраняем v_lat для каждого колеса
        if (isFrontLeft) _vLatFL = vLat;
        if (isFrontRight) _vLatFR = vLat;
        if (isRearLeft) _vLatRL = vLat;
        if (isRearRight) _vLatRR = vLat;

        // Сохраняем Fx, Fy для каждого колеса
        if (isFrontLeft) { _fxFL = Fx; _fyFL = Fy; }
        if (isFrontRight) { _fxFR = Fx; _fyFR = Fy; }
        if (isRearLeft) { _fxRL = Fx; _fyRL = Fy; }
        if (isRearRight) { _fxRR = Fx; _fyRR = Fy; }

        // Вычисляем и сохраняем slip ratio для каждого колеса
        float slipRatio = normalForce > 0.01f ? Mathf.Abs(Fx / (normalForce * _kartSettings.frictionCoefficient)) : 0f;
        if (isFrontLeft) _slipRatioFL = slipRatio;
        if (isFrontRight) _slipRatioFR = slipRatio;
        if (isRearLeft) _slipRatioRL = slipRatio;
        if (isRearRight) _slipRatioRR = slipRatio;

        // Суммируем силы по осям
        if (isDriven) // задние колеса для Fx
        {
            _totalRearFx += Fx;
        }
        else // передние колеса для Fy
        {
            _totalFrontFy += Fy;
        }

        // Вычисляем угол скольжения для задних колес
        if (isDriven)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
            _lateralSlipAngle = Mathf.Atan2(localVelocity.x, Mathf.Abs(localVelocity.z)) * Mathf.Rad2Deg;

            // Общий slip ratio задней оси
            _rearSlipRatio = normalForce > 0.01f ? Mathf.Abs(Fx / (normalForce * _kartSettings.frictionCoefficient)) : 0f;
        }
    }

}
