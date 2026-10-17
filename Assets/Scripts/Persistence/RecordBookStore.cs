using Nova.Core;

namespace Nova.Persistence
{
    // Implemented by issue #5 (atomic JSON under persistentDataPath). Core never touches storage.
    public interface RecordBookStore
    {
        RecordBook Load();
        void Save(RecordBook book);
    }
}
