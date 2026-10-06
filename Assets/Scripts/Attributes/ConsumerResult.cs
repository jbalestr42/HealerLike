// What a resource modifier did once processed by a ResourceAttribute
public struct ConsumerResult
{
    // Change of the resource: a heal is positive, damage negative
    public float value;
    public bool isCritical;
    // Part of a heal above the max, lost unless something uses it
    public float overflow;

    public ConsumerResult(float value, bool isCritical, float overflow = 0f)
    {
        this.value = value;
        this.isCritical = isCritical;
        this.overflow = overflow;
    }
}
