using Microsoft.EntityFrameworkCore;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Tests.TestSupport;

namespace BaseCleanArchitecture.Tests.Persistence;

public class AuditLogModelTests
{
    [Fact]
    public void AuditLog_Maps_To_Audit_Schema()
    {
        using var context = TestDbContextFactory.Create();

        var entityType = context.Model.FindEntityType(typeof(AuditLog));

        Assert.NotNull(entityType);
        Assert.Equal("audit", entityType.GetSchema());
        Assert.Equal("AuditLogs", entityType.GetTableName());
        Assert.Equal(typeof(string), entityType.FindProperty(nameof(AuditLog.AuditType))!.GetProviderClrType());
    }
}
