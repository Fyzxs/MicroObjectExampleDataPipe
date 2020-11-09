namespace Example.Test
{
    /*
     * INFO: The savers are interactions with 3rd party systems... I'm not doing much representation of those here.
     */

    public interface ISaver
    {
        void Save(DataSaveSerialized serialized);
    }

    /*
     * Example of saving out to multiple places in a given sitaution
     */
    public class SuccessSaver : ISaver
    {
        private readonly ISaver[] _savers;

        public SuccessSaver() : this(new HistorySaver(), new ExternalSaver()) { }

        public SuccessSaver(params ISaver[] savers) => _savers = savers;

        public void Save(DataSaveSerialized serialized)
        {
            foreach (ISaver saver in _savers)
            {
                saver.Save(serialized);
            }
        }
    }

    /*
     * These other classes would have some 3rd part adapter to save through; we don't need to care about the details here.
     */

    public class UnknownSaver : ISaver
    {
        public void Save(DataSaveSerialized serialized)
        {
            //Save it out somewhere... log it?
        }
    }

    public class ExternalSaver : ISaver
    {
        public void Save(DataSaveSerialized serialized)
        {
        }
    }

    public class HistorySaver : ISaver
    {
        public void Save(DataSaveSerialized serialized)
        {
        }
    }

    public sealed class SaveToExcludedPlace : ISaver
    {
        public void Save(DataSaveSerialized serialized)
        {}
    }
}