using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

//Pathをこのスクリプトの子に設定して名前を(1)からインクリメント
//Editorで右上のオプションからFind Path Points Automaticallyで実行
[ExecuteAlways]
public class PathPointAutoSetter : MonoBehaviour
{
    [Header("セット開始の基準になるTransform")]
    public Transform startingPoint;

    [Header("命名ルール（例：PathPoint）→ PathPoint, PathPoint (1), PathPoint (2)...を検索")]
    public string baseName = "GameObject";

    [Header("セット結果（Copyして使用するための結果）")]
    public Transform[] result;

    [ContextMenu("Find Path Points Automatically")]
    public void FindPathPoints()
    {
        if (startingPoint == null)
        {
            Debug.LogWarning("Starting point is not set.");
            return;
        }

        Transform parent = startingPoint.parent;
        if (parent == null)
        {
            Debug.LogWarning("Starting point must have a parent.");
            return;
        }

        // 名前の形式：PathPoint, PathPoint(1), PathPoint (1) など
        string pattern = $"^{Regex.Escape(baseName)}(?:\\s*\\((\\d+)\\))?$";
        Regex regex = new Regex(pattern);

        SortedDictionary<int, Transform> ordered = new SortedDictionary<int, Transform>();

        foreach (Transform child in parent)
        {
            Match match = regex.Match(child.name);
            if (match.Success)
            {
                int index = 0;
                if (match.Groups[1].Success)
                {
                    int.TryParse(match.Groups[1].Value, out index);
                }
                ordered[index] = child;
            }
        }

        result = new List<Transform>(ordered.Values).ToArray();
        Debug.Log($"✅ Found {result.Length} path points under '{parent.name}'.");
    }
}
