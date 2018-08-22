using System;
using System.Linq;
using Minion.Core.Models;
using Xunit;

namespace Minion.Tests
{
    [Trait("Category", "Sequence Tests")]
    public class SequenceTests : JobScopeTests
    {
        public SequenceTests()
        {
            JobScope = new Sequence();
        }
    }

    [Trait("Category", "Set Tests")]
    public class SetTests : JobScopeTests
    {
        public SetTests()
        {
            JobScope = new Set();
        }
    }

    [Trait("Category", "Job Scope Tests")]
    public abstract class JobScopeTests
    {
        protected JobScope JobScope { get; set; }

        [Fact(DisplayName = "Add Job With Input")]
        public void Add_Job_With_Input()
        {
            //Arrange
            var data = new TestJobWithInputData.TestData
            {
                Text = "text"
            };

            //Act
            var id = JobScope.Add<TestJobWithInputData, TestJobWithInputData.TestData>(data);

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(DateTime.MinValue, jobDescription.DueTime);
            Assert.Equal(0, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithInputData).AssemblyQualifiedName, jobDescription.Type);
            Assert.Equal(typeof(TestJobWithInputData.TestData).AssemblyQualifiedName, jobDescription.Input.Type);
            Assert.Equal(data, jobDescription.Input.InputData);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Job With Input With Due Time")]
        public void Add_Job_With_Input_With_DueTime()
        {
            //Arrange
            var data = new TestJobWithInputData.TestData
            {
                Text = "text"
            };
            var dueTime = new DateTime(2017, 1, 2, 3, 4, 5);

            //Act
            var id = JobScope.Add<TestJobWithInputData, TestJobWithInputData.TestData>(data, dueTime);

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(dueTime, jobDescription.DueTime);
            Assert.Equal(0, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithInputData).AssemblyQualifiedName, jobDescription.Type);
            Assert.Equal(typeof(TestJobWithInputData.TestData).AssemblyQualifiedName, jobDescription.Input.Type);
            Assert.Equal(data, jobDescription.Input.InputData);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Job With Input With Priority")]
        public void Add_Job_With_Input_With_Priority()
        {
            //Arrange
            var data = new TestJobWithInputData.TestData
            {
                Text = "text"
            };
            var priority = 123;

            //Act
            var id = JobScope.Add<TestJobWithInputData, TestJobWithInputData.TestData>(data, priority);

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(DateTime.MinValue, jobDescription.DueTime);
            Assert.Equal(priority, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithInputData).AssemblyQualifiedName, jobDescription.Type);
            Assert.Equal(typeof(TestJobWithInputData.TestData).AssemblyQualifiedName, jobDescription.Input.Type);
            Assert.Equal(data, jobDescription.Input.InputData);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Job With Input With Priority And Due Time")]
        public void Add_Job_With_Input_With_Priority_And_DueTime()
        {
            //Arrange
            var data = new TestJobWithInputData.TestData
            {
                Text = "text"
            };
            var priority = 123;
            var dueTime = new DateTime(2017, 1, 2, 3, 4, 5);

            //Act
            var id = JobScope.Add<TestJobWithInputData, TestJobWithInputData.TestData>(data, dueTime, priority);

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(dueTime, jobDescription.DueTime);
            Assert.Equal(priority, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithInputData).AssemblyQualifiedName, jobDescription.Type);
            Assert.Equal(typeof(TestJobWithInputData.TestData).AssemblyQualifiedName, jobDescription.Input.Type);
            Assert.Equal(data, jobDescription.Input.InputData);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Job Without Input")]
        public void Add_Job_Without_Input()
        {
            //Arrange

            //Act
            var id = JobScope.Add<TestJobWithoutInput>();

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(DateTime.MinValue, jobDescription.DueTime);
            Assert.Equal(0, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithoutInput).AssemblyQualifiedName, jobDescription.Type);
            Assert.Null(jobDescription.Input);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Job Without Input With Due Time")]
        public void Add_Job_Without_Input_With_DueTime()
        {
            //Arrange
            var dueTime = new DateTime(2017, 1, 2, 3, 4, 5);

            //Act
            var id = JobScope.Add<TestJobWithoutInput>(dueTime);

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(dueTime, jobDescription.DueTime);
            Assert.Equal(0, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithoutInput).AssemblyQualifiedName, jobDescription.Type);
            Assert.Null(jobDescription.Input);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Job Without Input With Priority")]
        public void Add_Job_Without_Input_With_Priority()
        {
            //Arrange
            var priority = 123;

            //Act
            var id = JobScope.Add<TestJobWithoutInput>(priority);

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(DateTime.MinValue, jobDescription.DueTime);
            Assert.Equal(priority, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithoutInput).AssemblyQualifiedName, jobDescription.Type);
            Assert.Null(jobDescription.Input);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Job Without Input With Priority And Due Time")]
        public void Add_Job_Without_Input_With_Priority_And_DueTime()
        {
            //Arrange
            var priority = 123;
            var dueTime = new DateTime(2017, 1, 2, 3, 4, 5);

            //Act
            var id = JobScope.Add<TestJobWithoutInput>(dueTime, priority);

            var result = JobScope.Items.Single();

            //Assert
            var jobDescription = (JobDescription)result;

            Assert.Equal(id, jobDescription.Id);
            Assert.NotEqual(Guid.Empty, jobDescription.Id);
            Assert.Equal(dueTime, jobDescription.DueTime);
            Assert.Equal(priority, jobDescription.Priority);
            Assert.Equal(typeof(TestJobWithoutInput).AssemblyQualifiedName, jobDescription.Type);
            Assert.Null(jobDescription.Input);
            Assert.Equal(ExecutionState.Waiting, jobDescription.State);
        }

        [Fact(DisplayName = "Add Sequence")]
        public void Add_Sequence()
        {
            //Arrange
            var sequence = new Sequence();

            //Act
            JobScope.Add(sequence);
            var result = JobScope.Items.Single();

            //Assert
            Assert.Equal(sequence, result);
        }

        [Fact(DisplayName = "Add Set")]
        public void Add_Set()
        {
            //Arrange
            var set = new Set();

            //Act
            JobScope.Add(set);
            var result = JobScope.Items.Single();

            //Assert
            Assert.Equal(set, result);
        }
    }
}