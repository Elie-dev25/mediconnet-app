using Mediconnet_Backend.Helpers;

namespace Mediconnet_Backend.Tests.Helpers;

public class DateTimeHelperTests
{
    [Fact]
    public void Now_IsApproximatelyUtcPlusOneHour()
    {
        var utcNow = DateTime.UtcNow;
        var cameroon = DateTimeHelper.Now;

        var diff = cameroon - utcNow;
        diff.TotalHours.Should().BeApproximately(1.0, 0.05);
    }

    [Fact]
    public void Today_HasNoTimeComponent()
    {
        var today = DateTimeHelper.Today;

        today.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void FromUtc_AddsOneHour()
    {
        var utc = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);

        var cameroon = DateTimeHelper.FromUtc(utc);

        cameroon.Hour.Should().Be(11);
        cameroon.Minute.Should().Be(0);
    }

    [Fact]
    public void ToUtc_SubtractsOneHour()
    {
        var cameroonLocal = new DateTime(2026, 4, 21, 11, 0, 0);

        var utc = DateTimeHelper.ToUtc(cameroonLocal);

        utc.Hour.Should().Be(10);
        utc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void FromUtc_ThenToUtc_IsIdempotent()
    {
        var utc = new DateTime(2026, 6, 15, 14, 30, 0, DateTimeKind.Utc);

        var roundTrip = DateTimeHelper.ToUtc(DateTimeHelper.FromUtc(utc));

        roundTrip.Should().Be(utc);
    }

    [Fact]
    public void IsSlotPassed_PastSlot_ReturnsTrue()
    {
        var past = DateTimeHelper.Now.AddHours(-1);

        DateTimeHelper.IsSlotPassed(past).Should().BeTrue();
    }

    [Fact]
    public void IsSlotPassed_FutureSlot_ReturnsFalse()
    {
        var future = DateTimeHelper.Now.AddHours(1);

        DateTimeHelper.IsSlotPassed(future).Should().BeFalse();
    }

    [Fact]
    public void IsSlotPassed_CurrentMinute_ReturnsFalse()
    {
        var now = DateTimeHelper.Now;
        var sameMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        DateTimeHelper.IsSlotPassed(sameMinute).Should().BeFalse();
    }

    [Fact]
    public void IsFuture_IsInverseOfIsSlotPassed()
    {
        var future = DateTimeHelper.Now.AddMinutes(10);
        var past = DateTimeHelper.Now.AddMinutes(-10);

        DateTimeHelper.IsFuture(future).Should().BeTrue();
        DateTimeHelper.IsFuture(past).Should().BeFalse();
    }

    [Fact]
    public void IsToday_SameDate_ReturnsTrue()
    {
        DateTimeHelper.IsToday(DateTimeHelper.Today).Should().BeTrue();
        DateTimeHelper.IsToday(DateTimeHelper.Today.AddHours(15)).Should().BeTrue();
    }

    [Fact]
    public void IsToday_OtherDate_ReturnsFalse()
    {
        DateTimeHelper.IsToday(DateTimeHelper.Today.AddDays(1)).Should().BeFalse();
        DateTimeHelper.IsToday(DateTimeHelper.Today.AddDays(-1)).Should().BeFalse();
    }
}
