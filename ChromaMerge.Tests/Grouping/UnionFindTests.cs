using Xunit;
using FluentAssertions;
using ChromaMerge.Models.Grouping;

namespace ChromaMerge.Tests.Grouping;

public class UnionFindTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCorrectSize()
    {
        var uf = new UnionFind(5);

        uf.Count.Should().Be(5);
        uf.GroupCount.Should().Be(5); // 初期状態では全て別々のグループ
    }

    [Fact]
    public void Constructor_ZeroSize_ShouldWork()
    {
        var uf = new UnionFind(0);

        uf.Count.Should().Be(0);
        uf.GroupCount.Should().Be(0);
        uf.GetGroups().Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NegativeSize_ShouldThrow()
    {
        var act = () => new UnionFind(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_LargeSize_ShouldWork()
    {
        var uf = new UnionFind(10000);

        uf.Count.Should().Be(10000);
        uf.GroupCount.Should().Be(10000);
    }

    [Fact]
    public void Find_InitialState_ShouldReturnOwnIndex()
    {
        var uf = new UnionFind(5);

        for (int i = 0; i < 5; i++)
        {
            uf.Find(i).Should().Be(i);
        }
    }

    [Fact]
    public void Union_TwoElements_ShouldMergeThem()
    {
        var uf = new UnionFind(5);

        uf.Union(0, 1);

        uf.Find(0).Should().Be(uf.Find(1));
        uf.GroupCount.Should().Be(4);
    }

    [Fact]
    public void Union_SameElement_ShouldNotChangeGroupCount()
    {
        var uf = new UnionFind(5);

        uf.Union(0, 0);

        uf.GroupCount.Should().Be(5);
    }

    [Fact]
    public void Union_AlreadyConnected_ShouldNotChangeGroupCount()
    {
        var uf = new UnionFind(5);

        uf.Union(0, 1);
        var countAfterFirst = uf.GroupCount;

        uf.Union(0, 1);

        uf.GroupCount.Should().Be(countAfterFirst);
    }

    [Fact]
    public void Connected_SameElement_ShouldReturnTrue()
    {
        var uf = new UnionFind(5);

        uf.Connected(0, 0).Should().BeTrue();
    }

    [Fact]
    public void Connected_UnionedElements_ShouldReturnTrue()
    {
        var uf = new UnionFind(5);

        uf.Union(0, 1);
        uf.Union(1, 2);

        uf.Connected(0, 2).Should().BeTrue();
    }

    [Fact]
    public void Connected_NotUnionedElements_ShouldReturnFalse()
    {
        var uf = new UnionFind(5);

        uf.Union(0, 1);

        uf.Connected(0, 2).Should().BeFalse();
    }

    [Fact]
    public void GetGroups_InitialState_ShouldReturnIndividualGroups()
    {
        var uf = new UnionFind(3);

        var groups = uf.GetGroups();

        groups.Should().HaveCount(3);
        groups.Should().AllSatisfy(g => g.Should().HaveCount(1));
    }

    [Fact]
    public void GetGroups_AfterUnion_ShouldReturnMergedGroups()
    {
        var uf = new UnionFind(5);

        uf.Union(0, 1);
        uf.Union(2, 3);
        uf.Union(3, 4);

        var groups = uf.GetGroups();

        groups.Should().HaveCount(2);
        groups.Should().Contain(g => g.Count == 2); // {0, 1}
        groups.Should().Contain(g => g.Count == 3); // {2, 3, 4}
    }

    [Fact]
    public void GetGroupSize_ShouldReturnCorrectSize()
    {
        var uf = new UnionFind(5);

        uf.Union(0, 1);
        uf.Union(0, 2);

        uf.GetGroupSize(0).Should().Be(3);
        uf.GetGroupSize(1).Should().Be(3);
        uf.GetGroupSize(2).Should().Be(3);
        uf.GetGroupSize(3).Should().Be(1);
        uf.GetGroupSize(4).Should().Be(1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Find_OutOfRange_ShouldThrow(int index)
    {
        var uf = new UnionFind(5);

        var act = () => uf.Find(index);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PathCompression_ShouldOptimizeTree()
    {
        var uf = new UnionFind(10);

        // 線形につなげる
        for (int i = 0; i < 9; i++)
        {
            uf.Union(i, i + 1);
        }

        // Find で経路圧縮が働く
        var root = uf.Find(0);

        // 全ての要素が同じルートを持つ
        for (int i = 0; i < 10; i++)
        {
            uf.Find(i).Should().Be(root);
        }

        uf.GroupCount.Should().Be(1);
    }
}
