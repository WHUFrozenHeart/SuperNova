using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 挂载在相机上面
// 方便进行移动
public class GameController : SingletonController<GameController>
{
    enum PatternSort
    {
        // 匹配对象
        Object,
        // 匹配对象
        // 制定了执行次数
        MultiObject,
        // 数组内所有对象依次匹配至无法继续
        Once,
        // 组内匹配规定次数
        Multi,
        // 数组内所有对象依次匹配一次
        All
    }

    // 匹配模式的数据结构
    class Pattern
    {
        // 识别码
        public PatternSort id;
        // 执行次数
        public int count;
        public string match;
        public string replace;
        public List<Pattern> patterns;

        public Pattern() { }

        public Pattern(PatternSort id)
        {
            this.id = id;
            patterns = new List<Pattern>();
        }

        public Pattern(string match, string replace)
        {
            id = PatternSort.Object;
            this.match = match;
            this.replace = replace;
            patterns = null;
        }

        public Pattern(string match, string replace, int count)
        {
            id = PatternSort.MultiObject;
            this.match = match;
            this.replace = replace;
            this.count = count;
            patterns = null;
        }
    }

    // 匹配只能在上下左右四个方向进行
    enum MatchDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    // 存储匹配结果
    class MatchResult
    {
        public int r;
        public int c;
        public MatchDirection direction;

        public MatchResult(int r, int c, MatchDirection direction)
        {
            this.r = r;
            this.c = c;
            this.direction = direction;
        }
    }

    // 是否准备好生成地图
    private bool ready;
    // 初始的处理数组
    // 需要指定数组大小
    public int row = 9;
    public int col = 9;
    // 匹配规则
    // 不能在unity中序列化
    // 因此只能在代码中固定
    private Pattern ownPattern;
    private char[, ] originMap;

    // 获取玩家的位置
    public Transform player;

    // 用于场景创建的prefab名称数组
    public List<string> borderGround;

    // 已生成的地图
    // 每一行对应一个字符串
    // 根据玩家移动
    // 再进行字符串解析
    // 并通过对象池生成相应的地图
    private Queue<string> readyMap = new Queue<string>();
    // 为方便执行边界探测
    // 每次都在两张程序化生成地图之间添加一个安全行
    private string safeRow;
    // 边界探测结果队列
    private Queue<string> seamMap = new Queue<string>();
    private string seamRow;
    // 地形需要的变量
    private float leftTilePos = -4.0f;
    private float tileLength = 1.0f;
    private float tileDistance = -1.0f;
    private float tileThreshold = 43.0f;
    // 边界需要的变量
    private float rightBorderPos = 12.5f;
    private float leftBorderPos = -12.5f;
    private float borderLength = 15.0f;
    private float borderDistance = 0.0f;
    private float borderThreashold = 60.0f;
    // 各种物品
    public List<GameObject> usefulItem;
    public List<GameObject> trapItems;
    // 是否暂停
    private bool isPause = false;
    // 转向相关
    public enum TurnState
    {
        None,
        Fill,
        Safe,
        Border,
        Ready,
        Turn,
        Wait
    }
    public static TurnState turnState = TurnState.None;
    // 转向
    // 0表示左转
    // 1表示右转
    private int turnDirection = 0;
    // 转向相关的阈值
    private float turnDistance;
    private int minBorderThreshold = 6;
    private int maxBorderThreshold = 10;
    private int safeIndex = 0;
    private int borderSize;
    private int tileSize;

    protected override void Awake()
    {
        base.Awake();
        ready = true;
        originMap = new char[row, col];
        for (int i = 0; i < row; ++i)
        {
            for (int j = 0; j < col; ++j)
            {
                originMap[i, j] = '0';
            }
        }
        // 初始化匹配机制
        // 目前匹配规则
        // 0表示草地
        // 1表示石地
        // 2表示水地
        // 3表示冰地
        // 初始化全部都是0
        // 之后生成1、2、3连片区域
        // 保证1、2、3连片区域之间是不能够相连的
        ownPattern = new Pattern(PatternSort.Once);
        // 初始化起始点
        ownPattern.patterns.Add(new Pattern("0", "1"));
        ownPattern.patterns.Add(new Pattern("0", "2"));
        ownPattern.patterns.Add(new Pattern("0", "3"));
        // 连片生成
        Pattern areaPattern = new Pattern(PatternSort.Multi);
        areaPattern.count = 15;
        areaPattern.patterns.Add(new Pattern("01", "11"));
        areaPattern.patterns.Add(new Pattern("02", "22"));
        areaPattern.patterns.Add(new Pattern("03", "33"));
        ownPattern.patterns.Add(areaPattern);
        // 避免1、2、3之间相连
        Pattern avoidPattern = new Pattern(PatternSort.All);
        avoidPattern.patterns.Add(new Pattern("12", "10"));
        avoidPattern.patterns.Add(new Pattern("13", "03"));
        avoidPattern.patterns.Add(new Pattern("23", "20"));
        ownPattern.patterns.Add(avoidPattern);
        // 给玩家创建一个安全区域
        safeRow = "";
        seamRow = new string((char)16, row);
        for (int i = 0;i < col;++i)
        {
            safeRow += '0';
        }
        for(int i = 0;i < 5;++i)
        {
            readyMap.Enqueue(safeRow);
            seamMap.Enqueue(seamRow);
        }
        // 边界
        InitMap();
    }

    private void Start()
    {
        Cursor.visible = false;
        turnDistance = Random.Range(minBorderThreshold, maxBorderThreshold) * borderLength;
        turnState = TurnState.None;
        StartCoroutine(RandomMap());
    }

    private void Update()
    {
        if(turnState == TurnState.None)
        {
            UpdateRelativePosition();
        }
        else if(turnState == TurnState.Fill)
        {
            if(tileDistance < borderDistance - borderLength + 8)
            {
                if(readyMap.Count < 9)
                {
                    ready = true;
                }
                CreateRowMap();
            }
            else
            {
                turnState = TurnState.Safe;
                PlayerController.readyToTurn = true;
                PlayerController.turnDistance = tileDistance;
                safeIndex = 0;
                Debug.Log(turnState);
            }
        }
        else if(turnState == TurnState.Safe)
        {
            if(safeIndex < 9)
            {
                CreateSafeRow();
                ++safeIndex;
            }
            else
            {
                turnState = TurnState.Border;
                RecycleController.turnPoint = new Vector3(0.0f, 0.0f, tileDistance - 5);
                Debug.Log(turnState);
            }
        }
        else if(turnState == TurnState.Border)
        {
            turnState = TurnState.Ready;
            readyMap.Clear();
            seamMap.Clear();
            GameObject topBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)]);
            topBorder.transform.position = new Vector3(0.0f, 0.0f, tileDistance + 7.5f);
            topBorder.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
            GameObject sideBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)]);
            if(turnDirection == 0)
            {
                sideBorder.transform.position = new Vector3(rightBorderPos, 0.0f, borderDistance);
                sideBorder.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
            }
            else
            {
                sideBorder.transform.position = new Vector3(leftBorderPos, 0.0f, borderDistance);
                sideBorder.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            }
            Debug.Log(turnState);
        }
        else if(turnState == TurnState.Ready)
        {
            if(readyMap.Count < tileThreshold)
            {
                 GenerateMap();
            }
            else
            {
                turnState = TurnState.Turn;
                borderSize = (int)(borderThreashold / borderLength);
                tileSize = (int)(tileThreshold / tileLength);
                Debug.Log(turnState);
            }
        }
        else if(turnState == TurnState.Turn)
        {
            if(borderSize > 0)
            {
                --borderSize;
                float offset = 0.5f;
                GameObject topBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)]);
                GameObject bottomBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)]);
                topBorder.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                bottomBorder.transform.rotation = Quaternion.Euler(0.0f, 270.0f, 0.0f);
                if(turnDirection == 0)
                {
                    topBorder.transform.position = new Vector3(leftBorderPos - borderSize * borderLength + offset, 0.0f, borderDistance + 9f + offset);
                    bottomBorder.transform.position = new Vector3(leftBorderPos - borderSize * borderLength + offset, 0.0f, borderDistance - borderLength - offset);
                }
                else
                {
                    topBorder.transform.position = new Vector3(rightBorderPos + borderSize * borderLength - offset, 0.0f, borderDistance + 9f + offset);
                    bottomBorder.transform.position = new Vector3(rightBorderPos + borderSize * borderLength - offset, 0.0f, borderDistance - borderLength - offset);
                }
            }
            if(tileSize > 0)
            {
                --tileSize;
                float hPos = tileThreshold - tileSize + 4;
                float vPos = tileDistance - 1;
                if (turnDirection == 0)
                {
                    hPos = -hPos;
                    vPos -= 8;
                }
                string currentRow = readyMap.Dequeue();
                string seam = seamMap.Dequeue();
                for(int i = 0;i < col;++i)
                {
                    // 生成特效的块
                    int index = -1;
                    if (Random.Range(0, 3) == 0)
                    {
                        index = Random.Range(1, col - 1);
                    }
                    GameObject tileObject = null;
                    float rotationAngle = 0.0f;
                    if (currentRow[i] == '0')
                    {
                        tileObject = ObjectPoolController.Instance.GetGameObject("GrassGround");
                    }
                    else
                    {
                        string name = "";
                        if (currentRow[i] == '1')
                        {
                            name += "StoneGround";
                        }
                        if (currentRow[i] == '2')
                        {
                            name += "WaterGround";
                        }
                        if (currentRow[i] == '3')
                        {
                            name += "IceGround";
                        }
                        int mask = seam[i] & 0xf;
                        int count = 0;
                        for (int j = 0; j < 4; ++j)
                        {
                            if ((mask & (1 << j)) == 0)
                            {
                                continue;
                            }
                            ++count;
                        }
                        if (count == 1)
                        {
                            name += "SingleSide";
                            // 默认单边
                            // 下
                            if ((mask & 0x1) == 0x1)
                            {
                                rotationAngle = 270.0f;
                            }
                            // 右
                            if ((mask & 0x2) == 0x2)
                            {
                                rotationAngle = 180.0f;
                            }
                            // 上
                            if ((mask & 0x4) == 0x4)
                            {
                                rotationAngle = 90.0f;
                            }
                        }
                        if (count == 2)
                        {
                            if ((mask & 0xa) == 0xa)
                            {
                                // 贯穿类型的两边
                                name += "ThroughSide";
                            }
                            else if ((mask & 0x5) == 0x5)
                            {
                                // 贯穿类型的两边
                                name += "ThroughSide";
                                rotationAngle = 90.0f;
                            }
                            else
                            {
                                // 相邻类型的两边
                                name += "DoubleSide";
                                // 右下
                                if ((mask & 0x3) == 0x3)
                                {
                                    rotationAngle = 180.0f;
                                }
                                // 右上
                                if ((mask & 0x6) == 0x6)
                                {
                                    rotationAngle = 90.0f;
                                }
                                // 左下
                                if ((mask & 0x9) == 0x9)
                                {
                                    rotationAngle = 270.0f;
                                }
                            }
                        }
                        if (count == 3)
                        {
                            name += "TriSide";
                            // 下
                            if ((mask & 0x1) == 0)
                            {
                                rotationAngle = 270.0f;
                            }
                            // 右
                            if ((mask & 0x2) == 0)
                            {
                                rotationAngle = 180.0f;
                            }
                            // 上
                            if ((mask & 0x4) == 0)
                            {
                                rotationAngle = 90.0f;
                            }
                        }
                        if (count == 4)
                        {
                            name += "FullSide";
                        }
                        tileObject = ObjectPoolController.Instance.GetGameObject(name);
                    }
                    if (tileObject != null)
                    {
                        tileObject.transform.position = new Vector3(hPos, 0.0f, vPos);
                        if(turnDirection == 0)
                        {
                            tileObject.transform.rotation = Quaternion.Euler(0.0f, rotationAngle - 90.0f, 0.0f);
                        }
                        else
                        {
                            tileObject.transform.rotation = Quaternion.Euler(0.0f, rotationAngle + 90.0f, 0.0f);
                        }
                        if (i == index)
                        {
                            int itemSort = Random.Range(0, 2);
                            GameObject item;
                            // 生成道具
                            if (itemSort == 0 || currentRow[index] == '2')
                            {
                                item = ObjectPoolController.Instance.GetGameObject(usefulItem[Random.Range(0, usefulItem.Count)].name);
                                item.transform.position = new Vector3(tileObject.transform.position.x, 2.0f, tileObject.transform.position.z);
                            }
                            // 生成陷阱
                            else
                            {
                                item = ObjectPoolController.Instance.GetGameObject(trapItems[Random.Range(0, trapItems.Count)].name);
                                item.transform.position = tileObject.transform.position;
                            }
                            if(turnDirection == 0)
                            {
                                item.transform.rotation = Quaternion.Euler(item.transform.rotation.eulerAngles - new Vector3(0.0f, 90.0f, 0.0f));
                            }
                            else
                            {
                                item.transform.rotation = Quaternion.Euler(item.transform.rotation.eulerAngles + new Vector3(0.0f, 90.0f, 0.0f));
                            }
                        }
                    }
                    else
                    {
                        // 需要进行报错
                        Debug.Log("地形获取失败");
                    }
                    if(turnDirection == 0)
                    {
                        ++vPos;
                    }
                    else
                    {
                        --vPos;
                    }
                }
            }
            if(tileSize == 0 && borderSize == 0)
            {
                turnState = TurnState.Wait;
                borderDistance += borderThreashold + 9;
                tileDistance += tileThreshold;
                turnDistance = borderDistance + Random.Range(minBorderThreshold, maxBorderThreshold) * borderLength;
            }
        }
        PauseGame();
    }

    private void PauseGame()
    {
        if(Input.GetKeyDown(KeyCode.Escape) && isPause == false)
        {
            isPause = true;
            PauseGamePanelController.Instance.ShowPanel();
            Time.timeScale = 0.0f;
        }
    }

    public void ResumeGame()
    {
        isPause = false;
        Time.timeScale = 1.0f;
    }

    private void UpdateRelativePosition()
    {
        if(tileDistance - player.position.z < tileThreshold)
        {
            if(readyMap.Count < 9)
            {
                ready = true;
            }
            CreateRowMap();
        }
        if(borderDistance - player.position.z < borderThreashold)
        {
            CreateBorder();
        }
        if (borderDistance == turnDistance)
        {
            turnState = TurnState.Fill;
            turnDirection = Random.Range(0, 2);
            if (turnDirection == 0)
            {
                RecycleController.turnDirection = Vector3.up;
            }
            else
            {
                RecycleController.turnDirection = Vector3.down;
            }
            Debug.Log("turn distance" + turnDistance);
            Debug.Log(turnState);
        }
    }

    private void CreateSafeRow()
    {
        float vPos = tileDistance;
        float hPos = leftTilePos;
        for (int j = 0;j < col;++j)
        {
            GameObject tileObject = ObjectPoolController.Instance.GetGameObject("GrassGround");
            if(tileObject != null)
            {
                tileObject.transform.position = new Vector3(hPos, 0.0f, vPos);
            }
            else
            {
                Debug.Log("获取地形失败");
            }
            hPos += tileLength;
        }
        tileDistance += tileLength;
    }

    private void InitMap()
    {
        for(int i = 0;i < 4;++i)
        {
            GenerateMap();
        }
        int size = readyMap.Count;
        for(int i = 0;i < size;++i)
        {
            CreateRowMap();
        }
        for(int i = 0;i < 5;++i)
        {
            CreateBorder();
        }
    }

    private void CreateBorder()
    {
        // 为了避免LOD组件的bug
        // 需要先把border激活状态设置为false
        // 再进行位置和转向的调整
        GameObject leftBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)], false);
        GameObject rightBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)], false);
        // 水塘抽样
        //GameObject leftBorder = ObjectPoolController.Instance.GetGameObject(borderGround[ReservoirSampling(borderGround.Count)], false);
        //GameObject rightBorder = ObjectPoolController.Instance.GetGameObject(borderGround[ReservoirSampling(borderGround.Count)], false);
        leftBorder.transform.rotation = Quaternion.identity;
        leftBorder.transform.position = new Vector3(leftBorderPos, 0.0f, borderDistance);
        rightBorder.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
        rightBorder.transform.position = new Vector3(rightBorderPos, 0.0f, borderDistance);
        leftBorder.SetActive(true);
        rightBorder.SetActive(true);
        borderDistance += borderLength;
    }

    private void CreateRowMap()
    {
        // 避免报错
        // 实际没有影响
        // 等地图生成完毕后
        // 会补上没有生成的地形
        if(readyMap.Count == 0)
        {
            return;
        }
        string currentRow = readyMap.Dequeue();
        string seam = seamMap.Dequeue();
        float hPos = leftTilePos;
        float vPos = tileDistance;
        // 生成特效的块
        int index = -1;
        if(Random.Range(0, 3) == 0)
        {
            index = Random.Range(1, col - 1);
        }
        for (int i = 0;i < col;++i)
        {
            GameObject tileObject = null;
            float rotationAngle = 0.0f;
            if (currentRow[i] == '0')
            {
                tileObject = ObjectPoolController.Instance.GetGameObject("GrassGround");
            }
            else
            {
                string name = "";
                if(currentRow[i] == '1')
                {
                    name += "StoneGround";
                }
                if (currentRow[i] == '2')
                {
                    name += "WaterGround";
                }
                if (currentRow[i] == '3')
                {
                    name += "IceGround";
                }
                int mask = seam[i] & 0xf;
                int count = 0;
                for(int j = 0;j < 4;++j)
                {
                    if((mask & (1 << j)) == 0)
                    {
                        continue;
                    }
                    ++count;
                }
                if(count == 1)
                {
                    name += "SingleSide";
                    // 默认单边
                    // 下
                    if((mask & 0x1) == 0x1)
                    {
                        rotationAngle = 270.0f;
                    }
                    // 右
                    if ((mask & 0x2) == 0x2)
                    {
                        rotationAngle = 180.0f;
                    }
                    // 上
                    if ((mask & 0x4) == 0x4)
                    {
                        rotationAngle = 90.0f;
                    }
                }
                if(count == 2)
                {
                    if((mask & 0xa) == 0xa)
                    {
                        // 贯穿类型的两边
                        name += "ThroughSide";
                    }
                    else if ((mask & 0x5) == 0x5)
                    {
                        // 贯穿类型的两边
                        name += "ThroughSide";
                        rotationAngle = 90.0f;
                    }
                    else
                    {
                        // 相邻类型的两边
                        name += "DoubleSide";
                        // 右下
                        if ((mask & 0x3) == 0x3)
                        {
                            rotationAngle = 180.0f;
                        }
                        // 右上
                        if ((mask & 0x6) == 0x6)
                        {
                            rotationAngle = 90.0f;
                        }
                        // 左下
                        if ((mask & 0x9) == 0x9)
                        {
                            rotationAngle = 270.0f;
                        }
                    }
                }
                if(count == 3)
                {
                    name += "TriSide";
                    // 下
                    if ((mask & 0x1) == 0)
                    {
                        rotationAngle = 270.0f;
                    }
                    // 右
                    if ((mask & 0x2) == 0)
                    {
                        rotationAngle = 180.0f;
                    }
                    // 上
                    if ((mask & 0x4) == 0)
                    {
                        rotationAngle = 90.0f;
                    }
                }
                if(count == 4)
                {
                    name += "FullSide";
                }
                tileObject = ObjectPoolController.Instance.GetGameObject(name);
            }
            if (tileObject != null)
            {
                tileObject.transform.position = new Vector3(hPos, 0.0f, vPos);
                tileObject.transform.rotation = Quaternion.Euler(0.0f, rotationAngle, 0.0f);
                if(tileDistance > 5 && i == index)
                {
                    //int itemSort = ReservoirSampling(2);
                    int itemSort = Random.Range(0, 2);
                    // 生成道具
                    if(itemSort == 0 || currentRow[index] == '2')
                    {
                        //GameObject item = ObjectPoolController.Instance.GetGameObject(usefulItem[ReservoirSampling(usefulItem.Count)].name);
                        GameObject item = ObjectPoolController.Instance.GetGameObject(usefulItem[Random.Range(0, usefulItem.Count)].name);
                        item.transform.position = new Vector3(tileObject.transform.position.x, 2.0f, tileObject.transform.position.z);
                    }
                    // 生成陷阱
                    else
                    {
                        //item = ObjectPoolController.Instance.GetGameObject(trapItems[ReservoirSampling(trapItems.Count)].name);
                        GameObject item = ObjectPoolController.Instance.GetGameObject(trapItems[Random.Range(0, trapItems.Count)].name);
                        item.transform.position = tileObject.transform.position;
                    }
                }
            }
            else
            {
                // 需要进行报错
                Debug.Log("地形获取失败");
            }
            hPos += tileLength;
        }
        tileDistance += tileLength;
    }

    private IEnumerator RandomMap()
    {
        // 需要不断随机生成地图
        while(true)
        {
            if(ready)
            {
                GenerateMap();
                ready = false;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    // 生成地图
    // 参考WFC作者的另一个算法来进行地图的生成
    private void GenerateMap()
    {
        // 每次地图生成都以9*9的形式进行生成
        // 每次游戏开始需要生成三次
        // 之后根据人物的移动来决定后续的生成
        // 初始化地图
        char[, ] map = new char[9, 9];
        System.Array.Copy(originMap, map, map.Length);
        // 选取起始点
        Markvo(map, ownPattern);
        // 使用4位从左到右分别表示左、上、右、下
        for(int i = 0;i < row;++i)
        {
            string currentRow = "";
            for(int j = 0;j < col;++j)
            {
                if(map[i, j] == '0')
                {
                    currentRow += (char)16;
                    continue;
                }
                int current = 16;
                // 上
                if (i == row - 1 || map[i + 1, j] == '0')
                {
                    current |= 1 << 2;
                }
                //  下
                if (i == 0 || map[i - 1, j] == '0')
                {
                    current |= 1;
                }
                // 左
                if(j == 0 || map[i, j - 1] == '0')
                {
                    current |= 1 << 3;
                }
                // 右
                if(j == col - 1 || map[i, j + 1] == '0')
                {
                    current |= 1 << 1;
                }
                if(current == 0x1f)
                {
                    //  去掉单独的情况
                    current = 16;
                    map[i, j] = '0';
                }
                currentRow += (char)current;
            }
            seamMap.Enqueue(currentRow);
        }
        seamMap.Enqueue(seamRow);
        for (int i = 0; i < row; ++i)
        {
            string currentRow = "";
            for (int j = 0; j < col; ++j)
            {
                currentRow += map[i, j];
            }
            readyMap.Enqueue(currentRow);
        }
        readyMap.Enqueue(safeRow);
    }

    private bool Markvo(char[,] map, Pattern pattern)
    {
        if(pattern == null)
        {
            return false;
        }
        switch(pattern.id)
        {
            case PatternSort.Object:
                return MatchObject(map, pattern);
            case PatternSort.MultiObject:
                return MatchMultiOjbect(map, pattern);
            case PatternSort.Once:
                return MatchOnce(map, pattern.patterns);
            case PatternSort.Multi:
                return MatchMulti(map, pattern.patterns, pattern.count);
            case PatternSort.All:
                return MatchAll(map, pattern.patterns);
            default:
                // 其他情况应该报错
                return false;
        }
    }

    private bool MatchUp(char[,] map, string match, int r, int c)
    {
        for(int i = 0;i < match.Length;++i)
        {
            if(r - i < 0)
            {
                return false;
            }
            if (match[i] == '*')
            {
                continue;
            }
            if (map[r - i, c] != match[i])
            {
                return false;
            }
        }
        return true;
    }

    private bool MatchDown(char[,] map, string match, int r, int c)
    {
        for (int i = 0; i < match.Length; ++i)
        {
            if (r + i >= row)
            {
                return false;
            }
            if (match[i] == '*')
            {
                continue;
            }
            if (map[r + i, c] != match[i])
            {
                return false;
            }
        }
        return true;
    }

    private bool MatchLeft(char[,] map, string match, int r, int c)
    {
        for (int i = 0; i < match.Length; ++i)
        {
            if (c - i < 0)
            {
                return false;
            }
            if (match[i] == '*')
            {
                continue;
            }
            if (map[r, c - i] != match[i])
            {
                return false;
            }
        }
        return true;
    }

    private bool MatchRight(char[,] map, string match, int r, int c)
    {
        for (int i = 0; i < match.Length; ++i)
        {
            if (c + i >= col)
            {
                return false;
            }
            if (match[i] == '*')
            {
                continue;
            }
            if (map[r, c + i] != match[i])
            {
                return false;
            }
        }
        return true;
    }

    // 根据模式进行匹配
    private void GetResult(char[,] map, string match, List<MatchResult> results)
    {
        for(int i = 0;i < row;++i)
        {
            for(int j = 0;j < col;++j)
            {
                if(MatchUp(map, match, i, j))
                {
                    results.Add(new MatchResult(i, j, MatchDirection.Up));
                }
                if (MatchDown(map, match, i, j))
                {
                    results.Add(new MatchResult(i, j, MatchDirection.Down));
                }
                if (MatchLeft(map, match, i, j))
                {
                    results.Add(new MatchResult(i, j, MatchDirection.Left));
                }
                if (MatchRight(map, match, i, j))
                {
                    results.Add(new MatchResult(i, j, MatchDirection.Right));
                }
            }
        }
    }

    // 将匹配结果进行替换
    private void ChangeMap(char[,] map, MatchResult result, string replace)
    {
        switch(result.direction)
        {
            case MatchDirection.Up:
                for(int i = 0;i < replace.Length;++i)
                {
                    if (replace[i] == '*')
                    {
                        continue;
                    }
                    map[result.r - i, result.c] = replace[i];
                }
                break;
            case MatchDirection.Down:
                for (int i = 0; i < replace.Length; ++i)
                {
                    if (replace[i] == '*')
                    {
                        continue;
                    }
                    map[result.r + i, result.c] = replace[i];
                }
                break;
            case MatchDirection.Left:
                for (int i = 0; i < replace.Length; ++i)
                {
                    if (replace[i] == '*')
                    {
                        continue;
                    }
                    map[result.r, result.c - i] = replace[i];
                }
                break;
            case MatchDirection.Right:
                for (int i = 0; i < replace.Length; ++i)
                {
                    if (replace[i] == '*')
                    {
                        continue;
                    }
                    map[result.r, result.c + i] = replace[i];
                }
                break;
            default:
                // 其他情况是错误的应该抛出异常
                break;
        }
    }

    // 水塘抽样随机
    // 返回选中的顺序
    private int ReservoirSampling(int count, float ratio = 1.0f)
    {
        int result = -1;
        for(int i = 0;i < count;++i)
        {
            if(Random.Range(0, (i + 1) * ratio) <= 1)
            {
                result = i;
            }
        }
        return result;
    }

    private bool MatchObject(char[,] map, Pattern pattern)
    {
        List<MatchResult> results = new List<MatchResult>();
        GetResult(map, pattern.match, results);
        if(results.Count == 0)
        {
            return false;
        }
        // 随机选取一个结果进行替换
        ChangeMap(map, results[Random.Range(0, results.Count)], pattern.replace);
        // 水塘抽样
        //ChangeMap(map, results[ReservoirSampling(results.Count)], pattern.replace);
        return true;
    }

    // 相较于最初版本
    // 添加能够匹配相应次数的功能
    // 这样既可以实现同时匹配过程中
    // 不同区域大小的区别
    // 还能够方便匹配模式的书写
    private bool MatchMultiOjbect(char[,] map, Pattern pattern)
    {
        // 对于多次匹配
        // 如果是在All模式之下
        // 那么只有任意一次匹配是true
        // 那么All模式就能够继续下去
        bool anyMatch = false;
        for(int i = 0;i < pattern.count;++i)
        {
            anyMatch |= MatchObject(map, pattern);
        }
        return anyMatch;
    }

    private bool MatchOnce(char[,] map, List<Pattern> patterns)
    {
        bool anyMatch = false;
        patterns.ForEach((pattern) => {
            anyMatch |= Markvo(map, pattern);
        });
        return anyMatch;
    }

    private bool MatchMulti(char[,] map, List<Pattern> patterns, int count)
    {
        bool anyMatch = false;
        for(int i = 0;i < count;++i)
        {
            anyMatch |= MatchOnce(map, patterns);
        }
        return anyMatch;
    }

    private bool MatchAll(char[,] map, List<Pattern> patterns)
    {
        // 依次遍历数组中所有对象
        // 进行多次迭代
        // 直到没有能够匹配的
        bool anyMatch = false;
        while(true)
        {
            patterns.ForEach((pattern) => {
                anyMatch |= Markvo(map, pattern);
            });
            if(anyMatch == false)
            {
                break;
            }
            anyMatch = false;
        }
        return false;
    }

    // 如果可以生成地图了
    // 那么需要将相关变量进行更改
    public void ReadyToGenerate()
    {
        ready = true;
    }
}
