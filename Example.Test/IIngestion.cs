using Newtonsoft.Json.Linq;

namespace Example.Test
{
    /*
     * NOTE: This skeleton is with a specific purpose in mind.
     * It can serve as a general example, but there's definitely a specific example in my mind.
     */

    /*
     * NOTE : Don't use #region in your code. I do it here for explanatory purposes.
     */

    /*
     * INFO: The '=>' used for constructs and methods is called an "Expression Body"
     */

    /*
     * INFO: You'll see this line in try/catch statements
     *    ExceptionDispatchInfo.Capture(ex).Throw();
     * It's the best way to re-throw an exception. The C# compiler isn't up to snuff yet and will complain. It might need to be followed with
     *      throw;
     * The compiler will eventually catch up and that line won't be needed.
     *
     * Most developers don't use ExceptionDispatchInfo.Capture(ex).Throw();. You should. It'll give the most stack trace information available.
     * Just do it.
     * Really.
     */

    /*
     * INFO: Don't catch "Exception". It's bad form. Have the specific exceptions caught so we can identify the types of things going wrong and
     * often apply custom handling.
     * A JsonParseException should be treated differently than a DatabaseUnavailableException. Put that difference into explicitness in the code.
     */

    #region Ingestion Point

    //Simple interface. Simple is good. It ensures out objects don't bloat.
    public interface IIngestion
    {
        void Ingest();
    }

    /*
     * This class is the entry point to process/ingest the messages from an eventing system.
     *
     * It's sole responsibility in the system is to get and invoke the object to save itself
     */
    public sealed class SourceIngestion : IIngestion
    {
        //The interface to the event system
        private readonly IEventSystem _eventSystem;

        #region Constructors
        //We are, of course, using constructor chaining
        //This is a specific type of Ingestion, for a specific "Source". It could be Kafka, Azure, MQ... whatever. In that case it'd be named, MqIngestion; or whichever
        public SourceIngestion():this(new SourceEventSystem()){}

        //We don't want our code dependent on that concrete class, so we can force the interface requirement (also useful for testing)
        private SourceIngestion(IEventSystem eventSystem) => _eventSystem = eventSystem;
        #endregion 
        public void Ingest()
        {
            //Just an example of something we can do to process until there are no more messages.
            //Maybe we only want to process 100 at a time; use a counter - whatever
            while (_eventSystem.HasMessage())
            {
                //We access the IEventSystem for the next message
                ICanWriteOut msg = _eventSystem.NextMessage();
                //This can be inlined, but is on 2 lines for discussion.
                //We simply call the "WriteOut" method and assume the object can handle everything it needs to do to complete it's task
                msg.Process();
            }
        }
    }
    #endregion

    #region Event System Stuff
    /*
    This is an Adapter / Facade / Abstraction over the code that's touching the dependency. 
        
    These classes tend to be non-unit-test-able. They normally require integration tests.
    This is OK as long as we don't put logic in the 3rd party abstraction classes; like this one

    Notice in this class; there's no logic. Just interaction with other things. That's why it's less risky to not have tests around it.
    */
    public sealed class SourceEventSystem : IEventSystem
    {
        private readonly dynamic _actualEventSystem;
        private readonly IMessageObjectFactory _messageObjectFactory;

        public SourceEventSystem():this(new SourceMessageObjectFactory()){}

        private SourceEventSystem(IMessageObjectFactory messageObjectFactory) => _messageObjectFactory = messageObjectFactory;

        public ICanWriteOut NextMessage() => _messageObjectFactory.Instance(_actualEventSystem.NextMessage(), this);

        public bool HasMessage() => false;

        public void Complete() => _actualEventSystem.Complete();

        public void Abandon() => _actualEventSystem.Abandon();
    }

    public interface IEventSystem
    {
        ICanWriteOut NextMessage();
        bool HasMessage();
        void Complete();
        void Abandon();
    }

    #region Object Factory
    /*
     * If you hear me talk enough, you'll hear that I don't like Factories. I don't. That I use it here is no exception to that.
     * I think factories are a sufficient general solution. I find I normally have to work in the specific code to find how to get rid of the
     * factory.
     * Factories are a code smell to me; but I'll use them until I find a better way.
     *
     * So - Until the better way; here's a factory.
     */
    public class SourceMessageObjectFactory : IMessageObjectFactory
    {
        public ICanWriteOut Instance(JObject jObject, IEventSystem eventSystem)
        {
            if (IsMemberEventMessageObject(jObject)) return new MemberEventMessageObject(jObject, eventSystem);
            //Add More Types Here

            return new UnknownMessageObject(jObject, eventSystem);
        }

        private bool IsMemberEventMessageObject(JObject jObject)
        {
            //Same as in the existing code to decide what type it is
            return false;
        }
    }

    public interface IMessageObjectFactory
    {
        ICanWriteOut Instance(JObject jObject, IEventSystem eventSystem);
    }
    #endregion
    #endregion
}
