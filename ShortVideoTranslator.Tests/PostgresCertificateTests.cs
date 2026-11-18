using Npgsql;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class PostgresCertificateTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NoOverridePreservesExistingConnectionConfiguration(string? path)
    {
        const string original = "Host=127.0.0.1;Database=fixture;SSL Mode=Disable";
        Assert.Equal(original, PostgresConnectionFile.WithRootCertificate(original, path));
    }

    [Fact]
    public void CertificateOverridePreservesCredentialsAndEnforcesHostnameVerification()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = new NpgsqlConnectionStringBuilder
            {
                Host = "pooler.example.test", Port = 5432, Database = "fixture",
                Username = "postgres.fixture", Password = "test-only;quote'\"value",
                SslMode = SslMode.Disable, Timeout = 23
            };
            var configured = new NpgsqlConnectionStringBuilder(PostgresConnectionFile.WithRootCertificate(original.ConnectionString, path));
            Assert.Equal(original.Host, configured.Host);
            Assert.Equal(original.Port, configured.Port);
            Assert.Equal(original.Database, configured.Database);
            Assert.Equal(original.Username, configured.Username);
            Assert.Equal(original.Password, configured.Password);
            Assert.Equal(original.Timeout, configured.Timeout);
            Assert.Equal(SslMode.VerifyFull, configured.SslMode);
            Assert.Equal(Path.GetFullPath(path), configured.RootCertificate);
            Assert.Equal(SslMode.Disable, original.SslMode);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingCertificateReturnsSafeFileDiagnosticBeforeConnecting()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "ca.crt");
        var result = SupabaseReview.Run(() => PostgresConnectionFile.WithRootCertificate("Password=private-test-value", path));
        Assert.False(result.Success);
        Assert.Equal("FILE", result.Code);
        Assert.Equal("configuration", result.Stage);
        Assert.DoesNotContain("private-test-value", result.ToString());
        Assert.DoesNotContain(path, result.ToString());
    }
}
