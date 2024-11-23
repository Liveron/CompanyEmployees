namespace Entities.LinkModels;

public class LinkCollectionWrapper<T>(List<T> value) : LinkResourceBase
{
    public List<T> Value { get; set; } = value;

    public LinkCollectionWrapper() : this([]) { }
}
