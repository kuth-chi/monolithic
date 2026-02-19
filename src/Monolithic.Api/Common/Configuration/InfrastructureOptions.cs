namespace Monolithic.Api.Common.Configuration;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    public PostgresOptions PostgreSql { get; init; } = new();

    public SqliteOptions SQLite { get; init; } = new();

    public RedisOptions Redis { get; init; } = new();

    public RabbitMqOptions RabbitMq { get; init; } = new();
}

public sealed class PostgresOptions
{
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class SqliteOptions
{
    public string ConnectionString { get; init; } = "Data Source=monolithic_dev.db";
}

public sealed class RedisOptions
{
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string Username { get; init; } = "guest";

    public string Password { get; init; } = "guest";
}