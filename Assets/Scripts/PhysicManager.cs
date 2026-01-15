using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PhysicManager : MonoBehaviour
{
    #region Variables

    public TMP_InputField UIInput;

    private static PhysicManager s_Instance = null;
    private Tree m_DynamicTree = new Tree();
    private List<MA_RigidBody> m_RigidBodies = new List<MA_RigidBody>();
    private List<(MA_PhysicShape, MA_PhysicShape)> m_ColliderPairsList = new List<(MA_PhysicShape, MA_PhysicShape)>();
    private List<CollisionPoints> m_CollisionPoints = new List<CollisionPoints>();

    public List<GameObject> Marbles = new List<GameObject>();
    public int marbleCount;
    private float camSpeed = 0.5f;
    private float camCd =7f;
    private float m_camTimer;

    private bool spawnMarble = true;

    public static PhysicManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                Debug.LogWarning("Physic Manager is null");
                return null;
            }

            return s_Instance;
        }
    }
    #endregion

    #region Unity's Methods

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            s_Instance = this;
        }

        DontDestroyOnLoad(gameObject);

        MA_PhysicShape.MA_ColliderAwake += OnColliderAwake;
        MA_PhysicShape.MA_ColliderDestroy += OnColliderDestroy;

        MA_RigidBody.MA_RigidBodyAwake += OnRigidBodyAwake;
        MA_RigidBody.MA_RigidBodyDestroy += OnRigidBodyDestroy;

        m_ColliderPairsList.Clear();
        m_RigidBodies.Clear();
        m_DynamicTree = new Tree();
    }

    private void Update()
    {

        if (m_camTimer > camCd)
        {
            Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, new Vector3(0, 41, -83.5f), camSpeed * Time.deltaTime);
            Camera.main.transform.eulerAngles = Vector3.RotateTowards(Camera.main.transform.rotation.eulerAngles, new Vector3(66, 0, 0), camSpeed * Time.deltaTime, camSpeed * Time.deltaTime);

            camSpeed += 0.05f;
        }
        else
            m_camTimer += Time.deltaTime;

        if(spawnMarble)
        {
            int index = 0;
            for (int i = 0; i < marbleCount; i++)
            {
                Instantiate(Marbles[UnityEngine.Random.Range(0, Marbles.Count)],
                            new Vector3(transform.position.x + (3 * ((index % 3) + 1)), transform.position.y + (3 * (int)(index / 3)), transform.position.z),
                            transform.rotation);
                index++;
            }

            m_camTimer = 0;
            camSpeed = 0.5f;
            Camera.main.transform.position = new Vector3(0, 81, -10);
            Camera.main.transform.eulerAngles = new Vector3(0, 0, 0);

            spawnMarble = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            spawnMarble = true;
            List<GameObject> marbles = new List<GameObject>();
            GameObject.FindGameObjectsWithTag("Marble", marbles);

            foreach (GameObject marble in marbles)
            {
                Destroy(marble.gameObject);
            }
        }

        m_DynamicTree.DrawTree();
    }

    public void OnMarbleCountUIChanged()
    {
        int newCount = 0;
        if(Int32.TryParse(UIInput.text, out newCount))
        {
            marbleCount = Mathf.Clamp(newCount, 1, 250);
        }
    }

    private Stack<(AABB, AABB)> HandleBroadPhase()
    {
        return m_DynamicTree.ComputePairs();
    }

    private void HandleNarrowPhase(Stack<(AABB, AABB)> _broadPairs)
    {
        List<(MA_PhysicShape, MA_PhysicShape)> currentPairs = new List<(MA_PhysicShape, MA_PhysicShape)>();
        List<CollisionPoints> list = new List<CollisionPoints>();

        while (_broadPairs.Count != 0)
        {
            var (aIndex, bIndex) = _broadPairs.Pop();

            MA_PhysicShape shapeA = m_DynamicTree.AABBShapes[aIndex];
            MA_PhysicShape shapeB = m_DynamicTree.AABBShapes[bIndex];

            (MA_PhysicShape, MA_PhysicShape) pair = (shapeA, shapeB);
            (MA_PhysicShape, MA_PhysicShape) inversePair = (shapeB, shapeA);

            CollisionPoints col = new CollisionPoints();

            MA_RigidBody rbA = shapeA.GetComponent<MA_RigidBody>();
            MA_RigidBody rbB = shapeB.GetComponent<MA_RigidBody>();

            if (!rbA.IsActive && !rbB.IsActive)
                continue;

            MA_PhysicShape firstShape;
            MA_PhysicShape secondShape;

            MA_RigidBody firstRb;
            MA_RigidBody secondRb;

            if(rbA.IsKinematic)
            {
                firstShape = shapeB;
                firstRb = rbB;

                secondShape = shapeA;
                secondRb = rbA;
            }
            else
            {
                firstShape = shapeA;
                firstRb = rbA;

                secondShape = shapeB;
                secondRb = rbB;
            }

            if (MathFunctions.GJK(firstShape, secondShape, out col))
            {
                if (!col.hasCollision)
                    continue;

                if (firstShape == shapeA)
                    col.normal *= -1f;

                MA_RigidBody.OnMaCollisionEnter(col, rbA, rbB);

                currentPairs.Add(pair);
                list.Add(col);

                if (m_ColliderPairsList.Contains(pair) || m_ColliderPairsList.Contains(inversePair))
                {
                    shapeA.OnMACollisionStay(shapeB.gameObject);
                    shapeB.OnMACollisionStay(shapeA.gameObject);

                }
                else
                {
                    shapeA.OnMACollisionEnter(shapeB.gameObject);
                    shapeB.OnMACollisionEnter(shapeA.gameObject);
                }
            }
        }

        foreach (var (a, b) in m_ColliderPairsList)
        {
            if (!currentPairs.Contains((a, b)) && !currentPairs.Contains((b, a)))
            {
                a.OnMACollisionExit(b.gameObject);
                b.OnMACollisionExit(a.gameObject);
            }
        }

        //m_ColliderPairsList = currentPairs.ToList();
        m_CollisionPoints = list;
    }

    private void OnDrawGizmos()
    {
        foreach(var p in m_CollisionPoints)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(p.contactPoint, .2f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(p.pointA, .2f);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(p.pointB, .2f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p.contactPoint, p.contactPoint + p.normal);
        }
    }

    private void FixedUpdate()
    {
        foreach (MA_RigidBody rb in m_RigidBodies)
        {
            rb.UpdateBody(Time.fixedDeltaTime);
        }

        if (m_DynamicTree.Root == null)
            return;

        m_DynamicTree.UpdateTree();

        Stack<(AABB, AABB)> pairs = HandleBroadPhase();
        HandleNarrowPhase(pairs);
    }

    private void OnDestroy()
    {
        MA_RigidBody.MA_RigidBodyDestroy -= OnRigidBodyDestroy;
        MA_RigidBody.MA_RigidBodyAwake -= OnRigidBodyAwake;

        MA_PhysicShape.MA_ColliderDestroy -= OnColliderDestroy;
        MA_PhysicShape.MA_ColliderAwake -= OnColliderAwake;

        m_ColliderPairsList.Clear();
        m_RigidBodies.Clear();
        m_DynamicTree = null;
    }
    #endregion

    #region Custom Methods
    public void OnColliderAwake(MA_PhysicShape _physicShape)
    {
        m_DynamicTree.Add(_physicShape);
    }

    public void OnColliderDestroy(MA_PhysicShape _physicShape)
    {
        m_DynamicTree.Remove(_physicShape);
    }

    public void OnRigidBodyAwake(MA_RigidBody _rb)
    {
        m_RigidBodies.Add(_rb);
    }

    public void OnRigidBodyDestroy(MA_RigidBody _rb)
    {
        m_RigidBodies.Remove(_rb);
    }

    #endregion
}
