using UnityEngine;
using UnityEngine.UI;

/**
界面上有三个输入框，分别对应 X,Y,Z 的值，请实现 {@link Q1.onGenerateBtnClick} 函数，生成一个 10 × 10 的可控随机矩阵，并显示到界面上，矩阵要求如下：
1. {@link COLORS} 中预定义了 5 种颜色
2. 每个点可选 5 种颜色中的 1 种
3. 按照从左到右，从上到下的顺序，依次为每个点生成颜色，(0, 0)为左上⻆点，(9, 9)为右下⻆点，(0, 9)为右上⻆点
4. 点(0, 0)随机在 5 种颜色中选取
5. 其他各点的颜色计算规则如下，设目标点坐标为(m, n）：
    a. (m, n - 1)所属颜色的概率为基准概率加 X%
    b. (m - 1, n)所属颜色的概率为基准概率加 Y%
    c. 如果(m, n - 1)和(m - 1, n)同色，则该颜色的概率为基准概率加 Z%
    d. 其他颜色平分剩下的概率
*/

public class Q1 : MonoBehaviour
{
    private static readonly Color[] COLORS = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        new Color(1f, 0.5f, 0f) // Orange
    };

    // 每个格子的大小
    private const float GRID_ITEM_SIZE = 75f;

    [SerializeField]
    private InputField xInputField = null;

    [SerializeField]
    private InputField yInputField = null;

    [SerializeField]
    private InputField zInputField = null;

    [SerializeField]
    private Transform gridRootNode = null;

    [SerializeField]
    private GameObject gridItemPrefab = null;

    public void OnGenerateBtnClick()
    {
        // TODO: 请在此处开始作答
        // 解析 X/Y/Z（百分比，例如输入 10 表示 10%）
        float xPercent = 0f, yPercent = 0f, zPercent = 0f;
        float.TryParse(xInputField != null ? xInputField.text : "0", out xPercent);
        float.TryParse(yInputField != null ? yInputField.text : "0", out yPercent);
        float.TryParse(zInputField != null ? zInputField.text : "0", out zPercent);

        float xFrac = Mathf.Clamp01(xPercent / 100f);
        float yFrac = Mathf.Clamp01(yPercent / 100f);
        float zFrac = Mathf.Clamp01(zPercent / 100f);

        if (gridRootNode == null || gridItemPrefab == null)
        {
            Debug.LogWarning("gridRootNode 或 gridItemPrefab 未设置。");
            return;
        }

        // 清空已有格子
        for (int i = gridRootNode.childCount - 1; i >= 0; i--)
        {
            var child = gridRootNode.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        const int SIZE = 10;
        int[,] assigned = new int[SIZE, SIZE]; // 行 m (0..9, 上->下)，列 n (0..9, 左->右)
        float baseProb = 1f / COLORS.Length;
        System.Random rnd = new System.Random();
        var offset = new Vector2(-GRID_ITEM_SIZE * SIZE / 2f + GRID_ITEM_SIZE / 2f, GRID_ITEM_SIZE * SIZE / 2f - GRID_ITEM_SIZE / 2f);
        for (int m = 0; m < SIZE; m++) // 行：从上到下
        {
            for (int n = 0; n < SIZE; n++) // 列：从左到右
            {
                int chosenIndex = 0;

                if (m == 0 && n == 0)
                {
                    // (0,0) 随机选取
                    chosenIndex = rnd.Next(0, COLORS.Length);
                    assigned[m, n] = chosenIndex;
                }
                else
                {
                    bool hasLeft = n - 1 >= 0;
                    bool hasUp = m - 1 >= 0;
                    int leftIndex = hasLeft ? assigned[m, n - 1] : -1;
                    int upIndex = hasUp ? assigned[m - 1, n] : -1;

                    float[] probs = new float[COLORS.Length];

                    // 特殊情况：上下都存在且同色 -> 该色 = base + Z，其他平分剩余
                    if (hasLeft && hasUp && leftIndex == upIndex)
                    {
                        int special = leftIndex;
                        float pSpecial = baseProb + zFrac;
                        // 防止超过1或负数
                        if (pSpecial < 0f) pSpecial = 0f;
                        if (pSpecial > 1f) pSpecial = 1f;
                        float remain = 1f - pSpecial;
                        int others = COLORS.Length - 1;
                        float otherProb = others > 0 ? remain / others : 0f;
                        for (int c = 0; c < COLORS.Length; c++)
                        {
                            probs[c] = (c == special) ? pSpecial : otherProb;
                        }
                    }
                    else
                    {
                        // 一般情况：先标记被加权的颜色及其加成
                        bool[] boosted = new bool[COLORS.Length];
                        float[] inc = new float[COLORS.Length];
                        if (hasLeft)
                        {
                            inc[leftIndex] += xFrac;
                            boosted[leftIndex] = true;
                        }
                        if (hasUp)
                        {
                            inc[upIndex] += yFrac;
                            boosted[upIndex] = true;
                        }

                        // 给被加权颜色分配 base + inc，其他颜色待分配剩余
                        float sumBoosted = 0f;
                        int boostedCount = 0;
                        for (int c = 0; c < COLORS.Length; c++)
                        {
                            if (boosted[c])
                            {
                                float p = baseProb + inc[c];
                                // 边界保护
                                if (p < 0f) p = 0f;
                                if (p > 1f) p = 1f;
                                probs[c] = p;
                                sumBoosted += probs[c];
                                boostedCount++;
                            }
                            else
                            {
                                probs[c] = 0f; // 暂时留空，稍后平分剩余
                            }
                        }

                        float remainProb = 1f - sumBoosted;
                        int others = COLORS.Length - boostedCount;
                        if (others > 0)
                        {
                            if (remainProb < 0f)
                            {
                                // 加成总和超出 1：退回到按比例归一化所有 base+inc
                                float[] provisional = new float[COLORS.Length];
                                float total = 0f;
                                for (int c = 0; c < COLORS.Length; c++)
                                {
                                    provisional[c] = baseProb + inc[c];
                                    if (provisional[c] < 0f) provisional[c] = 0f;
                                    total += provisional[c];
                                }
                                if (total <= 0f)
                                {
                                    // 极端情况回退到均匀分布
                                    for (int c = 0; c < COLORS.Length; c++) probs[c] = 1f / COLORS.Length;
                                }
                                else
                                {
                                    for (int c = 0; c < COLORS.Length; c++) probs[c] = provisional[c] / total;
                                }
                            }
                            else
                            {
                                float otherProb = remainProb / others;
                                for (int c = 0; c < COLORS.Length; c++)
                                {
                                    if (!boosted[c]) probs[c] = otherProb;
                                }
                            }
                        }
                        else
                        {
                            // 所有颜色都被加权（理论上只有 2 个被加权），但仍做保护：归一化
                            float total = 0f;
                            for (int c = 0; c < COLORS.Length; c++) total += probs[c];
                            if (total <= 0f)
                            {
                                for (int c = 0; c < COLORS.Length; c++) probs[c] = 1f / COLORS.Length;
                            }
                            else
                            {
                                for (int c = 0; c < COLORS.Length; c++) probs[c] /= total;
                            }
                        }
                    }

                    // 累加选择
                    float r = (float)rnd.NextDouble();
                    float acc = 0f;
                    chosenIndex = 0;
                    for (int c = 0; c < probs.Length; c++)
                    {
                        acc += probs[c];
                        if (r <= acc)
                        {
                            chosenIndex = c;
                            break;
                        }
                    }

                    assigned[m, n] = chosenIndex;
                }

                // 在 UI 上创建格子并设置位置与颜色
                GameObject item = GameObject.Instantiate(gridItemPrefab, gridRootNode);
                item.name = $"cell_{m}_{n}";

                // 尝试设置 RectTransform 位置（假设 root 的锚点为左上）
                RectTransform rt = item.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(n * GRID_ITEM_SIZE, -m * GRID_ITEM_SIZE) + offset;
                    rt.localScale = Vector3.one;
                    rt.sizeDelta = new Vector2(GRID_ITEM_SIZE, GRID_ITEM_SIZE);
                }

                // 设置颜色（尝试根结点 Image，然后子物体）
                Image img = item.GetComponent<Image>();
                if (img == null) img = item.GetComponentInChildren<Image>();
                if (img != null)
                {
                    img.color = COLORS[assigned[m, n]];
                }
            }
        }
    }
}
