using UnityEngine;

[CreateAssetMenu(fileName = "KartSettings", menuName = "Karting/Kart Settings")]
public class KartSettings : ScriptableObject
{
    [Header("Physics")]
    public float mass = 80f;
    public float frictionCoefficient = 4.0f;
    public float frontLateralStiffness = 1000f;
    public float rearLateralStiffness = 1000f;
    public float rollingResistance = 0.5f;
    public float maxSteerAngle = 30f;

    [Header("Engine")]
    [Tooltip("X = rpm, Y = максимальный момент при полном газе (Н*м).")]
    public AnimationCurve engineTorqueCurve;

    [Tooltip("Момент инерции маховика J, кг*м^2.")]
    public float engineInertia = 0.2f;
    public float maxRpm = 8000f;

    [Header("Drivetrain")]
    public float gearRatio = 8f;
    public float wheelRadius = 0.3f;
}