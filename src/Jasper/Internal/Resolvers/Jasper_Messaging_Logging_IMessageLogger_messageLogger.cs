using Lamar.IoC;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Jasper.Internal.Resolvers
{
    // START: Jasper_Messaging_Logging_IMessageLogger_messageLogger
    public class Jasper_Messaging_Logging_IMessageLogger_messageLogger : Lamar.IoC.Resolvers.SingletonResolver<Jasper.Messaging.Logging.IMessageLogger>
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory_966905065;
        private readonly Lamar.IoC.Scope _topLevelScope;

        public Jasper_Messaging_Logging_IMessageLogger_messageLogger([Lamar.Named("loggerFactory2")] Microsoft.Extensions.Logging.ILoggerFactory loggerFactory_966905065, Lamar.IoC.Scope topLevelScope) : base(topLevelScope)
        {
            _loggerFactory_966905065 = loggerFactory_966905065;
            _topLevelScope = topLevelScope;
        }



        public override Jasper.Messaging.Logging.IMessageLogger Build(Lamar.IoC.Scope scope)
        {
            var nulloMetrics = new Jasper.Messaging.Logging.NulloMetrics();
            return new Jasper.Messaging.Logging.MessageLogger(_loggerFactory_966905065, nulloMetrics);
        }

    }

    // END: Jasper_Messaging_Logging_IMessageLogger_messageLogger
    
    
}

