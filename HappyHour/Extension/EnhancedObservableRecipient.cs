using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Extension
{
    public abstract class EnhancedObservableRecipient : ObservableRecipient
    {
        // Cache reflection results.
        private static readonly MethodInfo RegisterMethodInfo = typeof(IMessengerExtensions)
            .GetMethod(nameof(IMessengerExtensions.Register), BindingFlags.Public | BindingFlags.Static);
        private static readonly MethodInfo UnregisterMethodInfo = typeof(IMessenger)
            .GetMethod("Unregister", [typeof(object)]);


        protected override void OnActivated()
        {
            base.OnActivated();
            RegisterAsyncHandlers();
        }

        protected override void OnDeactivated()
        {
            UnregisterAsyncHandlers();
            base.OnDeactivated();
        }

        private void RegisterAsyncHandlers()
        {
            var asyncRecipientInterfaces = this.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncRecipient<>));

            foreach (var iface in asyncRecipientInterfaces)
            {
                var messageType = iface.GetGenericArguments()[0];
                if (RegisterMethodInfo != null)
                {
                    var registerMethod = RegisterMethodInfo.MakeGenericMethod(messageType);
                    registerMethod.Invoke(null, [Messenger, this]);
                }
            }
        }

        private void UnregisterAsyncHandlers()
        {
            var interfaces = GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncRecipient<>));

            foreach (var iface in interfaces)
            {
                var messageType = iface.GetGenericArguments().First();
                if (UnregisterMethodInfo != null)
                {
                    var genericMethod = UnregisterMethodInfo.MakeGenericMethod(messageType);
                    genericMethod.Invoke(Messenger, [this]);
                }
            }
        }
    }
}
