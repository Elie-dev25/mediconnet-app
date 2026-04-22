using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Services;

namespace Mediconnet_Backend.Tests.Services;

public class PasswordValidationServiceTests
{
    private readonly PasswordValidationService _sut = new();

    // ==================== ValidatePassword ====================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidatePassword_EmptyOrNull_ReturnsInvalid(string? password)
    {
        var result = _sut.ValidatePassword(password!);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Le mot de passe est requis");
        result.StrengthLevel.Should().Be(PasswordStrength.Weak);
    }

    [Fact]
    public void ValidatePassword_TooShort_ReturnsInvalidWithLengthError()
    {
        var result = _sut.ValidatePassword("Ab1");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("au moins 8 caractères"));
    }

    [Fact]
    public void ValidatePassword_TooLong_ReturnsInvalid()
    {
        var password = new string('a', 129) + "A1";

        var result = _sut.ValidatePassword(password);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("dépasser 128"));
    }

    [Fact]
    public void ValidatePassword_MissingUppercase_ReturnsInvalid()
    {
        var result = _sut.ValidatePassword("abcdefg1");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Le mot de passe doit contenir au moins une majuscule");
        result.Criteria.HasUppercase.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_MissingLowercase_ReturnsInvalid()
    {
        var result = _sut.ValidatePassword("ABCDEFG1");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Le mot de passe doit contenir au moins une minuscule");
        result.Criteria.HasLowercase.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_MissingDigit_ReturnsInvalid()
    {
        var result = _sut.ValidatePassword("Abcdefgh");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Le mot de passe doit contenir au moins un chiffre");
        result.Criteria.HasDigit.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_AllRequirementsMet_ReturnsValid()
    {
        var result = _sut.ValidatePassword("Abcdefg1");

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Criteria.HasMinLength.Should().BeTrue();
        result.Criteria.HasUppercase.Should().BeTrue();
        result.Criteria.HasLowercase.Should().BeTrue();
        result.Criteria.HasDigit.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_StrongPassword_ReportsSpecialChar()
    {
        var result = _sut.ValidatePassword("Str0ng!Pass");

        result.IsValid.Should().BeTrue();
        result.Criteria.HasSpecialChar.Should().BeTrue();
    }

    // ==================== CalculateStrengthScore ====================

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    public void CalculateStrengthScore_EmptyOrNull_ReturnsZero(string? password, int expected)
    {
        _sut.CalculateStrengthScore(password!).Should().Be(expected);
    }

    [Fact]
    public void CalculateStrengthScore_ShortPassword_ReturnsLowScore()
    {
        var score = _sut.CalculateStrengthScore("abc");

        score.Should().BeLessThan(50);
    }

    [Fact]
    public void CalculateStrengthScore_ComplexPassword_ReturnsHighScore()
    {
        var score = _sut.CalculateStrengthScore("MyStr0ng!Password#2024");

        score.Should().BeGreaterOrEqualTo(70);
    }

    [Fact]
    public void CalculateStrengthScore_ClampedBetween0And100()
    {
        var score = _sut.CalculateStrengthScore("A1b!" + new string('x', 200));

        score.Should().BeInRange(0, 100);
    }

    // ==================== GetStrengthLevel ====================

    [Fact]
    public void GetStrengthLevel_WeakPassword_ReturnsWeak()
    {
        _sut.GetStrengthLevel("abc").Should().Be(PasswordStrength.Weak);
    }

    [Fact]
    public void GetStrengthLevel_MediumPassword_ReturnsMediumOrStronger()
    {
        var level = _sut.GetStrengthLevel("Abcdefg1");

        level.Should().BeOneOf(PasswordStrength.Medium, PasswordStrength.Strong);
    }

    [Fact]
    public void GetStrengthLevel_StrongPassword_ReturnsStrong()
    {
        _sut.GetStrengthLevel("MyStr0ng!Password#2024").Should().Be(PasswordStrength.Strong);
    }

    [Fact]
    public void GetStrengthLevel_NoSpecialCharButValid_IsAtMostMedium()
    {
        _sut.GetStrengthLevel("Abcdefgh1234").Should().NotBe(PasswordStrength.Strong);
    }
}
