using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerPhysicProperty
{
    // ����
    // ����������ԵĻ���
    public float mass = 75.0f;
    // �趨һ���������
    // ������������������
    public float minMass = 30.0f;
    // �����Ļ������ı���
    public float massReduceRatio = 0.01f;
    // ����
    // ������������˶��Ŀ���
    // �Լ��������Ŀ���
    public float maxPower = 150.0f;
    public int powerRatio = 50;
    // �ٶ�
    // ��Ҫ����һ����������ģ�����ܴﵽ������ٶ�
    // ���ݹ�ʽF��=1/2*C��SV^2���м���
    // �ٸ��ݹ��ʹ�ʽP=FĦVmax
    // �ڷǱ��������
    // ��������ٶȵ�ʱ��FĦ=F��
    // ����ٶ�Vmax=3��(2*P/C��S)
    // �������п�������ϵ��CΪ0.8
    // �����ܶ�Ϊ1.3kg/m^3
    // ����ӭ�������Ϊ���Σ��ɵ�S=0.5m^2
    // ���ռ���õ����Ǳ����ٶ�ΪVmax=1.57*3��P
    // ������ͨ���ܲ������ԼΪ300w������õ����10.5m/s
    // ������֤�ü��㷽ʽ��Ϊ����ʵ�����
    public float maxSpeed;
    // Ħ��ϵ��
    // Ϊ�˼�����Ч���ı���
    // ���ڷǱ���ȡϵ��Ϊ2.5
    // ���ڱ���ȡϵ��Ϊ0.2
    public float friction = 2.5f;
    // �������ٶ�
    public float gravity = 10.0f;
    // ת���ٶ�
    public float rotateSpeed = 720.0f;

}

public class PlayerController : MonoBehaviour
{
    public enum AudioSort
    {
        GrassRun,
        GrassJump,
        GrassLand,
        GrassSlip,
        StoneRun,
        StoneJump,
        StoneLand,
        StoneSlip,
        IceRun,
        IceJump,
        IceLand,
        IceSlip
    }

    [System.Serializable]
    public class AudioItem
    {
        public AudioSort audioSort;
        public AudioClip audioClip;
    }

    public enum LocationSort
    {
        Grass,
        Stone,
        Ice
    }

    // ��Ч���
    public List<AudioItem> audioItems;
    private Dictionary<AudioSort, AudioClip> audioClips = new Dictionary<AudioSort, AudioClip>();
    private AudioClip runClip;
    private AudioClip jumpClip;
    private AudioClip slipClip;
    private AudioSource audioSource;
    private bool isLand = true;

    // ��ҵ���������
    public PlayerPhysicProperty physicProperty;

    // ���������Ч
    public GameObject deathEffect;

    // �ٶ�
    // ˮƽ�ٶ�
    private Vector2 horizontalSpeed;
    // ��ֱ�ٶ�
    private float verticalSpeed;

    // ���
    private Animator animator;
    private CapsuleCollider capsuleCollider;

    // ��¼��Զ����
    private float maxDistance = 0.0f;

    // ��¼����Ƿ�����
    private bool isDead = false;

    // ���ת��
    public static bool readyToTurn;
    public static float turnDistance;
    public static bool isTurn = false;
    private int turnCount = 0;

    private void Start()
    {
        foreach(AudioItem audioItem in audioItems)
        {
            audioClips.Add(audioItem.audioSort, audioItem.audioClip);
        }
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        animator.SetBool("Ground", true);
        capsuleCollider = GetComponent<CapsuleCollider>();
        horizontalSpeed = Vector2.zero;
        verticalSpeed = 0.0f;
        UIController.Instance.InitMass(physicProperty.minMass, physicProperty.mass);
    }

    private void Update()
    {
        // ����һ�¶����
        //if(Input.GetKeyDown(KeyCode.Mouse0))
        //{
        //    Debug.Log("�����");
        //    GameObject testObject = ObjectPoolController.Instance.GetGameObject("Cake");
        //    testObject.transform.position = transform.position + Vector3.up * 2;
        //    Debug.Log("�����������Ʒ��" + testObject.name);
        //}
        // ��ͼת����ʱ��
        // ֹͣ�����˶�
        if (isTurn == false)
        {
            // ������µķ�ʽ������UE�����Cable Component�������д��
            // ���������ÿ�θ������μ����������Լ��
            // ����������״̬
            // ���ڴ�粻�������
            // ���մ�署��������
            UpdateGroundState();
            UpdatePower();
            UpdateMaxSpeed();
            UpdateWindResistance();
            UpdateHorizontalRotateAndSpeed();
            UpdateVerticalSpeed();
            UpdatePosition();
            UpdateGravity();
            UpdateMass();
            UpdateTurnState();
        }
    }

    private void FixedUpdate()
    {
        if(turnCount > 0)
        {
            --turnCount;
            transform.RotateAround(RecycleController.turnPoint, RecycleController.turnDirection, 90.0f * Time.deltaTime);
        }
    }

    private void UpdateTurnState()
    {
        if(readyToTurn && transform.position.z > turnDistance)
        {
            turnCount = 50;
            readyToTurn = false;
            isTurn = true;
            RecycleController.isTurn = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // �ٶ�̫���������
        // Ϊ��ֹ�����������￨������
        // ��Ҫ����һ���ϴ�ķ����ٶ�������ײ��
        if(other.gameObject.tag == "BackWall")
        {
            horizontalSpeed.y = -horizontalSpeed.y;
        }
        if(other.gameObject.tag == "LeftWall")
        {
            horizontalSpeed.x = -horizontalSpeed.x;
        }
        if(other.gameObject.tag == "RightWall")
        {
            horizontalSpeed.x = -horizontalSpeed.x;
        }
    }

    private void UpdateGroundState()
    {
        // ʹ�ý�����ײ������������ײ�����ķ��������Ƿ��ڵ�����ж�
        // �÷�������Ϊ�����ڲ�ͬ�ĵ���֮���ƶ�
        // �������жϳ���
        // �������ʹ���µ��жϷ���
        // ��Ȼ���ý������������ж�
        // ��Ҫʹ��Physics.OverlapCapsule����һ��������
        // �������жϽ������Ƿ���ָ���Ĳ㼶�н���
        // Ϊ���뱾�����ײ���������ײЧ������
        // ��Ҫ���һ������ֵ
        //Vector3 offset = new Vector3(0.0f, -0.01f, 0.0f);
        Vector3 offset = Vector3.zero;
        Vector3 bottomPoint = transform.position + capsuleCollider.center - transform.up * capsuleCollider.height / 2 + transform.up * capsuleCollider.radius + offset;
        Vector3 topPoint = transform.position + capsuleCollider.center + transform.up * capsuleCollider.height / 2 - transform.up * capsuleCollider.radius;
        // Layer 09��Ground
        // ��Ҫ�����еĵ�������Ϊ�ò㼶
        LayerMask layerMask = LayerMask.GetMask("Ground");
        Collider[] colliders = Physics.OverlapCapsule(bottomPoint, topPoint, capsuleCollider.radius, layerMask);
        if(colliders.Length > 0)
        {
            animator.SetBool("Ground", true);
            verticalSpeed = 0.0f;
        }
        else
        {
            animator.SetBool("Ground", false);
        }
    }

    private void UpdatePower()
    {
        int powerRatio = physicProperty.powerRatio;
        if(Input.GetKeyDown(KeyCode.K))
        {
            // ������������
            powerRatio += 10;
        }
        if(Input.GetKeyUp(KeyCode.L))
        {
            // ������������
            powerRatio -= 10;
        }
        physicProperty.powerRatio = Mathf.Clamp(powerRatio, 10, 100);
        UIController.Instance.SetPower(physicProperty.powerRatio);
    }

    private void UpdateMaxSpeed()
    {
        // ����ʵ�ʼ���
        // ��ʹ�Ǳ����Ħ����
        // ���������������
        // Ҳ�ܹ��ﵽĦ�������ڷ�������
        physicProperty.maxSpeed = 1.57f * Mathf.Pow(physicProperty.maxPower * physicProperty.powerRatio / 100, 1 / 3.0f);
        //if (physicProperty.friction == float.MaxValue)
        //{
        //    // ������ڷǱ���
        //    // Ħ�����ܹ��뵱ǰ������������������ͬ
        //    // ��ôֱ�Ӹ��ݹ�ʽ����
        //    physicProperty.maxSpeed = 1.57f * Mathf.Pow(physicProperty.maxPower * physicProperty.powerRatio, 1 / 3.0f);
        //}
        //else
        //{
        //    // ������ڱ���
        //    // ��Ҫ����Ħ�����Ϳ����������м���
        //}
    }

    private void UpdateWindResistance()
    {
        // ����������ٶȱ仯
        // �������ٶ��й�ϵ����Ҫ���Ƚ��и���
        // ��������ˮƽ�����ϵ�ӭ���涼Ϊ0.5m^2
        // ��ֱ�����϶�Ϊ0.1m^2
        // �������ȡֵ������ٶȼ�������ͬ
        // ��ֱ�ٶȸ���
        float acceleration = 0.052f * verticalSpeed * verticalSpeed / physicProperty.mass * Time.deltaTime;
        if(verticalSpeed > 0)
        {
            verticalSpeed = Mathf.Max(verticalSpeed - acceleration, 0.0f);
        }
        else
        {
            verticalSpeed = Mathf.Min(verticalSpeed + acceleration, 0.0f);
        }
        // ˮƽ�ٶȸ���
        // ��z�᷽��
        acceleration = 0.26f * horizontalSpeed.y * horizontalSpeed.y / physicProperty.mass * Time.deltaTime;
        if(horizontalSpeed.y > 0)
        {
            horizontalSpeed.y = Mathf.Max(horizontalSpeed.y - acceleration, 0.0f);
        }
        else
        {
            horizontalSpeed.y = Mathf.Min(horizontalSpeed.y + acceleration, 0.0f);
        }
        acceleration = 0.26f * horizontalSpeed.x * horizontalSpeed.x / physicProperty.mass * Time.deltaTime;
        if (horizontalSpeed.x > 0)
        {
            horizontalSpeed.x = Mathf.Max(horizontalSpeed.x - acceleration, 0.0f);
        }
        else
        {
            horizontalSpeed.x = Mathf.Min(horizontalSpeed.x + acceleration, 0.0f);
        }
    }

    private void UpdateHorizontalRotateAndSpeed()
    {
        // ��Ծ״̬������ˮƽת����ٶ�
        if(animator.GetBool("Ground") == false)
        {
            return;
        }
        // ����״̬
        Vector3 state = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            state += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            state += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            state += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            state += Vector3.right;
        }
        if(state != Vector3.zero)
        {
            // ����ģ�ͳ���
            Vector3 targetAngle = Quaternion.LookRotation(state).eulerAngles;
            float rotateDirec = targetAngle.y - transform.rotation.eulerAngles.y;
            bool isChanged = false;
            if(rotateDirec > 180.0f)
            {
                rotateDirec = -(360.0f - rotateDirec);
                isChanged = true;
            }
            if(rotateDirec < -180.0f)
            {
                rotateDirec = 360.0f + rotateDirec;
                isChanged = true;
            }
            // float���ͻ������ͬ��ֵ��������Ϊ������
            // ���ʹ��rotateDirec��Ϊ�������Ӧ�ø�Ϊ����ĳ������
            //if(rotateDirec != 0)
            if(Mathf.Abs(rotateDirec) > 0.0001f)
            {
                Vector3 angle = Vector3.zero;
                if (isChanged)
                {
                    if (rotateDirec > 0)
                    {
                        angle.y = Mathf.Clamp(transform.rotation.eulerAngles.y + physicProperty.rotateSpeed * Time.deltaTime - 360.0f, -180.0f, targetAngle.y);
                    }
                    else if (rotateDirec < 0)
                    {
                        angle.y = Mathf.Clamp(transform.rotation.eulerAngles.y - physicProperty.rotateSpeed * Time.deltaTime + 360.0f, targetAngle.y, 540.0f);
                    }
                }
                else
                {
                    if (rotateDirec > 0)
                    {
                        angle.y = Mathf.Clamp(transform.rotation.eulerAngles.y + physicProperty.rotateSpeed * Time.deltaTime, 0.0f, targetAngle.y);
                    }
                    else if (rotateDirec < 0)
                    {
                        angle.y = Mathf.Clamp(transform.rotation.eulerAngles.y - physicProperty.rotateSpeed * Time.deltaTime, targetAngle.y, 360.0f);
                    }
                }
                transform.rotation = Quaternion.Euler(angle);
            }
            else
            {
                // �����Ѿ�ת��ָ������
                // �ܹ���ʼ����
                // ����ڱ�����
                // ��ô�ù��̿��ܻ��Ѿ�����ƽ�кʹ�ֱ�����ﳯ����ٶ�
                // Ħ�����ķ����Ƿǳ����̶���
                // ���ｫ���Ϊƽ�кʹ�ֱ����ȡ��2�ķ���
                float verticalAcceleration = physicProperty.friction * physicProperty.gravity * Mathf.Sqrt(2) / 2 * Time.deltaTime;
                // ƽ�з����ϻ���Ҫ�����˵ŵص���
                // �趨���300w������
                // �˵ŵص���Ϊ300N
                float parallelAcceleration = verticalAcceleration + physicProperty.friction * physicProperty.maxPower * physicProperty.powerRatio / 100 / physicProperty.mass * Time.deltaTime;
                float angleRad = targetAngle.y * Mathf.Deg2Rad;
                //float parallelSpeed = physicProperty.speed.z * Mathf.Cos(targetAngle.y * Mathf.Deg2Rad) - physicProperty.speed.x * Mathf.Cos((targetAngle.y + 90.0f) * Mathf.Deg2Rad);
                //float verticalSpeed = physicProperty.speed.x * Mathf.Sin((targetAngle.y + 90.0f) * Mathf.Deg2Rad) - physicProperty.speed.z * Mathf.Sin(targetAngle.y * Mathf.Deg2Rad);
                // ��һ��
                float parallelSpeed = horizontalSpeed.x * Mathf.Sin(angleRad) + horizontalSpeed.y * Mathf.Cos(angleRad);
                float verticalSpeed = horizontalSpeed.x * Mathf.Cos(angleRad) - horizontalSpeed.y * Mathf.Sin(angleRad);
                //parallelSpeed = Mathf.Clamp(parallelSpeed + acceleration, 0.0f, physicProperty.maxSpeed);
                //verticalSpeed = Mathf.Clamp(verticalSpeed - acceleration, 0.0f, physicProperty.maxSpeed);
                // �����ǰ�ٶ��Ѿ���������ٶ�
                // ��ô����Ħ����������
                if(parallelSpeed > physicProperty.maxSpeed)
                {
                    parallelSpeed = Mathf.Max(parallelSpeed - parallelAcceleration, physicProperty.maxSpeed);
                }
                else
                {
                    parallelSpeed = Mathf.Min(parallelSpeed + parallelAcceleration, physicProperty.maxSpeed);
                }
                // ��Ϊ�ٶ��Ǵ��з��ŵ�
                // ����verticalSpeed�Ĵ����������ֵ
                if (verticalSpeed < 0)
                {
                    verticalSpeed = Mathf.Min(verticalSpeed + verticalAcceleration, 0.0f);
                }
                else
                {
                    verticalSpeed = Mathf.Max(verticalSpeed - verticalAcceleration, 0.0f);
                }
                horizontalSpeed.y = parallelSpeed * Mathf.Cos(angleRad) - verticalSpeed * Mathf.Sin(angleRad);
                horizontalSpeed.x = parallelSpeed * Mathf.Sin(angleRad) + verticalSpeed * Mathf.Cos(angleRad);
                //transform.Translate(physicProperty.speed * Time.deltaTime, Space.World);
                // ���¶���
                animator.SetBool("Run", true);
                // ��������
                MassMovementReduce(physicProperty.maxPower * physicProperty.powerRatio / 100000 * Time.deltaTime);
                if(audioSource.isPlaying == false)
                {
                    audioSource.PlayOneShot(runClip);
                }
                return;
            }
        }
        // ��ʵ����£����ܲ�ģ�͵��ٶȸ�����Ҫ���ǿ��������͵���
        // �������ǵ���Ϸ�ԺͶ�Ħ��ϵ���ļ�
        // ����Ҳ���Կ���������Ӱ��
        float speed = horizontalSpeed.magnitude;
        // û�в���
        // ����ͬʱ�������������򰴼��������
        // ���ߵ�ǰ���ﻹ��ת��������
        // ֻ��Ҫ�����µ��ٶ���ɵ��ٶȱ���
        // �ٳ������ڵ��ٶȼ���
        if (horizontalSpeed != Vector2.zero)
        {
            speed = Mathf.Clamp(speed - physicProperty.friction * physicProperty.gravity * Time.deltaTime, 0.0f, physicProperty.maxSpeed);
            horizontalSpeed *= speed / horizontalSpeed.magnitude;
            if(audioSource.isPlaying == false)
            {
                audioSource.PlayOneShot(slipClip);
            }
            //transform.Translate(physicProperty.speed * Time.deltaTime, Space.World);
        }
        else
        {
            if(audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
        // ���¶���
        animator.SetBool("Run", false);
    }

    private void UpdateVerticalSpeed()
    {
        // ��Ծ����������Ĺ��ʺ������кܴ�Ĺ�ϵ
        // ������Ծ�����������Ӵ�ʱ�䣬������ʱ��Ϊ1/3��
        // �˵ı��������ǵ�ǰ���ʵ�15��
        // ���㹫ʽ��(2*Pt/m)
        if (Input.GetKeyDown(KeyCode.Space) && animator.GetBool("Ground"))
        {
            isLand = false;
            audioSource.PlayOneShot(jumpClip);
            verticalSpeed = Mathf.Sqrt((physicProperty.powerRatio * physicProperty.maxPower / 50 + physicProperty.maxPower * 10) / physicProperty.mass);
            // ��������
            MassMovementReduce((physicProperty.powerRatio * physicProperty.maxPower / 50 + physicProperty.maxPower * 10) / 2000);
        }
    }

    private void UpdatePosition()
    {
        transform.Translate(horizontalSpeed.x * Time.deltaTime, verticalSpeed * Time.deltaTime, horizontalSpeed.y * Time.deltaTime, Space.World);
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, -4.3f, 4.3f), Mathf.Max(0.0f, transform.position.y), Mathf.Max(maxDistance - 1.4f, transform.position.z));
        maxDistance = Mathf.Max(maxDistance, transform.position.z);
    }

    private void UpdateGravity()
    {
        if (animator.GetBool("Ground") == false || transform.position.y > 0)
        {
            verticalSpeed -= physicProperty.gravity * Time.deltaTime;
        }
    }

    private void UpdateMass()
    {
        // ����������Ϊ����Ϸ�ԣ��趨���Ƿǳ��Ͻ�
        // ÿ�����ĵ�ǰ������1%
        physicProperty.mass -= physicProperty.mass * physicProperty.massReduceRatio * Time.deltaTime;
        if (physicProperty.mass < physicProperty.minMass)
        {
            // ���С��һ����������趨
            // ��ô��Ϸ����
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Hunger);
            PlayerDead("ֻ�˶����Զ��������ⲻ��������");
            Debug.Log("Game Over");
        }
        UIController.Instance.SetMass(physicProperty.mass);
    }

    private void MassMovementReduce(float reduce)
    {
        // �����˶�����������
        // �趨��1kwÿ������1kg
        physicProperty.mass -= reduce;
        if (physicProperty.mass < physicProperty.minMass)
        {
            // ���С��һ����������趨
            // ��ô��Ϸ����
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Hunger);
            PlayerDead("ֻ�˶����Զ��������ⲻ��������");
            Debug.Log("Game Over");
        }
    }

    public void ChangeMass(float mass)
    {
        physicProperty.mass += mass;
    }

    public void ChangeFriction(float friction)
    {
        physicProperty.friction = friction;
    }

    public void PlayerDead(string reason = "")
    {
        if(isDead)
        {
            return;
        }
        isDead = true;
        gameObject.SetActive(false);
        UIController.Instance.SaveScore();
        GameOverPanelController.Instance.ShowPanel(reason);
        if (deathEffect != null)
        {
            Destroy(Instantiate(deathEffect, transform.position, Quaternion.identity), 3.0f);
        }
        //Destroy(gameObject);
    }

    public void ChangeLocation(LocationSort location)
    {
        if(location == LocationSort.Grass)
        {
            if(isLand == false)
            {
                isLand = true;
                audioSource.PlayOneShot(audioClips[AudioSort.GrassLand]);
            }
            runClip = audioClips[AudioSort.GrassRun];
            jumpClip = audioClips[AudioSort.GrassJump];
            slipClip = audioClips[AudioSort.GrassSlip];
        }
        else if(location == LocationSort.Ice)
        {
            if (isLand == false)
            {
                isLand = true;
                audioSource.PlayOneShot(audioClips[AudioSort.IceLand]);
            }
            runClip = audioClips[AudioSort.IceRun];
            jumpClip = audioClips[AudioSort.IceJump];
            slipClip = audioClips[AudioSort.IceSlip];
        }
        else
        {
            if (isLand == false)
            {
                isLand = true;
                audioSource.PlayOneShot(audioClips[AudioSort.StoneLand]);
            }
            runClip = audioClips[AudioSort.StoneRun];
            jumpClip = audioClips[AudioSort.StoneJump];
            slipClip = audioClips[AudioSort.StoneSlip];
        }
    }
}
