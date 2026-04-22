using Mediconnet_Backend.DTOs.Auth;
using Mediconnet_Backend.Validators;

namespace Mediconnet_Backend.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void Validate_ValidEmailIdentifier_IsValid()
    {
        var req = new LoginRequest { Identifier = "user@example.com", Password = "secret1" };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidPhoneIdentifier_IsValid()
    {
        var req = new LoginRequest { Identifier = "+237612345678", Password = "secret1" };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyIdentifier_IsInvalid(string identifier)
    {
        var req = new LoginRequest { Identifier = identifier, Password = "secret1" };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Identifier));
    }

    [Fact]
    public void Validate_IdentifierTooLong_IsInvalid()
    {
        var req = new LoginRequest { Identifier = new string('a', 121), Password = "secret1" };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("user<script>@x.com")]
    [InlineData("user--drop@x.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("u/*comment*/@x.com")]
    public void Validate_SuspiciousIdentifier_IsInvalid(string identifier)
    {
        var req = new LoginRequest { Identifier = identifier, Password = "secret1" };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("invalide"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("12345")]
    public void Validate_PasswordTooShort_IsInvalid(string password)
    {
        var req = new LoginRequest { Identifier = "user@x.com", Password = password };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void Validate_PasswordTooLong_IsInvalid()
    {
        var req = new LoginRequest { Identifier = "user@x.com", Password = new string('a', 101) };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeFalse();
    }
}

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    private static RegisterRequest ValidRequest() => new()
    {
        FirstName = "Jean",
        LastName = "Dupont",
        Email = "jean@example.com",
        Telephone = "+237612345678",
        Password = "Secret12",
        ConfirmPassword = "Secret12"
    };

    [Fact]
    public void Validate_FullyValid_IsValid()
    {
        _sut.Validate(ValidRequest()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("J@hn")]
    [InlineData("123")]
    public void Validate_InvalidFirstName_IsInvalid(string firstName)
    {
        var req = ValidRequest();
        req.FirstName = firstName;

        _sut.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_FirstNameWithAccentedLetters_IsValid()
    {
        var req = ValidRequest();
        req.FirstName = "Éléonore-Marie";

        _sut.Validate(req).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@")]
    public void Validate_InvalidEmail_IsInvalid(string email)
    {
        var req = ValidRequest();
        req.Email = email;

        _sut.Validate(req).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase")]
    [InlineData("ALLUPPERCASE")]
    [InlineData("NoDigitsHere")]
    public void Validate_WeakPassword_IsInvalid(string password)
    {
        var req = ValidRequest();
        req.Password = password;
        req.ConfirmPassword = password;

        _sut.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MismatchedConfirmPassword_IsInvalid()
    {
        var req = ValidRequest();
        req.ConfirmPassword = "Different1";

        var result = _sut.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.ConfirmPassword));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("letters")]
    public void Validate_InvalidPhone_IsInvalid(string phone)
    {
        var req = ValidRequest();
        req.Telephone = phone;

        _sut.Validate(req).IsValid.Should().BeFalse();
    }
}

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _sut = new();

    [Fact]
    public void Validate_FullyValid_IsValid()
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1",
            NewPassword = "NewPass2",
            ConfirmNewPassword = "NewPass2"
        };

        _sut.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NewPasswordEqualsCurrent_IsInvalid()
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "SamePass1",
            NewPassword = "SamePass1",
            ConfirmNewPassword = "SamePass1"
        };

        var result = _sut.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("différent"));
    }

    [Fact]
    public void Validate_ConfirmMismatch_IsInvalid()
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1",
            NewPassword = "NewPass2",
            ConfirmNewPassword = "Different2"
        };

        _sut.Validate(req).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase")]
    [InlineData("NOUPPERCASEONLY")]
    [InlineData("NoDigitsAtAll")]
    public void Validate_WeakNewPassword_IsInvalid(string weak)
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1",
            NewPassword = weak,
            ConfirmNewPassword = weak
        };

        _sut.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MissingCurrentPassword_IsInvalid()
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "",
            NewPassword = "NewPass2",
            ConfirmNewPassword = "NewPass2"
        };

        _sut.Validate(req).IsValid.Should().BeFalse();
    }
}
