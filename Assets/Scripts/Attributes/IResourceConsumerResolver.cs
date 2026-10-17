// Turns the consumers sent to a resource into the value it changes by (e.g. damage reduced by armor, critical heals)
public interface IResourceConsumerResolver
{
    // Reads the attributes it needs from the owner of the resource
    void Init(AttributeManager attributeManager);

    // The value the resource changes by and whether it is critical, the consumers of the modifier used up
    (float value, bool isCritical) ComputeValue(ResourceAttribute resourceAttribute, ResourceModifier resourceModifier);
}
