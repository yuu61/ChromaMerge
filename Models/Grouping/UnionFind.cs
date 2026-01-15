using System;
using System.Collections.Generic;

namespace ChromaMerge.Models.Grouping;

/// <summary>
/// Union-Find (素集合データ構造) 実装
/// 経路圧縮とランクによる最適化を含む
/// </summary>
public class UnionFind
{
    private readonly int[] _parent;
    private readonly int[] _rank;
    private readonly int[] _size;

    /// <summary>
    /// 要素数
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// 現在のグループ数
    /// </summary>
    public int GroupCount { get; private set; }

    /// <summary>
    /// 指定したサイズで初期化
    /// </summary>
    /// <param name="n">要素数</param>
    public UnionFind(int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        Count = n;
        GroupCount = n;
        _parent = new int[n];
        _rank = new int[n];
        _size = new int[n];

        for (int i = 0; i < n; i++)
        {
            _parent[i] = i;
            _rank[i] = 0;
            _size[i] = 1;
        }
    }

    /// <summary>
    /// 指定した要素が属するグループの代表元を返す (経路圧縮付き)
    /// </summary>
    public int Find(int x)
    {
        ValidateIndex(x);

        if (_parent[x] != x)
        {
            _parent[x] = Find(_parent[x]); // 経路圧縮
        }
        return _parent[x];
    }

    /// <summary>
    /// 2つの要素を同じグループに統合
    /// </summary>
    /// <returns>統合が行われた場合は true</returns>
    public bool Union(int x, int y)
    {
        int rootX = Find(x);
        int rootY = Find(y);

        if (rootX == rootY)
            return false;

        // ランクによる統合
        if (_rank[rootX] < _rank[rootY])
        {
            _parent[rootX] = rootY;
            _size[rootY] += _size[rootX];
        }
        else if (_rank[rootX] > _rank[rootY])
        {
            _parent[rootY] = rootX;
            _size[rootX] += _size[rootY];
        }
        else
        {
            _parent[rootY] = rootX;
            _size[rootX] += _size[rootY];
            _rank[rootX]++;
        }

        GroupCount--;
        return true;
    }

    /// <summary>
    /// 2つの要素が同じグループに属するか判定
    /// </summary>
    public bool Connected(int x, int y)
    {
        return Find(x) == Find(y);
    }

    /// <summary>
    /// 指定した要素が属するグループのサイズを返す
    /// </summary>
    public int GetGroupSize(int x)
    {
        return _size[Find(x)];
    }

    /// <summary>
    /// 全グループをリストとして取得
    /// </summary>
    public List<List<int>> GetGroups()
    {
        var groups = new Dictionary<int, List<int>>();

        for (int i = 0; i < Count; i++)
        {
            int root = Find(i);
            if (!groups.TryGetValue(root, out var list))
            {
                list = new List<int>();
                groups[root] = list;
            }
            list.Add(i);
        }

        return new List<List<int>>(groups.Values);
    }

    private void ValidateIndex(int x)
    {
        if (x < 0 || x >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Index {x} is out of range [0, {Count})");
        }
    }
}
