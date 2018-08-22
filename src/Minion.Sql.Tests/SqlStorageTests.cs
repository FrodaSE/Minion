using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Minion;
using Minion.Tests;
using Xunit;

namespace Minion.Sql.Tests
{
    [NCrunch.Framework.ExclusivelyUses("sql")]
    [Collection("sql")]
    [Trait("Category", "Sql Storage Tests")]
    public class SqlStorageTests : StoreTests, IDisposable
    {
        private readonly string _conn;
        private readonly string _dbName;

        public SqlStorageTests()
        {
            _dbName = $"minion_test_db_{Guid.NewGuid().ToString().Replace("-", "")}";
            _conn =
                "Server=.\\SQLExpress;Trusted_Connection=True;MultipleActiveResultSets=true;Integrated Security=SSPI;Pooling=false;";

            using (var connection = new SqlConnection(_conn))
            {
                connection.Execute("CREATE Database " + _dbName);
            }

            Store = new SqlStorage(DateService, $"Server=.\\SQLExpress;Database={_dbName};Trusted_Connection=True;MultipleActiveResultSets=true;Integrated Security=SSPI;Pooling=false;");

            Task.WaitAll(Store.InitAsync());
        }

        public void Dispose()
        {
            using (var connection = new SqlConnection(_conn))
            {
                connection.Execute("DROP Database " + _dbName);
            }
        }
    }
}
