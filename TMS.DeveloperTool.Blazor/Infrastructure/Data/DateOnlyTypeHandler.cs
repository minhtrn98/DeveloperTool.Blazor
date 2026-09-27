using System.Data;
using Dapper;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Data;

/// <summary>
/// Lets Dapper read PostgreSQL <c>date</c> columns into <see cref="DateOnly"/> properties
/// (Npgsql hands them to Dapper as <see cref="DateTime"/>). Registered once in
/// <c>DatabaseServiceExtensions.AddDatabaseServices</c>.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value;
    }
}
