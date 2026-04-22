using Mediconnet_Backend.Core.Helpers;

namespace Mediconnet_Backend.Tests.Helpers;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        var result = new PagedResult<int>
        {
            TotalCount = 25,
            PageSize = 10
        };

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_WithExactDivision_CalculatesCorrectly()
    {
        var result = new PagedResult<int>
        {
            TotalCount = 20,
            PageSize = 10
        };

        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ReturnsFalse()
    {
        var result = new PagedResult<int> { Page = 1 };

        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ReturnsTrue()
    {
        var result = new PagedResult<int> { Page = 2 };

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ReturnsFalse()
    {
        var result = new PagedResult<int>
        {
            Page = 3,
            TotalCount = 25,
            PageSize = 10
        };

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_NotOnLastPage_ReturnsTrue()
    {
        var result = new PagedResult<int>
        {
            Page = 1,
            TotalCount = 25,
            PageSize = 10
        };

        result.HasNextPage.Should().BeTrue();
    }
}

public class PaginationParamsTests
{
    [Fact]
    public void Page_DefaultsToOne()
    {
        var pagination = new PaginationParams();

        pagination.Page.Should().Be(1);
    }

    [Fact]
    public void PageSize_DefaultsTo20()
    {
        var pagination = new PaginationParams();

        pagination.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-100, 1)]
    public void Page_LessThanOne_SetsToOne(int input, int expected)
    {
        var pagination = new PaginationParams { Page = input };

        pagination.Page.Should().Be(expected);
    }

    [Theory]
    [InlineData(101, 100)]
    [InlineData(200, 100)]
    [InlineData(1000, 100)]
    public void PageSize_GreaterThanMax_SetsToMax(int input, int expected)
    {
        var pagination = new PaginationParams { PageSize = input };

        pagination.PageSize.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    public void PageSize_LessThanOne_SetsToOne(int input, int expected)
    {
        var pagination = new PaginationParams { PageSize = input };

        pagination.PageSize.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(3, 10, 20)]
    [InlineData(1, 20, 0)]
    [InlineData(5, 20, 80)]
    public void Skip_CalculatesCorrectly(int page, int pageSize, int expectedSkip)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };

        pagination.Skip.Should().Be(expectedSkip);
    }
}

public class PaginationExtensionsTests
{
    [Fact]
    public void ApplyPagination_WithParams_AppliesCorrectly()
    {
        var items = Enumerable.Range(1, 100).AsQueryable();
        var pagination = new PaginationParams { Page = 2, PageSize = 10 };

        var result = items.ApplyPagination(pagination).ToList();

        result.Should().HaveCount(10);
        result.First().Should().Be(11);
        result.Last().Should().Be(20);
    }

    [Fact]
    public void ApplyPagination_WithDirectParams_AppliesCorrectly()
    {
        var items = Enumerable.Range(1, 100).AsQueryable();

        var result = items.ApplyPagination(3, 15).ToList();

        result.Should().HaveCount(15);
        result.First().Should().Be(31);
        result.Last().Should().Be(45);
    }

    [Fact]
    public void ToPagedResult_WithPaginationParams_CreatesCorrectResult()
    {
        var items = new List<string> { "a", "b", "c" };
        var pagination = new PaginationParams { Page = 1, PageSize = 10 };

        var result = items.ToPagedResult(30, pagination);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(30);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void ToPagedResult_WithDirectParams_CreatesCorrectResult()
    {
        var items = new List<int> { 1, 2, 3, 4, 5 };

        var result = items.ToPagedResult(50, 2, 5);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(50);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
    }
}
