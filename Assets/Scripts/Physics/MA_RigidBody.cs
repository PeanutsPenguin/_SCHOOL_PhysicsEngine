using UnityEngine;
using UnityEngine.Timeline;

public delegate void RigidBodyEvent(MA_RigidBody _rb);

public static class PhysicSettings
{
    public const float GRAVITY_VALUE = 9.80665f;
}

public class MA_RigidBody : MonoBehaviour
{
    [SerializeField] float m_Mass;
    float m_InverseMass;

    [SerializeField] private bool m_IsKinematic = false;
    [SerializeField][Range(0f, 1f)] private float m_Bounciness = 0f;
    [SerializeField][Range(0f, 1f)] private float m_staticFriction;
    [SerializeField][Range(0f, 1f)] private float m_dynamicFriction;
    [SerializeField][Range(0f, 1f)] private float m_LinearDamping = 0.0f;
    [SerializeField][Range(0f, 1f)] private float m_AngularDamping = 0.05f;
    [SerializeField] private bool m_ApplyGravity = true;

    Matrix4x4 m_LocalInertiaTensor;

    public Matrix4x4 InverseInertiaTensorWorld {  get { return m_InverseInertiaTensorWorld; } }
    private Matrix4x4 m_InverseInertiaTensorWorld;

    public bool IsActive { get { return !m_IsSleeping && !m_IsKinematic; } }

    Vector3 m_Centroid;

    Vector3 m_LinearVelocity;
    Vector3 m_AngularVelocity;

    Vector3 m_ForceAccumulator;
    Vector3 m_TorqueAccumulator;

    private float m_MinimalAngularValue = 1.5f;
    private float m_MinimalVelocityValue = 0.050f;

    public static RigidBodyEvent MA_RigidBodyAwake;
    public static RigidBodyEvent MA_RigidBodyDestroy;

    private bool m_IsSleeping = false;

    public bool IsSleeping { get { return m_IsSleeping; } }

    float m_MaxTimeBeforeSleep = 1f;
    float m_CurrentTimeBeforeSleep = 0f;

    public bool IsKinematic {  get { return m_IsKinematic; } }

    private void UpdateGlobalCentroidPosition()
    {
        m_Centroid = transform.position;
    }

    private void UpdatePositionFromGlobalCentroid()
    {
        transform.position = m_Centroid;
    }

    public void ApplyForce(Vector3 _force)
    {
        ApplyForce(_force, transform.position);
    }

    private void ApplyCollisionForce(Vector3 _force)
    {
        m_LinearVelocity += _force;
    }

    private void ApplyCollisionAngularForce(Vector3 _force)
    {
        m_AngularVelocity += _force;
    }

    public void AwakeBody()
    {
        m_IsSleeping = false;
        m_CurrentTimeBeforeSleep = 0f;
    }

    public void ApplyForce(Vector3 _force, Vector3 _point)
    {
        AwakeBody();

        m_ForceAccumulator += _force;
        m_TorqueAccumulator += Vector3.Cross((_point - m_Centroid), _force);
    }

    public void ApplyAngularForce(Vector3 _angularForce)
    {
        AwakeBody();

        m_TorqueAccumulator += _angularForce;
    }

    public void ApplyGravity()
    {
        if(m_IsSleeping) return;

        Vector3 gravity = new Vector3(0f, -PhysicSettings.GRAVITY_VALUE * m_Mass, 0f);

        m_ForceAccumulator += gravity;
    }

    public void UpdateBody(float _time)
    {
        if (m_IsKinematic || m_IsSleeping)
            return;

        if (m_LinearVelocity.magnitude < m_MinimalVelocityValue && m_AngularVelocity.magnitude < m_MinimalAngularValue)
        {
            m_CurrentTimeBeforeSleep += _time;

            if (m_CurrentTimeBeforeSleep >= m_MaxTimeBeforeSleep)
            {
                m_IsSleeping = true;
                return;
            }
        }

        if(m_ApplyGravity)
            ApplyGravity();

        m_LinearVelocity += m_InverseMass * m_ForceAccumulator * _time;

        Vector3 angularVelocity = InverseInertiaTensorWorld * m_TorqueAccumulator * _time;
        m_AngularVelocity += angularVelocity;

        m_LinearVelocity *= Mathf.Clamp(1f - m_LinearDamping * _time, 0f, 1f);
        m_AngularVelocity *= Mathf.Clamp(1f - m_AngularDamping * _time, 0f, 1f);

        m_ForceAccumulator = Vector3.zero;
        m_TorqueAccumulator = Vector3.zero;

        m_Centroid += m_LinearVelocity * _time;

        transform.rotation = Quaternion.Euler(m_AngularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime) * transform.rotation;
        UpdatePositionFromGlobalCentroid();

        m_InverseInertiaTensorWorld = (Matrix4x4.Rotate(transform.rotation) * m_LocalInertiaTensor * Matrix4x4.Rotate(transform.rotation).transpose).inverse;
    }

    private void Awake()
    {
        MA_RigidBodyAwake?.Invoke(this);
    }

    private void Start()
    {
        m_InverseMass = 1f / m_Mass;

        MA_PhysicShape shape;
        TryGetComponent(out shape);

        Matrix4x4 inertiaTensor = Matrix4x4.identity;

        float Ix = 1f;
        float Iy = 1f;
        float Iz = 1f;

        switch (shape.getShapeType())
        {
            case eShapeType.E_CUBE:
                DebugCube cube = (DebugCube)shape;

                float sx = cube.m_width * 2f;
                float sy = cube.m_height * 2f;
                float sz = cube.m_depth * 2f;

                //Ix = (1f / 12f) * m_Mass * (sy * sy + sz * sz);
                //Iy = (1f / 12f) * m_Mass * (sx * sx + sz * sz);
                //Iz = (1f / 12f) * m_Mass * (sx * sx + sy * sy);
                break;
            case eShapeType.E_SPHERE:
                DebugSphere sphere = (DebugSphere)shape;

                Ix = (2f / 3f) * m_Mass * sphere.radius * sphere.radius;
                Iy = (2f / 3f) * m_Mass * sphere.radius * sphere.radius;
                Iz = (2f / 3f) * m_Mass * sphere.radius * sphere.radius;
                break;
            case eShapeType.E_MESH:
                PhysicShape_Mesh mesh = (PhysicShape_Mesh)shape;

                Ix = (2f / 5f) * m_Mass;
                Iy = (2f / 5f) * m_Mass;
                Iz = (2f / 5f) * m_Mass;    
                break;
        }

        inertiaTensor.m00 = Ix;
        inertiaTensor.m11 = Iy;
        inertiaTensor.m22 = Iz;

        m_LocalInertiaTensor = inertiaTensor;

        if (m_IsKinematic)
        {
            m_InverseMass = 0f;
            m_LocalInertiaTensor = Matrix4x4.identity;
        }

        UpdateGlobalCentroidPosition();
    }

    public static void OnMaCollisionEnter(CollisionPoints _collisionData, MA_RigidBody _firstRb, MA_RigidBody _secondRb)
    {
        Vector3 rA = _collisionData.contactPoint - _firstRb.m_Centroid;
        Vector3 rB = _collisionData.contactPoint - _secondRb.m_Centroid;

        Vector3 vAi = _firstRb.m_LinearVelocity + Vector3.Cross(_firstRb.m_AngularVelocity, rA);
        Vector3 vBi = Vector3.zero;

        float vRel = 0f;

        if (!_secondRb.m_IsKinematic) vBi = _secondRb.m_LinearVelocity + Vector3.Cross(_secondRb.m_AngularVelocity, rB);

        vRel = Vector3.Dot(vAi - vBi, _collisionData.normal);

        if (vRel >= 0f)
            return;

        Matrix4x4 inverseInertiaTensorA = _firstRb.InverseInertiaTensorWorld;
        Matrix4x4 inverseInertiaTensorB = _secondRb.InverseInertiaTensorWorld;

        Vector3 momentumA = inverseInertiaTensorA * Vector3.Cross(rA, _collisionData.normal);
        Vector3 momentumB = inverseInertiaTensorB * Vector3.Cross(rB, _collisionData.normal);

        float weightRotA = Vector3.Dot(Vector3.Cross(momentumA, rA), _collisionData.normal);
        float weightRotB = Vector3.Dot(Vector3.Cross(momentumB, rB), _collisionData.normal);

        float totalWeight = weightRotA;
        if (!_secondRb.m_IsKinematic) totalWeight += weightRotB;

        float totalInverseMass = _firstRb.m_InverseMass;
        if (!_secondRb.m_IsKinematic) totalInverseMass += _secondRb.m_InverseMass;

        float e = (_firstRb.m_Bounciness + _secondRb.m_Bounciness) * 0.5f;
        float J = (-(1f + e) * vRel) / (totalInverseMass + totalWeight);

        Vector3 correction = (_collisionData.depth * _collisionData.normal) / totalInverseMass;
        correction = correction.sqrMagnitude == Mathf.Infinity ? Vector3.zero : correction;

        _firstRb.m_Centroid += correction * _firstRb.m_InverseMass;

        _firstRb.ApplyCollisionForce(J * _collisionData.normal * _firstRb.m_InverseMass);
        _firstRb.ApplyCollisionAngularForce(J * momentumA);
            
        if (!_secondRb.m_IsKinematic)
        {
            _secondRb.m_Centroid -= correction * _secondRb.m_InverseMass;

            _secondRb.ApplyCollisionForce(-J * _collisionData.normal * _secondRb.m_InverseMass);
            _secondRb.ApplyCollisionAngularForce(-J * momentumB);

            if (_firstRb.m_IsSleeping) _firstRb.AwakeBody();
            if (_secondRb.m_IsSleeping) _secondRb.AwakeBody();
        }

        Vector3 vRelT = vAi;
        if (!_secondRb.IsKinematic) vRelT -= vBi;

        Vector3 vt = vRelT - (Vector3.Dot(vRelT, _collisionData.normal) * _collisionData.normal);

        Vector3 t = vt.magnitude > 1e-6f ? vt.normalized : Vector3.zero;

        Vector3 rAt = Vector3.Cross(rA, t);
        Vector3 rBt = Vector3.Cross(rB, t);

        Vector3 momentumAT = inverseInertiaTensorA * rAt;
        Vector3 momentumBT = inverseInertiaTensorB * rBt;

        float totalWeightT = Vector3.Dot(Vector3.Cross(momentumAT, rAt), t);
        if (!_secondRb.m_IsKinematic) totalWeightT += Vector3.Dot(Vector3.Cross(momentumBT, rBt), t);

        float Jfriction = -Vector3.Dot(vRelT, t) / (totalInverseMass + totalWeightT);

        float staticFriction = (_firstRb.m_staticFriction + _secondRb.m_staticFriction) * 0.5f;
        float dynamicFriction = (_firstRb.m_dynamicFriction + _secondRb.m_dynamicFriction) * 0.5f;

        if (Mathf.Abs(Jfriction) > staticFriction * Mathf.Abs(J))
            Jfriction = -dynamicFriction * Mathf.Sign(Vector3.Dot(vRelT, t)) * Mathf.Abs(J);

        _firstRb.ApplyCollisionForce(Jfriction * t * _firstRb.m_InverseMass);
        _firstRb.ApplyCollisionAngularForce(Jfriction * momentumAT);

        if (!_secondRb.m_IsKinematic)
        {
            _secondRb.ApplyCollisionForce(-Jfriction * t * _secondRb.m_InverseMass);
            _secondRb.ApplyCollisionAngularForce(-Jfriction * momentumBT);
        }
    }

    private void OnDestroy()
    {
        MA_RigidBodyDestroy?.Invoke(this);
    }
}
