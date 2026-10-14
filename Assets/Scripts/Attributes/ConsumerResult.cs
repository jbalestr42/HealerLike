// What a resource modifier did once processed by a ResourceAttribute
public struct ConsumerResult
{
    // Change of the resource: a heal is positive, damage negative
    public float value;
    public bool isCritical;
    // Part of a heal above the max, lost unless something uses it
    public float overflow;
    // Part of the damage the outer layers took (e.g. a shield), positive, the resource itself only changed by value
    public float absorbed;
    // The whole damage of the hit, before the outer layers took their part (e.g. shown above the unit)
    public float totalValue => value - absorbed;

    public ConsumerResult(float value, bool isCritical, float overflow = 0f, float absorbed = 0f)
    {
        this.value = value;
        this.isCritical = isCritical;
        this.overflow = overflow;
        this.absorbed = absorbed;
    }
}
