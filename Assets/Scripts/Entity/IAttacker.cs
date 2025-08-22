using System.Collections.Generic;

public interface IAttacker
{
    void AddOnHitConsumer(AConsumerFactory consumerFactory);
    List<AConsumerFactory> GetOnHitConsumers();
    void RemoveOnHitConsumer(AConsumerFactory onHitConsumer);

    void AddOnHitEffect(ABuffHandlerFactory onHitEffect);
    List<ABuffHandlerFactory> GetOnHitEffects();
    void RemoveOnHitEffect(ABuffHandlerFactory onHitEffect);
}