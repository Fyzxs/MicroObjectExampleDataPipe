namespace Example.Test
{
    /*
     * INFO: Some placeholder classes I didn't want to use dynamic for.
     */

    public class LoadedData
    {
        //This type represents the loaded data - Wherever that comes from
        //It won't really look like this - This is a hack.
        public string this[string index] => "";
    }
    public class DataSaveSerialized
    {
        /*
         * This is a lazy class that I'm using to represent an object serializing itself into.
         * Since it's serialization, it's into some kinda Data Object
         */
        public string MessageId { get; set; }
        public string OtherValue { get; set; }
        public string AnotherValue { get; set; }
        public string MemberEventOnlyValue { get; set; }
    }
}