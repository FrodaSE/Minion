using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using Minion.InMemory;
using NSubstitute;
using Xunit;

namespace Minion.IntegrationTestingSample
{

    public class IntegrationTestExample
    {
        private readonly IDateSimulationService _dateSimulationService;
        private readonly ITestingBatchStore _store;
        private readonly EmailScheduler _emailScheduler;
        private readonly IEmailSender _emailSender;
        private readonly IDependencyResolver _dependencyResolver;
        private readonly IJobScheduler _scheduler;

        public IntegrationTestExample()
        {
            var startDate = new DateTime(2018, 04, 20);

            _dateSimulationService = new SimpleDateSimulationService(startDate);
            _store = new InMemoryStorage(_dateSimulationService);
            _scheduler = new JobScheduler(_store, _dateSimulationService);
            _emailScheduler = new EmailScheduler(_scheduler, _dateSimulationService);
            _emailSender = Substitute.For<IEmailSender>();
            _dependencyResolver = new SimpleResolver(_emailSender);
        }

        [Fact]
        public async Task Send_Emails()
        {
            var eninge = new TestingBatchEngine(_store, _dateSimulationService, _dependencyResolver);

            var emails = new List<string>
            {
                "to1@example.com",
                "to2@example.com"
            };

            //Call some method that schedules work
            await _emailScheduler.SendEmailsAsync(emails);

            //Should not have received any calls
            _emailSender.DidNotReceive().SendEmail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            //Simulate that 8 hours has passed
            await eninge.AdvanceToDateAsync(_dateSimulationService.GetNow().AddHours(8));

            //Assert that emails have been sent
            _emailSender.Received(1).SendEmail("test@example.com", "to1@example.com", "Hello world!");
            _emailSender.Received(1).SendEmail("test@example.com", "to2@example.com", "Hello world!");
        }
    }


    public class EmailScheduler
    {
        private readonly IJobScheduler _scheduler;
        private readonly IDateService _dateService;

        public EmailScheduler(IJobScheduler scheduler, IDateService dateService)
        {
            _scheduler = scheduler;
            _dateService = dateService;
        }

        public async Task SendEmailsAsync(IEnumerable<string> emails)
        {
            var set = new Set();

            foreach (var email in emails)
            {
                var emailData = new SendEmailJob.EmailData
                {
                    From = "test@example.com",
                    To = email,
                    Message = "Hello world!"
                };

                set.Add<SendEmailJob, SendEmailJob.EmailData>(emailData, _dateService.GetNow().AddHours(8));
            }

            await _scheduler.QueueAsync(set);

        }
    }

    //Simple resolver, in a real application this might be an IOC
    public class SimpleResolver : IDependencyResolver
    {
        private readonly IEmailSender _emailSender;

        public SimpleResolver(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public bool Resolve(Type type, out object resolvedType)
        {
            if (type == typeof(IEmailSender))
            {
                resolvedType = _emailSender;
                return true;
            }

            resolvedType = null;

            return false;

        }
    }


    public class SendEmailJob : Job<SendEmailJob.EmailData>
    {
        private readonly IEmailSender _emailSender;

        public class EmailData
        {
            public string From { get; set; }
            public string To { get; set; }
            public string Message { get; set; }
        }

        public SendEmailJob(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public override async Task<JobResult> ExecuteAsync(EmailData emailData)
        {
            _emailSender.SendEmail(emailData.From, emailData.To, emailData.Message);

            return Finished();
        }
    }

    public interface IEmailSender
    {
        void SendEmail(string from, string to, string message);
    }
}
