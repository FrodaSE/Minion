using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Polly;

namespace Minion.Sql
{
    public static class PollySqlExtensions
    {
        private const int SqlRetryCount = 3;

        private const int SqlErrorConnectionBroken = 0;
        private const int SqlErrorInstanceDoesNotSupportEncryption = 20;
        private const int SqlErrorConnectedButLoginFailed = 64;
        private const int SqlErrorUnableToEstablishConnection = 233;
        private const int SqlErrorTransportLevelErrorReceivingResult = 10053;
        private const int SqlErrorTransportLevelErrorWhenSendingRequestToServer = 10054;
        private const int SqlErrorNetworkReleatedErrorDuringConnect = 10060;
        private const int SqlErrorDatabaseLimitReached = 10928;
        private const int SqlErrorResourceLimitReached = 10929;
        private const int SqlErrorServiceErrorEncountered = 40197;
        private const int SqlErrorServiceBusy = 40501;
        private const int SqlErrorServiceRequestProcessFail = 40540;
        private const int SqlErrorServiceExperiencingAProblem = 40545;
        private const int SqlErrorDatabaseUnavailable = 40613;
        private const int SqlErrorOperationInProgress = 40627;

        private static readonly Policy SqlRetryAsyncPolicy = Policy
            .Handle<TimeoutException>()
            .Or<SqlException>(AnyRetryableError)
            .WaitAndRetryAsync(SqlRetryCount, ExponentialBackoff);

        private static TimeSpan ExponentialBackoff(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }

        private static bool AnyRetryableError(SqlException exception)
        {
            return exception.Errors.OfType<SqlError>().Any(RetryableError);
        }

        private static bool RetryableError(SqlError error)
        {
            switch (error.Number)
            {
                case SqlErrorConnectionBroken:
                case SqlErrorOperationInProgress:
                case SqlErrorDatabaseUnavailable:
                case SqlErrorServiceExperiencingAProblem:
                case SqlErrorServiceRequestProcessFail:
                case SqlErrorServiceBusy:
                case SqlErrorServiceErrorEncountered:
                case SqlErrorResourceLimitReached:
                case SqlErrorDatabaseLimitReached:
                case SqlErrorNetworkReleatedErrorDuringConnect:
                case SqlErrorTransportLevelErrorWhenSendingRequestToServer:
                case SqlErrorTransportLevelErrorReceivingResult:
                case SqlErrorUnableToEstablishConnection:
                case SqlErrorConnectedButLoginFailed:
                case SqlErrorInstanceDoesNotSupportEncryption:
                    return true;

                default:
                    return false;
            }
        }

        public static Task<TResult> OpenWithRetryAsync<TResult>(this SqlConnection conn, Func<SqlConnection, Task<TResult>> process)
        {
            return SqlRetryAsyncPolicy.ExecuteAsync(async () =>
                {
                    await conn.OpenAsync();
                    return await process(conn);
                }
            );
        }
    }
}