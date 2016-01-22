using Microsoft.Azure;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureApiHelpers
{
    public class BusQueue
    {
        public QueueClient Client { get; private set; }

        public BusQueue(BusQueueSettings settings)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);

            if (!namespaceManager.QueueExists(settings.QueueName))
                namespaceManager.CreateQueue(settings.QueueName);

            Client = QueueClient.CreateFromConnectionString(settings.ConnectionString, settings.QueueName);
        }
    }
}
