using System.Reactive.Concurrency;
using WampSharp.Core.Listener;
using WampSharp.Core.Serialization;
using WampSharp.V2.Client;
using WampSharp.V2.Core;
using WampSharp.V2.Realm;
using WampSharp.V2.Transports;

namespace WampSharp.V2
{
    extern alias rxCore;

    internal class InMemoryWampHost : WampHostBase
    {
        private readonly InMemoryBinding mInternalBinding;
        private readonly InMemoryTransport mInternalTransport;
        private readonly WampChannelFactory mChannelFactory = 
            new WampChannelFactory();

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryWampHost"/>.
        /// </summary>
        public InMemoryWampHost() : this(new WampRealmContainer(), new LooseUriValidator())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryWampHost"/> given the
        /// <see cref="IWampRealmContainer"/> associated with this host.
        /// </summary>
        /// <param name="realmContainer"></param>
        /// <param name="uriValidator"></param>
        public InMemoryWampHost(IWampRealmContainer realmContainer,
                                IWampUriValidator uriValidator) :
                                    base(realmContainer, uriValidator)
        {
            mInternalBinding = new InMemoryBinding();

            mInternalTransport = new InMemoryTransport(rxCore::System.Reactive.Concurrency.Scheduler.Immediate);

            this.RegisterTransport
                (mInternalTransport,
                 mInternalBinding);
        }

        public void AddFormatter<TMessage>(IWampFormatter<TMessage> formatter)
        {
            mInternalBinding.AddFormatter(formatter);
        }

        public IWampChannel CreateClientConnection(string realm, IScheduler scheduler)
        {
            IControlledWampConnection<object> connection =
                mInternalTransport.CreateClientConnection(mInternalBinding, scheduler);

            IWampChannel result =
                mChannelFactory.CreateChannel(realm, connection, mInternalBinding);

            return result;
        }
    }
}