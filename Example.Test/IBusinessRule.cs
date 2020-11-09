namespace Example.Test
{
    public interface IBusinessRule
    {
        void Apply(IMessageObject messageObject);
    }

    #region The Temporal Class
    /*
     * This is the "Temporal Class" because it controls the order the Business Rules are executed.
     * That's it.
     * This classes entire, and only, job is to understand the ordering of the rules for the MemberEvent
     *
     * That makes this class only for the MemberEvent, since it's the set of rules for that.
     *
     * If you need to add a new rule, just figure out where it goes in the flow, and add it there. Done.
     */
    public class MemberEventBusinessRules : IBusinessRule
    {
        private readonly IBusinessRule _nextAction;

        public MemberEventBusinessRules() : this(
            new MemberEventRuleOneBusinessRule(
                new MemberEventRuleTwoBusinessRule(
                    new AllEventsSaveToSuccessLocationBusinessRule())))
        { }

        private MemberEventBusinessRules(IBusinessRule nextAction) => _nextAction = nextAction;

        public void Apply(IMessageObject messageObject) => _nextAction.Apply(messageObject);
    }
    #endregion

    #region General Rules
    /*
     * This says, Rules, plural - but I'm just using one as the example of a rule that can be used for many/all types of events coming in.
     * This saves successfully. If it's a type that should be saved in the success case; use this one.
     */
    public class AllEventsSaveToSuccessLocationBusinessRule : IBusinessRule
    {
        private readonly ISaver _saver;

        public AllEventsSaveToSuccessLocationBusinessRule() : this(new SuccessSaver()) { }

        private AllEventsSaveToSuccessLocationBusinessRule(ISaver saver) => _saver = saver;

        public void Apply(IMessageObject messageObject) => messageObject.Save(_saver);
    }
    #endregion

    #region Type Specific Rules
    /*
     * These rules are for a specific type. These should only be instantiated in the MemberEventBusinessRules class.
     */
    public class MemberEventRuleTwoBusinessRule : IBusinessRule
    {
        #region Name the Constants
        /*
         * What? Naming 'true' and 'false'? Yep.
         * If `DoTheWork` returns true... what's that mean?... Are you sure?
         * Now, with these constants created, we can read the code EXACTLY. The code is more readable.
         *
         * Naming constants makes the code more readable. The compiler is better at optimizing than us.
         * Let the compiler do it's job; we need the code to be readable; let's focus on that.
         */
        private const bool ShouldInvokeNext = false;
        private const bool ShouldNotInvokeNext = true;
        #endregion

        private readonly IBusinessRule _nextAction;
        private readonly ISaver _saver;

        #region Constructors
        public MemberEventRuleTwoBusinessRule(IBusinessRule nextAction) : this(nextAction, new SaveToExcludedPlace()) { }

        private MemberEventRuleTwoBusinessRule(IBusinessRule nextAction, ISaver saver)
        {
            _nextAction = nextAction;
            _saver = saver;
        }
        #endregion
        public void Apply(IMessageObject messageObject)
        {
            /*
             * OMG - Comparing to a boolean?! But the books say not to!!! Right. Never compare to a boolean.
             * Because that's meaningless. This... This is us naming against a constant. It makes the code more legible.
             *
             * Also; we're now free to change the return type without modifying THIS code. We can make 'DoTheWork' return int, and update constants, DONE!
             *
             * Giving things names makes the code more flexible and maintainable. Naming is easy, it's also important.
             */ 
            if (DoTheWork(messageObject) == ShouldNotInvokeNext) return;

            /*
             * Invoke the next action. The great thing about the Temporal class is this one does not know, and does not care what's next.
             * If it should be called - it'll be called. DONE.
             */
            _nextAction.Apply(messageObject);
        }

        private bool DoTheWork(IMessageObject messageObject)
        {
            if (!messageObject.Example_ShouldBeExcluded()) return ShouldInvokeNext;//Should not actually do the work

            messageObject.Save(_saver);//The Work

            return ShouldNotInvokeNext;
        }
    }

    /*
     * Pretty much the same as the above, just a different check
     * So... why not a base class?
     * Composition, not Inheritance. Unless there's significant gain - don't do it.
     * These classes are so small; inheritance isn't going to simplify or clarify anything for us. It will be bad abstraction.
     *
     */
    public class MemberEventRuleOneBusinessRule : IBusinessRule
    {
        private readonly ISaver _saver;
        private const bool ShouldInvokeNext = false;
        private const bool ShouldNotInvokeNext = true;

        private readonly IBusinessRule _nextAction;
        public MemberEventRuleOneBusinessRule(IBusinessRule nextAction) : this(nextAction, new SaveToExcludedPlace()) { }

        private MemberEventRuleOneBusinessRule(IBusinessRule nextAction, ISaver saver)
        {
            _nextAction = nextAction;
            _saver = saver;
        }

        public void Apply(IMessageObject messageObject)
        {
            if (DoTheWork(messageObject) == ShouldNotInvokeNext) return;
            _nextAction.Apply(messageObject);
        }

        private bool DoTheWork(IMessageObject messageObject)
        {
            if (!messageObject.Example_ShouldBeExcluded()) return ShouldInvokeNext;//Should not actually do the work
            //This example has the object needing to be excluded if that bool is true - aka, we get to here

            messageObject.Save(_saver);

            return ShouldNotInvokeNext;
        }
    }
    #endregion
}