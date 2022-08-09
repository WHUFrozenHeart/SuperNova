using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �������������
// ��������ƶ�
public class GameController : SingletonController<GameController>
{
    enum PatternSort
    {
        // ƥ�����
        Object,
        // ƥ�����
        // �ƶ���ִ�д���
        MultiObject,
        // ���������ж�������ƥ�����޷�����
        Once,
        // ����ƥ��涨����
        Multi,
        // ���������ж�������ƥ��һ��
        All
    }

    // ƥ��ģʽ�����ݽṹ
    class Pattern
    {
        // ʶ����
        public PatternSort id;
        // ִ�д���
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

    // ƥ��ֻ�������������ĸ��������
    enum MatchDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    // �洢ƥ����
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

    // �Ƿ�׼�������ɵ�ͼ
    private bool ready;
    // ��ʼ�Ĵ�������
    // ��Ҫָ�������С
    public int row = 9;
    public int col = 9;
    // ƥ�����
    // ������unity�����л�
    // ���ֻ���ڴ����й̶�
    private Pattern ownPattern;
    private char[, ] originMap;

    // ��ȡ��ҵ�λ��
    public Transform player;

    // ���ڳ���������prefab��������
    public List<string> borderGround;

    // �����ɵĵ�ͼ
    // ÿһ�ж�Ӧһ���ַ���
    // ��������ƶ�
    // �ٽ����ַ�������
    // ��ͨ�������������Ӧ�ĵ�ͼ
    private Queue<string> readyMap = new Queue<string>();
    // Ϊ����ִ�б߽�̽��
    // ÿ�ζ������ų������ɵ�ͼ֮�����һ����ȫ��
    private string safeRow;
    // �߽�̽��������
    private Queue<string> seamMap = new Queue<string>();
    private string seamRow;
    // ������Ҫ�ı���
    private float leftTilePos = -4.0f;
    private float tileLength = 1.0f;
    private float tileDistance = -1.0f;
    private float tileThreshold = 43.0f;
    // �߽���Ҫ�ı���
    private float rightBorderPos = 12.5f;
    private float leftBorderPos = -12.5f;
    private float borderLength = 15.0f;
    private float borderDistance = 0.0f;
    private float borderThreashold = 60.0f;
    // ������Ʒ
    public List<GameObject> usefulItem;
    public List<GameObject> trapItems;
    // �Ƿ���ͣ
    private bool isPause = false;
    // ת�����
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
    // ת��
    // 0��ʾ��ת
    // 1��ʾ��ת
    private int turnDirection = 0;
    // ת����ص���ֵ
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
        // ��ʼ��ƥ�����
        // Ŀǰƥ�����
        // 0��ʾ�ݵ�
        // 1��ʾʯ��
        // 2��ʾˮ��
        // 3��ʾ����
        // ��ʼ��ȫ������0
        // ֮������1��2��3��Ƭ����
        // ��֤1��2��3��Ƭ����֮���ǲ��ܹ�������
        ownPattern = new Pattern(PatternSort.Once);
        // ��ʼ����ʼ��
        ownPattern.patterns.Add(new Pattern("0", "1"));
        ownPattern.patterns.Add(new Pattern("0", "2"));
        ownPattern.patterns.Add(new Pattern("0", "3"));
        // ��Ƭ����
        Pattern areaPattern = new Pattern(PatternSort.Multi);
        areaPattern.count = 15;
        areaPattern.patterns.Add(new Pattern("01", "11"));
        areaPattern.patterns.Add(new Pattern("02", "22"));
        areaPattern.patterns.Add(new Pattern("03", "33"));
        ownPattern.patterns.Add(areaPattern);
        // ����1��2��3֮������
        Pattern avoidPattern = new Pattern(PatternSort.All);
        avoidPattern.patterns.Add(new Pattern("12", "10"));
        avoidPattern.patterns.Add(new Pattern("13", "03"));
        avoidPattern.patterns.Add(new Pattern("23", "20"));
        ownPattern.patterns.Add(avoidPattern);
        // ����Ҵ���һ����ȫ����
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
        // �߽�
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
                    // ������Ч�Ŀ�
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
                            // Ĭ�ϵ���
                            // ��
                            if ((mask & 0x1) == 0x1)
                            {
                                rotationAngle = 270.0f;
                            }
                            // ��
                            if ((mask & 0x2) == 0x2)
                            {
                                rotationAngle = 180.0f;
                            }
                            // ��
                            if ((mask & 0x4) == 0x4)
                            {
                                rotationAngle = 90.0f;
                            }
                        }
                        if (count == 2)
                        {
                            if ((mask & 0xa) == 0xa)
                            {
                                // �ᴩ���͵�����
                                name += "ThroughSide";
                            }
                            else if ((mask & 0x5) == 0x5)
                            {
                                // �ᴩ���͵�����
                                name += "ThroughSide";
                                rotationAngle = 90.0f;
                            }
                            else
                            {
                                // �������͵�����
                                name += "DoubleSide";
                                // ����
                                if ((mask & 0x3) == 0x3)
                                {
                                    rotationAngle = 180.0f;
                                }
                                // ����
                                if ((mask & 0x6) == 0x6)
                                {
                                    rotationAngle = 90.0f;
                                }
                                // ����
                                if ((mask & 0x9) == 0x9)
                                {
                                    rotationAngle = 270.0f;
                                }
                            }
                        }
                        if (count == 3)
                        {
                            name += "TriSide";
                            // ��
                            if ((mask & 0x1) == 0)
                            {
                                rotationAngle = 270.0f;
                            }
                            // ��
                            if ((mask & 0x2) == 0)
                            {
                                rotationAngle = 180.0f;
                            }
                            // ��
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
                            // ���ɵ���
                            if (itemSort == 0 || currentRow[index] == '2')
                            {
                                item = ObjectPoolController.Instance.GetGameObject(usefulItem[Random.Range(0, usefulItem.Count)].name);
                                item.transform.position = new Vector3(tileObject.transform.position.x, 2.0f, tileObject.transform.position.z);
                            }
                            // ��������
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
                        // ��Ҫ���б���
                        Debug.Log("���λ�ȡʧ��");
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
                Debug.Log("��ȡ����ʧ��");
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
        // Ϊ�˱���LOD�����bug
        // ��Ҫ�Ȱ�border����״̬����Ϊfalse
        // �ٽ���λ�ú�ת��ĵ���
        GameObject leftBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)], false);
        GameObject rightBorder = ObjectPoolController.Instance.GetGameObject(borderGround[Random.Range(0, borderGround.Count)], false);
        // ˮ������
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
        // ���ⱨ��
        // ʵ��û��Ӱ��
        // �ȵ�ͼ������Ϻ�
        // �Ჹ��û�����ɵĵ���
        if(readyMap.Count == 0)
        {
            return;
        }
        string currentRow = readyMap.Dequeue();
        string seam = seamMap.Dequeue();
        float hPos = leftTilePos;
        float vPos = tileDistance;
        // ������Ч�Ŀ�
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
                    // Ĭ�ϵ���
                    // ��
                    if((mask & 0x1) == 0x1)
                    {
                        rotationAngle = 270.0f;
                    }
                    // ��
                    if ((mask & 0x2) == 0x2)
                    {
                        rotationAngle = 180.0f;
                    }
                    // ��
                    if ((mask & 0x4) == 0x4)
                    {
                        rotationAngle = 90.0f;
                    }
                }
                if(count == 2)
                {
                    if((mask & 0xa) == 0xa)
                    {
                        // �ᴩ���͵�����
                        name += "ThroughSide";
                    }
                    else if ((mask & 0x5) == 0x5)
                    {
                        // �ᴩ���͵�����
                        name += "ThroughSide";
                        rotationAngle = 90.0f;
                    }
                    else
                    {
                        // �������͵�����
                        name += "DoubleSide";
                        // ����
                        if ((mask & 0x3) == 0x3)
                        {
                            rotationAngle = 180.0f;
                        }
                        // ����
                        if ((mask & 0x6) == 0x6)
                        {
                            rotationAngle = 90.0f;
                        }
                        // ����
                        if ((mask & 0x9) == 0x9)
                        {
                            rotationAngle = 270.0f;
                        }
                    }
                }
                if(count == 3)
                {
                    name += "TriSide";
                    // ��
                    if ((mask & 0x1) == 0)
                    {
                        rotationAngle = 270.0f;
                    }
                    // ��
                    if ((mask & 0x2) == 0)
                    {
                        rotationAngle = 180.0f;
                    }
                    // ��
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
                    // ���ɵ���
                    if(itemSort == 0 || currentRow[index] == '2')
                    {
                        //GameObject item = ObjectPoolController.Instance.GetGameObject(usefulItem[ReservoirSampling(usefulItem.Count)].name);
                        GameObject item = ObjectPoolController.Instance.GetGameObject(usefulItem[Random.Range(0, usefulItem.Count)].name);
                        item.transform.position = new Vector3(tileObject.transform.position.x, 2.0f, tileObject.transform.position.z);
                    }
                    // ��������
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
                // ��Ҫ���б���
                Debug.Log("���λ�ȡʧ��");
            }
            hPos += tileLength;
        }
        tileDistance += tileLength;
    }

    private IEnumerator RandomMap()
    {
        // ��Ҫ����������ɵ�ͼ
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

    // ���ɵ�ͼ
    // �ο�WFC���ߵ���һ���㷨�����е�ͼ������
    private void GenerateMap()
    {
        // ÿ�ε�ͼ���ɶ���9*9����ʽ��������
        // ÿ����Ϸ��ʼ��Ҫ��������
        // ֮�����������ƶ�����������������
        // ��ʼ����ͼ
        char[, ] map = new char[9, 9];
        System.Array.Copy(originMap, map, map.Length);
        // ѡȡ��ʼ��
        Markvo(map, ownPattern);
        // ʹ��4λ�����ҷֱ��ʾ���ϡ��ҡ���
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
                // ��
                if (i == row - 1 || map[i + 1, j] == '0')
                {
                    current |= 1 << 2;
                }
                //  ��
                if (i == 0 || map[i - 1, j] == '0')
                {
                    current |= 1;
                }
                // ��
                if(j == 0 || map[i, j - 1] == '0')
                {
                    current |= 1 << 3;
                }
                // ��
                if(j == col - 1 || map[i, j + 1] == '0')
                {
                    current |= 1 << 1;
                }
                if(current == 0x1f)
                {
                    //  ȥ�����������
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
                // �������Ӧ�ñ���
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

    // ����ģʽ����ƥ��
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

    // ��ƥ���������滻
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
                // ��������Ǵ����Ӧ���׳��쳣
                break;
        }
    }

    // ˮ���������
    // ����ѡ�е�˳��
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
        // ���ѡȡһ����������滻
        ChangeMap(map, results[Random.Range(0, results.Count)], pattern.replace);
        // ˮ������
        //ChangeMap(map, results[ReservoirSampling(results.Count)], pattern.replace);
        return true;
    }

    // ���������汾
    // ����ܹ�ƥ����Ӧ�����Ĺ���
    // �����ȿ���ʵ��ͬʱƥ�������
    // ��ͬ�����С������
    // ���ܹ�����ƥ��ģʽ����д
    private bool MatchMultiOjbect(char[,] map, Pattern pattern)
    {
        // ���ڶ��ƥ��
        // �������Allģʽ֮��
        // ��ôֻ������һ��ƥ����true
        // ��ôAllģʽ���ܹ�������ȥ
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
        // ���α������������ж���
        // ���ж�ε���
        // ֱ��û���ܹ�ƥ���
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

    // ����������ɵ�ͼ��
    // ��ô��Ҫ����ر������и���
    public void ReadyToGenerate()
    {
        ready = true;
    }
}
