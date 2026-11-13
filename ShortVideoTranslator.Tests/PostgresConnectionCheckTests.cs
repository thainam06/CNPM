using System.Security.Authentication;
using Npgsql;
using ShortVideoTranslator.Api.Services;
namespace ShortVideoTranslator.Tests;
public sealed class PostgresConnectionCheckTests
{
    [Fact]
    public void DistinguishesTlsAndConnectivityWithoutLeakingExceptionValues()
    {
        var secret = "Password=must-never-be-returned";
        var tls = PostgresConnectionCheck.Failure(new NpgsqlException(secret, new AuthenticationException(secret)));
        Assert.Equal("TLS", tls.Code); Assert.DoesNotContain(secret, tls.ToString());
        Assert.Equal("CONNECT", PostgresConnectionCheck.Failure(new NpgsqlException(secret)).Code);
        Assert.Equal("FILE", PostgresConnectionCheck.Failure(new FileNotFoundException(secret)).Code);
        Assert.Equal("DATA", PostgresConnectionCheck.Failure(new InvalidDataException(secret)).Code);
    }
    [Theory]
    [InlineData("28P01", "AUTH")]
    [InlineData("42P01", "SCHEMA")]
    [InlineData("42501", "PERMISSION")]
    [InlineData("25006", "READ_ONLY")]
    [InlineData("42703", "SCHEMA")]
    [InlineData("57P03", "UNAVAILABLE")]
    [InlineData("XX000", "DATABASE")]
    public void SqlErrorsAreClassifiedWithoutRowData(string sqlState, string expected)
    {
        var result = PostgresConnectionCheck.Failure(new PostgresException("private-row", "ERROR", "ERROR", sqlState));
        Assert.False(result.Ready); Assert.Equal(expected, result.Code); Assert.Equal(sqlState, result.SqlState); Assert.DoesNotContain("private-row", result.ToString());
    }
    [Theory]
    [InlineData("Tenant or user not found: Password=private", "POOLER_PROJECT")]
    [InlineData("MaxClientsInSessionMode: Password=private", "CAPACITY")]
    [InlineData("Circuit breaker open: Password=private", "POOLER_BLOCKED")]
    public void PoolerErrorReportsFixedHintAndSqlStateOnly(string message, string expected)
    {
        var result = PostgresConnectionCheck.Failure(new NpgsqlException("private-wrapper",
            new PostgresException(message, "ERROR", "ERROR", "XX000")));
        Assert.Equal(expected, result.Code); Assert.Equal("XX000", result.SqlState);
        Assert.DoesNotContain("private", result.ToString());
    }
    [Fact]
    public void ConfigurationFailureIsSafeAndDoesNotAttemptBootstrap()
    {
        var result = PostgresConnectionCheck.Check(() => throw new InvalidOperationException("Password=secret"));
        Assert.Equal("configuration", result.Stage); Assert.Equal("CONFIG", result.Code); Assert.False(result.Ready); Assert.DoesNotContain("secret", result.ToString());
    }
}
