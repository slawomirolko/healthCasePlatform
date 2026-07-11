using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Common;
using HealthCasePlatform.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HealthCasePlatform.Infrastructure.Persistence.Mongo;

public static class AuditEntryClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(AuditEntry)))
        {
            return;
        }

        BsonSerializer.TryRegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));

        if (!BsonClassMap.IsClassMapRegistered(typeof(Entity)))
        {
            BsonClassMap.RegisterClassMap<Entity>(cm =>
            {
                cm.MapIdMember(x => x.Id);
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(AuditEntry)))
        {
            BsonClassMap.RegisterClassMap<AuditEntry>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(x => x.CaseId);
                cm.MapMember(x => x.Action).SetSerializer(new EnumSerializer<AuditAction>(BsonType.Int32));
                cm.MapMember(x => x.Actor);
                cm.MapMember(x => x.Detail);
                cm.MapMember(x => x.OccurredAt);
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
