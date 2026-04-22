using Mediconnet_Backend.DTOs.Patient;
using Mediconnet_Backend.Validators;

namespace Mediconnet_Backend.Tests.Validators;

public class UpdatePatientProfileRequestValidatorTests
{
    private readonly UpdatePatientProfileRequestValidator _sut = new();

    [Fact]
    public void Validate_EmptyRequest_IsValid()
    {
        _sut.Validate(new UpdatePatientProfileRequest()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("M")]
    [InlineData("F")]
    [InlineData("Masculin")]
    [InlineData("Féminin")]
    [InlineData("Autre")]
    [InlineData("masculin")]
    public void Validate_ValidSexe_IsValid(string sexe)
    {
        _sut.Validate(new UpdatePatientProfileRequest { Sexe = sexe }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("X")]
    [InlineData("123")]
    [InlineData("garcon")]
    public void Validate_InvalidSexe_IsInvalid(string sexe)
    {
        _sut.Validate(new UpdatePatientProfileRequest { Sexe = sexe }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("A+")]
    [InlineData("O-")]
    [InlineData("AB+")]
    [InlineData("b-")]
    public void Validate_ValidGroupeSanguin_IsValid(string groupe)
    {
        _sut.Validate(new UpdatePatientProfileRequest { GroupeSanguin = groupe }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("X+")]
    [InlineData("Z")]
    [InlineData("A++")]
    public void Validate_InvalidGroupeSanguin_IsInvalid(string groupe)
    {
        _sut.Validate(new UpdatePatientProfileRequest { GroupeSanguin = groupe }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_FuturBirthDate_IsInvalid()
    {
        var req = new UpdatePatientProfileRequest { Naissance = DateTime.Now.AddYears(1) };

        _sut.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_AncientBirthDate_IsInvalid()
    {
        var req = new UpdatePatientProfileRequest { Naissance = DateTime.Now.AddYears(-200) };

        _sut.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ReasonableBirthDate_IsValid()
    {
        var req = new UpdatePatientProfileRequest { Naissance = new DateTime(1990, 1, 1) };

        _sut.Validate(req).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public void Validate_InvalidNbEnfants_IsInvalid(int nb)
    {
        _sut.Validate(new UpdatePatientProfileRequest { NbEnfants = nb }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(50)]
    public void Validate_ValidNbEnfants_IsValid(int nb)
    {
        _sut.Validate(new UpdatePatientProfileRequest { NbEnfants = nb }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("letters")]
    [InlineData("123abc")]
    public void Validate_InvalidTelephone_IsInvalid(string tel)
    {
        _sut.Validate(new UpdatePatientProfileRequest { Telephone = tel }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("+237612345678")]
    [InlineData("06 12 34 56 78")]
    [InlineData("(237) 612-345")]
    public void Validate_ValidTelephone_IsValid(string tel)
    {
        _sut.Validate(new UpdatePatientProfileRequest { Telephone = tel }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TelephoneTooLong_IsInvalid()
    {
        _sut.Validate(new UpdatePatientProfileRequest { Telephone = new string('1', 21) }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_AdresseTooLong_IsInvalid()
    {
        _sut.Validate(new UpdatePatientProfileRequest { Adresse = new string('a', 501) }).IsValid.Should().BeFalse();
    }
}

public class PatientSearchRequestValidatorTests
{
    private readonly PatientSearchRequestValidator _sut = new();

    [Fact]
    public void Validate_ValidTerm_IsValid()
    {
        var req = new PatientSearchRequest { SearchTerm = "Dupont", Limit = 10 };

        _sut.Validate(req).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public void Validate_TermTooShort_IsInvalid(string term)
    {
        _sut.Validate(new PatientSearchRequest { SearchTerm = term, Limit = 10 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TermTooLong_IsInvalid()
    {
        _sut.Validate(new PatientSearchRequest { SearchTerm = new string('x', 101), Limit = 10 }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("name-- drop table")]
    [InlineData("a/*comment*/")]
    [InlineData("exec(bad)")]
    [InlineData("char(65)")]
    public void Validate_SqlInjectionAttempts_AreRejected(string term)
    {
        _sut.Validate(new PatientSearchRequest { SearchTerm = term, Limit = 10 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_LimitOver100_IsInvalid()
    {
        _sut.Validate(new PatientSearchRequest { SearchTerm = "valid", Limit = 101 }).IsValid.Should().BeFalse();
    }
}
