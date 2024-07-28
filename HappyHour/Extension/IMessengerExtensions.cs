using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HappyHour.Extension
{
    public static partial class IMessengerExtensions
    {
        public static void Register<TMessage>(this IMessenger messenger, IAsyncRecipient<TMessage> recipient)
            where TMessage : AsyncMessage
        {
            messenger.Register<TMessage>(recipient, (r, message) =>
            {
                if (r is IAsyncRecipient<TMessage> asyncRecipient)
                {
                    var task = asyncRecipient.ReceiveAsync(message, message.CancellationToken);
                    message.Reply(task);
                }
            });
        }

        public static Task SendAsync<TMessage>(this IMessenger messenger, TMessage message)
            where TMessage : AsyncMessage
        {
            messenger.Send(message);
            return Task.WhenAll(message.Responses);
        }

        public static Task SendAsync<TMessage>(this IMessenger messenger)
            where TMessage : AsyncMessage, new()
        {
            return messenger.SendAsync(new TMessage());
        }
    }

    public class AsyncMessage : CollectionRequestMessage<Task>
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        protected AsyncMessage(CancellationToken cancellationToken = default)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? default;
    }

    public class AsyncMessage<T> : AsyncMessage
    {
        public T Value { get; }

        public AsyncMessage(T value, CancellationToken cancellationToken = default) : base(cancellationToken)
        {
            Value = value;
        }
    }

    public interface IAsyncRecipient<TMessage>
        where TMessage : AsyncMessage
    {
        Task ReceiveAsync(TMessage message, CancellationToken cancellationToken);
    }
}
