using System;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json.Linq;

namespace Example.Test
{
    #region Primary Message Object Interface
    /*
     * An interface implementing an interface? What madness is this?!
     *
     * It's a way to isolate functionality. This is the 'I' in SOLID. Interface Segregation.
     *
     * We need two bits of functionality; one for the entry point to call WriteOut and one internal to access additional information.
     */

    public interface IWritableMessageObject : IMessageObject, ICanWriteOut{}

    /*
     * OK - This is a VERY data centric interface. Which is not OO.
     * Simply put - I am skimping on creating the "data layer". There's to many options to show a sensible general example.
     * Since it's methods; it's a little better than Properties.
     *
     * I'd have to dig into the specific data layer to find what'd be a good way to save that - if I could find one. Might not.
     *
     * A lot of the time it's easier to start with the data, see how it's used, then pull that behavior in and hide the data.
     */
    public interface IMessageObject
    {
        string MessageId();
        bool Example_ShouldBeExcluded();
        bool Other_ShouldBeExcluded();
        bool HasMemberEventOnly();
        void Save(ISaver saver);
    }

    public interface ICanWriteOut
    {
        void Process();
    }
    #endregion

    /*
     * specific scenario for an incoming message
     */
    public sealed class MemberEventMessageObject : IWritableMessageObject
    {
        /*
         * This is an example implementation given something like a Member Event.
         */

        #region Don't Use Dynamics
        /*
         * NEVER USE DYNAMIC IN REAL CODE. You won't need it 99.99% percent of the time; if you think you do; you're probably wrong.
         * 'dynamic' is used here because I don't want to define a bunch of placer holder classes
         */

        /*
         * These represent where ever we pull data from. Probably a database, but could be elsewhere.
         * Each of these could be the same database, but different SQL used to retrieve data.
         */
        private readonly dynamic _dataLoadSourceOne;
        private readonly dynamic _dataLoadSourceTwo;
        #endregion

        #region Readonly Class Fields
        private readonly JObject _sourceJson;
        private readonly IEventSystem _eventSystem;
        private readonly IBusinessRule _businessRule;
        #endregion

        #region Caching Fields
        /*
         * I use local caching in a class to hit the database once for the set of data and hold onto it.
         * If it matters, the caching can become more advanced, but for short lived things this is good enough.
         */
        private LoadedData _cachedDataSourceOne;
        private LoadedData _cachedDataSourceTwo;

        /*
         * These are the methods that do the actual retrieval and caching.
         * the <code>??=</code> is equivalent to
         * <code>
               if(_cached == null) _cached = _dataLoadSource.LoadData()
               return _cachedData
         * </code>
         */
        private LoadedData CachedDataSourceOne() => _cachedDataSourceOne ??= _dataLoadSourceOne.LoadData();
        private LoadedData CachedDataSourceTwo() => _cachedDataSourceTwo ??= _dataLoadSourceOne.LoadData();
        #endregion


        #region Constructors
        /*
         * As this is the MemberEvent, we'll want the MemberEventBusinessRules to execute
         */
        public MemberEventMessageObject(JObject sourceJson, IEventSystem eventSystem) : this(sourceJson, eventSystem, new MemberEventBusinessRules()) { }

        private MemberEventMessageObject(JObject sourceJson, IEventSystem eventSystem, IBusinessRule businessRule)
        {
            _sourceJson = sourceJson;
            _eventSystem = eventSystem;
            _businessRule = businessRule;
        }
        #endregion

        #region Example From JSON Source Data
        /*
         * There can be data from multiple data sources, this is an example of pulling it from the JSON
         */
        public string MessageId() => _sourceJson.Value<string>("MessageId");
        public bool Example_ShouldBeExcluded() => _sourceJson.Value<bool>("Example");
        public bool Other_ShouldBeExcluded() => _sourceJson.Value<bool>("Other");
        #endregion

        #region Example of answering a question about the data
        /*
         * This is an example of how behavior can be moved into the object.
         * If something needs to know if a piece of data exists; let's do that check for them.
         *
         * This allows us to change how "no data" is represented.
         * We could make "NOT_AVAILABLE" how we know there's no data and our this method changes, but nothing else in the system would.
         */
        public bool HasMemberEventOnly() => string.IsNullOrWhiteSpace(MemberEventOnlyValue());


        #endregion

        #region The main interaction points
        //This starts the flow
        public void Process()
        {
            try
            {
                //Run the rules - They'll know what to do
                _businessRule.Apply(this);

                //If we don't blow up - we're finished. Complete our self
                _eventSystem.Complete();
            }
            catch(Exception ex)
            {
                //We don't know what happened, but it shouldn't have - Let's abandon so it can be tried again
                _eventSystem.Abandon();

                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        /*
         * When the objects needs to be saved, we use a call back.
         * This allows us to never expose the data out of the object.
         *
         * We can also change the saver w/o ever having change how the object serializes itself.
         * This forces all things that want to save the object (or push to a different event system) to implement the ISaver interface.
         *
         * This object PUSHES it's data. Nothing PULLS the data. This design around data forces better design into the system. Data should not flow.
         */
        public void Save(ISaver saver) => saver.Save(Serialized());
        #endregion

        #region Serialization
        /*
         * Self Serialization gives us A LOT of flexibility.
         * There's no checks about what data gets set how - this object KNOWS it's representation.
         * It can populate all the values without a bunch of if's and checks. Just do it.
         *
         * Even though this is a private method, we still want to isolate it into a method. Makes it simpler for us to focus on it, or
         * to extract it if/when it gets complicated.
         */
        private DataSaveSerialized Serialized()
        {
            //Except... serialize the data into the object... however it needs
            return new DataSaveSerialized { MessageId = MessageId(), OtherValue = OtherValue_FromDataLoad()};
        }
        #endregion


        #region Example from Cached Data Source
        /*
         * And a contrived example from the data source. The idea here is these would be 'Column' names
         *
         * These are private because they are data; should be required for internal or serialization, but not exposed
         */
        private string OtherValue_FromDataLoad() => CachedDataSourceOne()["OtherValue"];
        private string AnotherValue_FromDataLoad() => CachedDataSourceTwo()["AnotherValue"];
        private string MemberEventOnlyValue() => CachedDataSourceTwo()["ThatValue"];
        #endregion
    }


    public class UnknownMessageObject : IWritableMessageObject
    {
        private readonly JObject _jObject;
        private readonly IEventSystem _eventSystem;
        private readonly ISaver _saver;

        #region Constructors
        public UnknownMessageObject(JObject jObject, IEventSystem eventSystem) : this(jObject, eventSystem, new UnknownSaver()) { }

        private UnknownMessageObject(JObject jObject, IEventSystem eventSystem, ISaver saver)
        {
            _jObject = jObject;
            _eventSystem = eventSystem;
            _saver = saver;
        }
        #endregion

        #region No Data Should Work
        public string MessageId() => throw new NotSupportedException("Unknown Member Invoked");
        public bool Example_ShouldBeExcluded() => throw new NotSupportedException("Unknown Member Invoked");
        public bool Other_ShouldBeExcluded() => throw new NotSupportedException("Unknown Member Invoked");
        public bool HasMemberEventOnly() => throw new NotSupportedException("Unknown Member Invoked");
        public void Save(ISaver saver) => saver.Save(Serialized());

        #endregion

        public void Process()
        {
            try
            {
                //Save it
                _saver.Save(Serialized());
                //We don't know what this event is - Just complete it no matter what happens
                _eventSystem.Complete();
            }
            catch(Exception ex)
            {
                //We don't know what it is... but if something went this wrong; let's go ahead and abandon it
                _eventSystem.Abandon();

                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }


        #region Serialization
        /*
         * Self Serialization gives us A LOT of flexibility.
         * There's no checks about what data gets set how - this object KNOWS it's representation.
         * It can populate all the values without a bunch of if's and checks. Just do it.
         */
        private DataSaveSerialized Serialized()
        {
            //Except... serialize the data into the object... however it needs
            //Probably with an interface so there could be an "UnknownSerialized" type of object
            //This is the data layer, it gets pretty specific at time and defies this general example idea
            return new DataSaveSerialized();
        }
        #endregion
    }
}