namespace MassTransit.Azure.ServiceBus.Core.Tests.Turnout
{
    using System;
    using System.Threading.Tasks;
    using Conductor;
    using Contracts.JobService;
    using Definition;
    using NUnit.Framework;


    [TestFixture]
    public class Submitting_a_job_to_turnout_with_entity_path :
        TwoScopeJobConsumerAzureServiceBusTestFixture
    {
        [Test]
        [Order(1)]
        public async Task Should_get_the_job_accepted()
        {

            var serviceClient = Bus.CreateServiceClient();

            IRequestClient<SubmitJob<CrunchTheNumbers>> requestClient = serviceClient.CreateRequestClient<SubmitJob<CrunchTheNumbers>>();

            Response<JobSubmissionAccepted> response = await requestClient.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = _jobId,
                Job = new {Duration = TimeSpan.FromSeconds(40)}
            });
        }

        [Test]
        [Order(2)]
        public async Task Should_have_received_the_status_check_response()
        {
            ConsumeContext<JobAttemptStatus> statusCheckResponseReceived = await _jobAttemptStatusResponse;
        }

        Guid _jobId;
        Task<ConsumeContext<JobAttemptStatus>> _jobAttemptStatusResponse;

        [OneTimeSetUp]
        public async Task Arrange()
        {
            _jobId = NewId.NextGuid();
        }

        protected override void ConfigureSecondBus(IServiceBusBusFactoryConfigurator configurator)
        {
        }

        protected override void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
        {
            configurator.UseServiceBusMessageScheduler();

            var options = new ServiceInstanceOptions()
                .EnableInstanceEndpoint()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);

            configurator.ServiceInstance(options, instance =>
            {
                instance.ConfigureJobServiceEndpoints(options =>
                {
                    options.StatusCheckInterval = TimeSpan.FromSeconds(30);
                });
            });
        }

        protected override void ConfigureServiceBusReceiveEndpoint(IServiceBusReceiveEndpointConfigurator configurator)
        {
            _jobAttemptStatusResponse = Handled<JobAttemptStatus>(configurator, context => context.Message.JobId == _jobId);
        }

    }
}
