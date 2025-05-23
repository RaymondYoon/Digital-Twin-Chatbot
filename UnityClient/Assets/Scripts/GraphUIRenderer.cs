using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphUIRenderer : MonoBehaviour
{
    public RectTransform graphArea;
    public GameObject dotPrefab;
    public GameObject linePrefab;
    public GameObject xLabelPrefab;
    public GameObject yLabelPrefab;

    private readonly List<GameObject> spawnedObjects = new();

    public void DrawGraph(List<float> values)
    {
        ClearGraph();
        if (values == null || values.Count < 2) return;

        float width = graphArea.rect.width;
        float height = graphArea.rect.height;

        float maxVal = Mathf.Max(values.ToArray());
        float minVal = Mathf.Min(values.ToArray());

        float range = Mathf.Max(0.1f, maxVal - minVal);
        float xSpacing = width / (values.Count - 1);

        Vector2[] positions = new Vector2[values.Count];

        // 점 + X축 라벨
        for (int i = 0; i < values.Count; i++)
        {
            float x = i * xSpacing;
            float y = ((values[i] - minVal) / range) * height;

            GameObject dot = Instantiate(dotPrefab, graphArea);
            dot.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            spawnedObjects.Add(dot);

            positions[i] = new Vector2(x, y);

            // X축 라벨
            if (xLabelPrefab != null)
            {
                GameObject xLabel = Instantiate(xLabelPrefab, graphArea);
                RectTransform xRt = xLabel.GetComponent<RectTransform>();
                float normalizedX = i / (float)(values.Count - 1);

                xRt.anchorMin = new Vector2(normalizedX, 0);
                xRt.anchorMax = new Vector2(normalizedX, 0);
                xRt.pivot = new Vector2(0.5f, 1);
                xRt.anchoredPosition = new Vector2(0, -5f);
                xLabel.GetComponentInChildren<Text>().text = $"T{i + 1}";
                spawnedObjects.Add(xLabel);
            }
        }

        // 선
        for (int i = 0; i < positions.Length - 1; i++)
        {
            Vector2 start = positions[i];
            Vector2 end = positions[i + 1];
            Vector2 dir = (end - start).normalized;
            float dist = Vector2.Distance(start, end);

            GameObject line = Instantiate(linePrefab, graphArea);
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(dist, 4f);
            rt.anchoredPosition = start + dir * dist / 2f;
            rt.rotation = Quaternion.FromToRotation(Vector3.right, end - start);
            spawnedObjects.Add(line);
        }

        // Y축 라벨 (하단/상단만)
        for (int i = 0; i <= 1; i++)
        {
            float ratio = i; // 0 또는 1
            float y = ratio * height;
            float val = minVal + (range * ratio);

            if (yLabelPrefab != null)
            {
                GameObject yLabel = Instantiate(yLabelPrefab, graphArea);
                RectTransform yRt = yLabel.GetComponent<RectTransform>();
                yRt.anchorMin = new Vector2(0, ratio);
                yRt.anchorMax = new Vector2(0, ratio);
                yRt.pivot = new Vector2(1, 0.5f);
                yRt.anchoredPosition = new Vector2(0, 0);
                yLabel.GetComponentInChildren<Text>().text = val.ToString("F1");
                spawnedObjects.Add(yLabel);
            }
        }
    }

    private void ClearGraph()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}
