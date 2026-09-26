// The existing data held by a factory or runtime object, without creating a runtime instance.
public interface IGameDataSource
{
    object sourceData { get; }
}
