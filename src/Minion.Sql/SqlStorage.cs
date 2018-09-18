using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using Newtonsoft.Json;

namespace Minion.Sql
{
    public class SqlStorage : IBatchStore
    {
        private readonly IDateService _dateService;
        private readonly string _connectionString;

        public SqlStorage(string connectionString)
        {
            _dateService = MinionConfiguration.Configuration.DateService;
            _connectionString = connectionString;
        }

        public SqlStorage(IDateService dateService, string connectionString)
        {
            _dateService = dateService;
            _connectionString = connectionString;
        }

        public async Task InitAsync()
        {
            const string sql = @"
                IF object_id('Jobs') is null
                BEGIN
                    CREATE TABLE Jobs (
                        _id             int                 NOT NULL IDENTITY (1, 1),
                        Id              uniqueidentifier    NOT NULL PRIMARY KEY,
                        Type            nvarchar(MAX)       NOT NULL,
                        DueTime         DATETIME            NOT NULL,
                        Priority        int                 NOT NULL,
                        WaitCount       int                 NOT NULL,
                        PrevId          uniqueidentifier    NULL,
                        NextId          uniqueidentifier    NULL,
                        BatchId         uniqueidentifier    NULL,
                        CreatedTime     DATETIME            NOT NULL,
                        UpdatedTime     DATETIME            NOT NULL,
                        InputType       nvarchar(MAX)       NULL,
                        InputData       nvarchar(MAX)       NULL,
                        State           int                 NOT NULL,
                        StatusInfo      nvarchar(MAX)       NULL
                    )
                END";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenWithRetryAsync(conn => conn.ExecuteAsync(sql));
            }
        }

        public Task HeartBeatAsync(string machineName, int numberOfTasks, int pollingFrequency, int heartBeatFrequency)
        {
            return Task.FromResult(false);
        }

        public async Task<JobDescription> AcquireJobAsync()
        {
            var now = GetSqlSafeDate(_dateService.GetNow());

            const string sql = @"
                BEGIN TRAN

                    Update job WITH (TABLOCK, HOLDLOCK) 
                        set 
                            job.State = @NewState, 
                            UpdatedTime = @Now
                  
                    output 
                        inserted.Id, 
                        inserted.Type, 
                        inserted.DueTime, 
                        inserted.Priority, 
                        inserted.WaitCount, 
                        inserted.PrevId, 
                        inserted.NextId, 
                        inserted.BatchId, 
                        inserted.CreatedTime, 
                        inserted.UpdatedTime, 
                        inserted.InputType, 
                        inserted.InputData, 
                        inserted.State, 
                        inserted.StatusInfo
                    
                    from Jobs as job WITH (TABLOCK, HOLDLOCK)
                        inner join (
		                    select 
			                    top 1 Id
		                    from
			                    Jobs WITH (TABLOCK, HOLDLOCK)
		                    where
			                    State = @State and
			                    WaitCount = 0 and
                                DueTime <= @Now
		                    order by
			                    DueTime asc,
			                    Priority desc,
                                _id asc
	                    ) as job_top on job_top.id = job.Id

                COMMIT";

            IEnumerable<JobDescriptionSqlModel> rows;

            using (var connection = new SqlConnection(_connectionString))
            {
                rows = await connection.OpenWithRetryAsync(conn =>
                    conn.QueryAsync<JobDescriptionSqlModel>(sql,
                        new {
                            State = (int) ExecutionState.Waiting,
                            Now = now,
                            NewState = (int) ExecutionState.Running
                        }
                    )
                );
            }

            var result = rows.FirstOrDefault();

            if (result == null)
                return null;

            var jobDescription = new JobDescription
            {
                Id = result.Id,
                Type = result.Type,
                DueTime = result.DueTime,
                Priority = result.Priority,
                WaitCount = result.WaitCount,
                PrevId = result.PrevId,
                NextId = result.NextId,
                BatchId = result.BatchId,
                CreatedTime = result.CreatedTime,
                UpdatedTime = result.UpdatedTime,
                State = result.State,
                StatusInfo = result.StatusInfo
            };

            if (!string.IsNullOrEmpty(result.InputData))
                jobDescription.Input = new JobInputDescription
                {
                    Type = result.InputType,
                    InputData = JsonConvert.DeserializeObject(result.InputData, Type.GetType(result.InputType))
                };

            return jobDescription;


        }

        public async Task ReleaseJobAsync(Guid jobId, JobResult result)
        {
            const string sql = @"
                BEGIN TRAN

                    UPDATE Jobs WITH (TABLOCK, HOLDLOCK)
                        SET 
                            State = @State, 
                            StatusInfo = @StatusInfo, 
                            DueTime = @DueTime

                    WHERE 
                        Id = @Id

                    IF @State = @FinishedState
                        UPDATE Jobs WITH (TABLOCK, HOLDLOCK)
                            SET 
                                WaitCount = WaitCount - 1 
                            WHERE 
                                PrevId = @Id or 
                                Id = (
                                    select 
                                        top 1 NextId 
                                    From 
                                        Jobs WITH (TABLOCK, HOLDLOCK)
                                    Where 
                                        Id = @Id
                                )

                COMMIT";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenWithRetryAsync(conn =>
                    conn.ExecuteAsync(sql,
                        new
                        {
                            State = (int) result.State,
                            StatusInfo = result.StatusInfo,
                            DueTime = GetSqlSafeDate(result.DueTime),
                            FinishedState = (int) ExecutionState.Finished,
                            Id = jobId
                        })
                );
            }
        }

        public async Task AddJobsAsync(IEnumerable<JobDescription> jobs)
        {
            var jobDescriptions = new List<JobDescriptionSqlModel>();

            foreach (var job in jobs)
            {
                var jobDescription = new JobDescriptionSqlModel
                {
                    Id = job.Id,
                    Type = job.Type,
                    DueTime = job.DueTime,
                    Priority = job.Priority,
                    WaitCount = job.WaitCount,
                    PrevId = job.PrevId,
                    NextId = job.NextId,
                    BatchId = job.BatchId,
                    CreatedTime = job.CreatedTime,
                    UpdatedTime = job.UpdatedTime,
                    State = job.State,
                    StatusInfo = job.StatusInfo,
                };

                if (job.Input != null)
                {
                    jobDescription.InputType = job.Input.Type;
                    jobDescription.InputData = JsonConvert.SerializeObject(job.Input.InputData);
                }

                jobDescription.CreatedTime = GetSqlSafeDate(jobDescription.CreatedTime);
                jobDescription.UpdatedTime = GetSqlSafeDate(jobDescription.UpdatedTime);
                jobDescription.DueTime = GetSqlSafeDate(jobDescription.DueTime);

                jobDescriptions.Add(jobDescription);
            }

            var sql = @"
                BEGIN TRAN

                    INSERT INTO Jobs WITH (TABLOCK, HOLDLOCK)

                        (
                            Id, 
                            Type,  
                            DueTime,  
                            Priority,  
                            WaitCount,  
                            PrevId,  
                            NextId,  
                            BatchId,  
                            CreatedTime,  
                            UpdatedTime,  
                            InputType,  
                            InputData,  
                            State,  
                            StatusInfo
                         ) 

                    Values 

                        ( 
                            @Id,  
                            @Type,  
                            @DueTime,  
                            @Priority,  
                            @WaitCount,  
                            @PrevId,  
                            @NextId,  
                            @BatchId,  
                            @CreatedTime,  
                            @UpdatedTime,  
                            @InputType,  
                            @InputData,  
                            @State,  
                            @StatusInfo
                        )

                COMMIT";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenWithRetryAsync(conn => conn.ExecuteAsync(sql, jobDescriptions));
            }
        }

        private DateTime GetSqlSafeDate(DateTime date)
        {
            if (date < (DateTime)SqlDateTime.MinValue)
                return (DateTime)SqlDateTime.MinValue;

            if (date > (DateTime) SqlDateTime.MaxValue)
                return (DateTime) SqlDateTime.MaxValue;

            return date;
        }


    }
}