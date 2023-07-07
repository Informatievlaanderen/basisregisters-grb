namespace Grb.Building.Tests.NotificationService
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;
    using FluentAssertions;
    using Moq;
    using Notifications;
    using Xunit;

    public class NotificationServiceTests
    {
        [Fact]
        public async Task ThenNotificationIsSend()
        {
            var mockSns = new Mock<IAmazonSimpleNotificationService>();
            var sut = new NotificationService(mockSns.Object, "topic");

            PublishRequest publishRequest = null;

            mockSns.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PublishResponse())
                .Callback<PublishRequest, CancellationToken>((pr, ct) => publishRequest = pr);

            var notificationMessage = new NotificationMessage(
                nameof(Grb.Building.Processor.Job),
                $"JobRecordErrors, Job: 1001 has 5 errors.",
                "Grb job processor",
                NotificationSeverity.Danger);

            await sut.PublishToTopicAsync(notificationMessage);

            var expectedMessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "MessageType", new MessageAttributeValue { DataType = "String", StringValue = nameof(Grb.Building.Processor.Job) } },
                { "service", new MessageAttributeValue { DataType = "String", StringValue = "Grb job processor" } },
                { "warning", new MessageAttributeValue { DataType = "String", StringValue = "danger" } }
            };

            publishRequest.Message.Should().Be(@"{""basisregistersError"":""JobRecordErrors, Job: 1001 has 5 errors."",""service"":""Grb job processor"",""warning"":""danger""}");
            publishRequest.TopicArn.Should().Be("topic");
            publishRequest.MessageAttributes.Should().BeEquivalentTo(expectedMessageAttributes);
        }
    }
}
