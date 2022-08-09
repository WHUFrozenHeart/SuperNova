using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerPhysicProperty
{
    // 质量
    // 玩家物理特性的基础
    public float mass = 75.0f;
    // 设定一个最低质量
    // 低于这个质量玩家死亡
    public float minMass = 30.0f;
    // 质量的基础消耗比例
    public float massReduceRatio = 0.01f;
    // 功率
    // 决定玩家消耗运动的快慢
    // 以及质量消耗快慢
    public float maxPower = 150.0f;
    public int powerRatio = 50;
    // 速度
    // 需要假设一个阻力，来模拟人能达到的最大速度
    // 根据公式F阻=1/2*CρSV^2进行计算
    // 再根据功率公式P=F摩Vmax
    // 在非冰面情况下
    // 到达最大速度的时候F摩=F阻
    // 最大速度Vmax=3√(2*P/CρS)
    // 假设其中空气阻力系数C为0.8
    // 空气密度为1.3kg/m^3
    // 人体迎风面近似为矩形，可得S=0.5m^2
    // 最终计算得到最大非冰面速度为Vmax=1.57*3√P
    // 根据普通人跑步最大功率约为300w，计算得到最大10.5m/s
    // 可以验证该计算方式较为符合实际情况
    public float maxSpeed;
    // 摩擦系数
    // 为了简化物理效果的表现
    // 对于非冰面取系数为2.5
    // 对于冰面取系数为0.2
    public float friction = 2.5f;
    // 重力加速度
    public float gravity = 10.0f;
    // 转动速度
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

    // 音效相关
    public List<AudioItem> audioItems;
    private Dictionary<AudioSort, AudioClip> audioClips = new Dictionary<AudioSort, AudioClip>();
    private AudioClip runClip;
    private AudioClip jumpClip;
    private AudioClip slipClip;
    private AudioSource audioSource;
    private bool isLand = true;

    // 玩家的物理属性
    public PlayerPhysicProperty physicProperty;

    // 玩家死亡特效
    public GameObject deathEffect;

    // 速度
    // 水平速度
    private Vector2 horizontalSpeed;
    // 垂直速度
    private float verticalSpeed;

    // 组件
    private Animator animator;
    private CapsuleCollider capsuleCollider;

    // 记录最远距离
    private float maxDistance = 0.0f;

    // 记录玩家是否死亡
    private bool isDead = false;

    // 玩家转向
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
        // 测试一下对象池
        //if(Input.GetKeyDown(KeyCode.Mouse0))
        //{
        //    Debug.Log("鼠标点击");
        //    GameObject testObject = ObjectPoolController.Instance.GetGameObject("Cake");
        //    testObject.transform.position = transform.position + Vector3.up * 2;
        //    Debug.Log("鼠标点击生成物品：" + testObject.name);
        //}
        // 地图转动的时候
        // 停止人物运动
        if (isTurn == false)
        {
            // 物理更新的方式是依据UE里面的Cable Component组件进行写的
            // 具体策略是每次更新依次计算各个物理约束
            // 最后更新物理状态
            // 中期答辩不具体介绍
            // 最终答辩讲解代码介绍
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
        // 速度太慢的情况下
        // 为防止阻力导致人物卡在其中
        // 需要给予一个较大的反向速度脱离碰撞器
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
        // 使用胶囊碰撞体与地面进行碰撞器检测的方法进行是否在地面的判断
        // 该方法会因为人物在不同的地面之间移动
        // 而导致判断出错
        // 因此这里使用新的判断方法
        // 仍然利用胶囊体来进行判断
        // 需要使用Physics.OverlapCapsule虚拟一个胶囊体
        // 来定向判断胶囊体是否与指定的层级有交集
        // 为了与本身的碰撞体的物理碰撞效果分离
        // 需要添加一个修正值
        //Vector3 offset = new Vector3(0.0f, -0.01f, 0.0f);
        Vector3 offset = Vector3.zero;
        Vector3 bottomPoint = transform.position + capsuleCollider.center - transform.up * capsuleCollider.height / 2 + transform.up * capsuleCollider.radius + offset;
        Vector3 topPoint = transform.position + capsuleCollider.center + transform.up * capsuleCollider.height / 2 - transform.up * capsuleCollider.radius;
        // Layer 09是Ground
        // 需要将所有的地面设置为该层级
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
            // 增加能量消耗
            powerRatio += 10;
        }
        if(Input.GetKeyUp(KeyCode.L))
        {
            // 减少能量消耗
            powerRatio -= 10;
        }
        physicProperty.powerRatio = Mathf.Clamp(powerRatio, 10, 100);
        UIController.Instance.SetPower(physicProperty.powerRatio);
    }

    private void UpdateMaxSpeed()
    {
        // 经过实际计算
        // 即使是冰面的摩擦力
        // 在人物最低体重下
        // 也能够达到摩擦力等于风阻的情况
        physicProperty.maxSpeed = 1.57f * Mathf.Pow(physicProperty.maxPower * physicProperty.powerRatio / 100, 1 / 3.0f);
        //if (physicProperty.friction == float.MaxValue)
        //{
        //    // 如果是在非冰面
        //    // 摩擦力能够与当前功耗下最大空气阻力相同
        //    // 那么直接根据公式计算
        //    physicProperty.maxSpeed = 1.57f * Mathf.Pow(physicProperty.maxPower * physicProperty.powerRatio, 1 / 3.0f);
        //}
        //else
        //{
        //    // 如果是在冰面
        //    // 需要根据摩擦力和空气阻力进行计算
        //}
    }

    private void UpdateWindResistance()
    {
        // 风阻带来的速度变化
        // 风阻与速度有关系，需要最先进行更新
        // 假设人物水平方向上的迎风面都为0.5m^2
        // 垂直方向上都为0.1m^2
        // 其余参数取值与最大速度计算中相同
        // 垂直速度更新
        float acceleration = 0.052f * verticalSpeed * verticalSpeed / physicProperty.mass * Time.deltaTime;
        if(verticalSpeed > 0)
        {
            verticalSpeed = Mathf.Max(verticalSpeed - acceleration, 0.0f);
        }
        else
        {
            verticalSpeed = Mathf.Min(verticalSpeed + acceleration, 0.0f);
        }
        // 水平速度更新
        // 沿z轴方向
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
        // 跳跃状态不更新水平转向和速度
        if(animator.GetBool("Ground") == false)
        {
            return;
        }
        // 按键状态
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
            // 更新模型朝向
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
            // float类型会存在相同数值相减结果不为零的情况
            // 因此使用rotateDirec不为零的条件应该改为大于某个精度
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
                // 人物已经转到指定方向
                // 能够开始加速
                // 如果在冰面上
                // 那么该过程可能会已经存在平行和垂直于人物朝向的速度
                // 摩擦力的方向是非常不固定的
                // 这里将其简化为平行和垂直各获取√2的分量
                float verticalAcceleration = physicProperty.friction * physicProperty.gravity * Mathf.Sqrt(2) / 2 * Time.deltaTime;
                // 平行方向上还需要加上人蹬地的力
                // 设定最大300w功率下
                // 人蹬地的力为300N
                float parallelAcceleration = verticalAcceleration + physicProperty.friction * physicProperty.maxPower * physicProperty.powerRatio / 100 / physicProperty.mass * Time.deltaTime;
                float angleRad = targetAngle.y * Mathf.Deg2Rad;
                //float parallelSpeed = physicProperty.speed.z * Mathf.Cos(targetAngle.y * Mathf.Deg2Rad) - physicProperty.speed.x * Mathf.Cos((targetAngle.y + 90.0f) * Mathf.Deg2Rad);
                //float verticalSpeed = physicProperty.speed.x * Mathf.Sin((targetAngle.y + 90.0f) * Mathf.Deg2Rad) - physicProperty.speed.z * Mathf.Sin(targetAngle.y * Mathf.Deg2Rad);
                // 简化一下
                float parallelSpeed = horizontalSpeed.x * Mathf.Sin(angleRad) + horizontalSpeed.y * Mathf.Cos(angleRad);
                float verticalSpeed = horizontalSpeed.x * Mathf.Cos(angleRad) - horizontalSpeed.y * Mathf.Sin(angleRad);
                //parallelSpeed = Mathf.Clamp(parallelSpeed + acceleration, 0.0f, physicProperty.maxSpeed);
                //verticalSpeed = Mathf.Clamp(verticalSpeed - acceleration, 0.0f, physicProperty.maxSpeed);
                // 如果当前速度已经大于最大速度
                // 那么会以摩擦力来减速
                if(parallelSpeed > physicProperty.maxSpeed)
                {
                    parallelSpeed = Mathf.Max(parallelSpeed - parallelAcceleration, physicProperty.maxSpeed);
                }
                else
                {
                    parallelSpeed = Mathf.Min(parallelSpeed + parallelAcceleration, physicProperty.maxSpeed);
                }
                // 因为速度是带有符号的
                // 所以verticalSpeed的处理是针对数值
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
                // 更新动画
                animator.SetBool("Run", true);
                // 更新质量
                MassMovementReduce(physicProperty.maxPower * physicProperty.powerRatio / 100000 * Time.deltaTime);
                if(audioSource.isPlaying == false)
                {
                    audioSource.PlayOneShot(runClip);
                }
                return;
            }
        }
        // 真实情况下，人跑步模型的速度更新需要考虑空气阻力和地面
        // 不过考虑到游戏性和对摩擦系数的简化
        // 这里也忽略空气阻力的影响
        float speed = horizontalSpeed.magnitude;
        // 没有操作
        // 或者同时按下两个反方向按键的情况下
        // 或者当前人物还在转向的情况下
        // 只需要计算新的速度与旧的速度比例
        // 再乘以现在的速度即可
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
        // 更新动画
        animator.SetBool("Run", false);
    }

    private void UpdateVerticalSpeed()
    {
        // 跳跃过程与人物的功率和质量有很大的关系
        // 假设跳跃过程中与地面接触时间，即做工时间为1/3秒
        // 人的爆发功率是当前功率的15倍
        // 计算公式√(2*Pt/m)
        if (Input.GetKeyDown(KeyCode.Space) && animator.GetBool("Ground"))
        {
            isLand = false;
            audioSource.PlayOneShot(jumpClip);
            verticalSpeed = Mathf.Sqrt((physicProperty.powerRatio * physicProperty.maxPower / 50 + physicProperty.maxPower * 10) / physicProperty.mass);
            // 更新质量
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
        // 质量的消耗为了游戏性，设定不是非常严谨
        // 每秒消耗当前质量的1%
        physicProperty.mass -= physicProperty.mass * physicProperty.massReduceRatio * Time.deltaTime;
        if (physicProperty.mass < physicProperty.minMass)
        {
            // 如果小于一个最低质量设定
            // 那么游戏结束
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Hunger);
            PlayerDead("只运动不吃东西？你这不得瘦死！");
            Debug.Log("Game Over");
        }
        UIController.Instance.SetMass(physicProperty.mass);
    }

    private void MassMovementReduce(float reduce)
    {
        // 进行运动的能量消耗
        // 设定上1kw每秒消耗1kg
        physicProperty.mass -= reduce;
        if (physicProperty.mass < physicProperty.minMass)
        {
            // 如果小于一个最低质量设定
            // 那么游戏结束
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Hunger);
            PlayerDead("只运动不吃东西？你这不得瘦死！");
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
