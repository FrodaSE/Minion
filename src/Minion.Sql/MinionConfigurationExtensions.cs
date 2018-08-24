using Minion.Core;

namespace Minion.Sql
{
    public static class MinionConfigurationExtensions
    {
        public static void UseSqlStorage(this MinionConfiguration configuration, string connectionString)
        {
            configuration.UseBatchStore(new SqlStorage(connectionString));
        }
    }
}