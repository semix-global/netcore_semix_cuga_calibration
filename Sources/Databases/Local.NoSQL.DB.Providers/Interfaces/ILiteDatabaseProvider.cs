using LiteDB;

namespace Local.NoSQL.DB.Providers.Interfaces;

public interface ILiteDatabaseProvider
{
    bool ModifyLiteDatabase(string liteDbSource);

    LiteDatabase? GetLiteDatabase();

    void Dispose();
}