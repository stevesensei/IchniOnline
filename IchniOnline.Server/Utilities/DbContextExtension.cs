using Npgsql;
using SqlSugar;

namespace IchniOnline.Server.Utilities;

public static class DbContextExtension
{
    public static IServiceCollection AddDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName = "platformAdminConnStr"
    )
    {
        var connectionString =
            configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"缺少连接字符串：{connectionName}");
        services.AddScoped<ISqlSugarClient>(serviceProvider =>
        {
            // 优先复用 Program 中注册的 NpgsqlDataSource，继承 Aspire 官方 tracing / metrics
            var dataSource = serviceProvider.GetService<NpgsqlDataSource>();
            Func<NpgsqlConnection> connectionFactory =
                dataSource != null
                    ? dataSource.CreateConnection
                    : () => new NpgsqlConnection(connectionString);
            return new SqlSugarClient(
                new ConnectionConfig
                {
                    DbType = SqlSugar.DbType.PostgreSQL,
                    ConnectionString = connectionString,
                    IsAutoCloseConnection = true,
                },
                db =>
                {
                    // 让 SqlSugar 使用 DataSource 创建的连接，便于接入 Aspire.Npgsql 的探测能力
                    db.Ado.Connection ??= connectionFactory();
                    var logger = serviceProvider.GetRequiredService<ILogger<SqlSugarClient>>();
                    db.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        var formattedSql = UtilMethods.GetNativeSql(sql, pars);
                        var parameters =
                            pars?.Length > 0
                                ? string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}"))
                                : "none";
                        logger.LogInformation(
                            "SqlSugar SQL: {Sql}; Params: {Params}",
                            formattedSql,
                            parameters
                        );
                    };
                    db.Aop.OnError = exp => { logger.LogError(exp, "SqlSugar 执行错误"); };
                    db.Aop.OnLogExecuted = (_, _) => { logger.LogDebug("SqlSugar SQL 执行完成"); };
                }
            );
        });
        return services;
    }
}