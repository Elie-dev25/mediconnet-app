using Mediconnet_Backend.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mediconnet_Backend.Tests.Services;

public class DataProtectionServiceTests
{
    private static DataProtectionService CreateSut()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var provider = services.BuildServiceProvider();

        var dpProvider = provider.GetRequiredService<IDataProtectionProvider>();
        return new DataProtectionService(dpProvider, NullLogger<DataProtectionService>.Instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encrypt_EmptyInput_ReturnsSameValue(string? input)
    {
        var sut = CreateSut();

        sut.Encrypt(input!).Should().Be(input);
    }

    [Fact]
    public void Encrypt_RealString_ReturnsEncryptedWithPrefix()
    {
        var sut = CreateSut();

        var encrypted = sut.Encrypt("Hello World");

        encrypted.Should().StartWith("ENC:");
        encrypted.Should().NotBe("Hello World");
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
    {
        var sut = CreateSut();
        var original = "Patient info 42";

        var encrypted = sut.Encrypt(original);
        var decrypted = sut.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Decrypt_PlainText_ReturnsAsIs()
    {
        var sut = CreateSut();

        sut.Decrypt("not encrypted").Should().Be("not encrypted");
    }

    [Fact]
    public void EncryptMedicalData_DecryptMedicalData_RoundTrip()
    {
        var sut = CreateSut();
        var data = "Diagnostic confidentiel";

        var cipher = sut.EncryptMedicalData(data);
        var clear = sut.DecryptMedicalData(cipher);

        cipher.Should().StartWith("ENC:");
        clear.Should().Be(data);
    }

    [Fact]
    public void IsEncrypted_WithPrefix_ReturnsTrue()
    {
        CreateSut().IsEncrypted("ENC:payload").Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("plain text")]
    [InlineData("enc:lowercase")]
    public void IsEncrypted_WithoutPrefix_ReturnsFalse(string? text)
    {
        CreateSut().IsEncrypted(text!).Should().BeFalse();
    }

    [Fact]
    public void EncryptMedicalData_And_Decrypt_UseDifferentPurposes()
    {
        var sut = CreateSut();
        var original = "cross-purpose test";

        var medicalCipher = sut.EncryptMedicalData(original);

        // Using general Decrypt on medical cipher must fail (different purpose)
        var act = () => sut.Decrypt(medicalCipher);
        act.Should().Throw<InvalidOperationException>();
    }
}
