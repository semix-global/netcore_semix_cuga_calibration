namespace Local.NoSQL.DB.Providers.Interfaces;

public interface ICacheItem
{
    long Id { get; set; }

    long Expiration { get; set; }

    DateTime CreatedTime { get; set; }

    bool IsDeleted { get; set; }
}