using Mediconnet_Backend.Helpers;

namespace Mediconnet_Backend.Tests.Helpers;

public class TimeSlotHelperTests
{
    // ==================== CalculerHeureFin ====================

    [Theory]
    [InlineData("08:00", 30, "08:30")]
    [InlineData("08:00", 60, "09:00")]
    [InlineData("08:30", 90, "10:00")]
    [InlineData("23:00", 120, "01:00")] // Passage minuit
    [InlineData("14:45", 15, "15:00")]
    [InlineData("00:00", 0, "00:00")]
    public void CalculerHeureFin_ReturnsCorrectEndTime(string heureDebut, int dureeMinutes, string expected)
    {
        TimeSlotHelper.CalculerHeureFin(heureDebut, dureeMinutes).Should().Be(expected);
    }

    [Fact]
    public void CalculerHeureFin_WithEmptyHeureDebut_ThrowsArgumentException()
    {
        var act = () => TimeSlotHelper.CalculerHeureFin("", 30);
        act.Should().Throw<ArgumentException>().WithParameterName("heureDebut");
    }

    [Fact]
    public void CalculerHeureFin_WithNullHeureDebut_ThrowsArgumentException()
    {
        var act = () => TimeSlotHelper.CalculerHeureFin(null!, 30);
        act.Should().Throw<ArgumentException>().WithParameterName("heureDebut");
    }

    [Fact]
    public void CalculerHeureFin_WithNegativeDuration_ThrowsArgumentException()
    {
        var act = () => TimeSlotHelper.CalculerHeureFin("08:00", -30);
        act.Should().Throw<ArgumentException>().WithParameterName("dureeMinutes");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("8:00:00")]
    [InlineData("8")]
    public void CalculerHeureFin_WithInvalidFormat_ThrowsArgumentException(string invalidHeure)
    {
        var act = () => TimeSlotHelper.CalculerHeureFin(invalidHeure, 30);
        act.Should().Throw<ArgumentException>().WithParameterName("heureDebut");
    }

    // ==================== HasOverlap ====================

    [Theory]
    [InlineData("08:00", "09:00", "08:30", "09:30", true)]  // Chevauchement partiel
    [InlineData("08:00", "10:00", "08:30", "09:30", true)]  // Inclus
    [InlineData("08:30", "09:30", "08:00", "10:00", true)]  // Contenu
    [InlineData("08:00", "09:00", "09:00", "10:00", false)] // Adjacent sans chevauchement
    [InlineData("09:00", "10:00", "08:00", "09:00", false)] // Adjacent sans chevauchement (inverse)
    [InlineData("08:00", "09:00", "10:00", "11:00", false)] // Pas de chevauchement
    [InlineData("10:00", "11:00", "08:00", "09:00", false)] // Pas de chevauchement (inverse)
    [InlineData("08:00", "09:00", "08:00", "09:00", true)]  // Identiques
    public void HasOverlap_ReturnsCorrectResult(string debut1, string fin1, string debut2, string fin2, bool expected)
    {
        TimeSlotHelper.HasOverlap(debut1, fin1, debut2, fin2).Should().Be(expected);
    }

    // ==================== IsInPast ====================

    [Fact]
    public void IsInPast_WithPastDate_ReturnsTrue()
    {
        var pastDate = DateTime.Now.AddDays(-1);
        TimeSlotHelper.IsInPast(pastDate, "10:00").Should().BeTrue();
    }

    [Fact]
    public void IsInPast_WithFutureDate_ReturnsFalse()
    {
        var futureDate = DateTime.Now.AddDays(1);
        TimeSlotHelper.IsInPast(futureDate, "10:00").Should().BeFalse();
    }

    [Fact]
    public void IsInPast_WithTodayAndPastTime_ReturnsTrue()
    {
        var today = DateTime.Now.Date;
        TimeSlotHelper.IsInPast(today, "00:00").Should().BeTrue();
    }

    [Fact]
    public void IsInPast_WithTodayAndFutureTime_ReturnsFalse()
    {
        var today = DateTime.Now.Date;
        TimeSlotHelper.IsInPast(today, "23:59").Should().BeFalse();
    }

    // ==================== IsCurrentlyActive ====================

    [Fact]
    public void IsCurrentlyActive_WithDifferentDate_ReturnsFalse()
    {
        var tomorrow = DateTime.Now.AddDays(1);
        TimeSlotHelper.IsCurrentlyActive(tomorrow, "00:00", "23:59").Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyActive_WithTodayAndActiveSlot_ReturnsTrue()
    {
        var today = DateTime.Now.Date;
        // Créneau qui couvre toute la journée
        TimeSlotHelper.IsCurrentlyActive(today, "00:00", "23:59").Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyActive_WithTodayAndPastSlot_ReturnsFalse()
    {
        var today = DateTime.Now.Date;
        TimeSlotHelper.IsCurrentlyActive(today, "00:00", "00:01").Should().BeFalse();
    }

    // ==================== ParseHeure ====================

    [Theory]
    [InlineData("08:00", 8, 0)]
    [InlineData("14:30", 14, 30)]
    [InlineData("00:00", 0, 0)]
    [InlineData("23:59", 23, 59)]
    public void ParseHeure_ReturnsCorrectTimeSpan(string heure, int expectedHours, int expectedMinutes)
    {
        var result = TimeSlotHelper.ParseHeure(heure);
        result.Hours.Should().Be(expectedHours);
        result.Minutes.Should().Be(expectedMinutes);
    }

    [Fact]
    public void ParseHeure_WithEmptyString_ThrowsArgumentException()
    {
        var act = () => TimeSlotHelper.ParseHeure("");
        act.Should().Throw<ArgumentException>().WithParameterName("heure");
    }

    [Fact]
    public void ParseHeure_WithNull_ThrowsArgumentException()
    {
        var act = () => TimeSlotHelper.ParseHeure(null!);
        act.Should().Throw<ArgumentException>().WithParameterName("heure");
    }

    // ==================== CalculerDureeMinutes ====================

    [Theory]
    [InlineData("08:00", "09:00", 60)]
    [InlineData("08:00", "08:30", 30)]
    [InlineData("14:00", "16:30", 150)]
    [InlineData("08:00", "08:00", 0)]
    public void CalculerDureeMinutes_ReturnsCorrectDuration(string heureDebut, string heureFin, int expected)
    {
        TimeSlotHelper.CalculerDureeMinutes(heureDebut, heureFin).Should().Be(expected);
    }

    [Fact]
    public void CalculerDureeMinutes_WithMidnightCrossing_ReturnsCorrectDuration()
    {
        // 23:00 à 01:00 = 2 heures = 120 minutes
        TimeSlotHelper.CalculerDureeMinutes("23:00", "01:00").Should().Be(120);
    }
}
