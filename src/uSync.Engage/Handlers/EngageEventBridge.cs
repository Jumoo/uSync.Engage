using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Engage.Infrastructure.Events;

using uSync.BackOffice.SyncHandlers;

namespace uSync.Engage.Handlers;

/// <summary>
///  passes Engage's own events on to the uSync handlers, so changes are exported on save.
/// </summary>
/// <remarks>
///  Engage raises these from its repositories (so for our imports too, which the handlers
///  ignore while uSync is paused) and doesn't go through Umbraco notifications. the 'Saving'
///  events are raised once, on the server that made the change, after it's committed.
///
///  Engage's event list is static, this registers once the site has started.
/// </remarks>
internal class EngageEventBridge :
    INotificationAsyncHandler<UmbracoApplicationStartedNotification>,
    IEventHandler<PersonaGroupSavingEvent>,
    IEventHandler<CustomerJourneyGroupSavingEvent>,
    IEventHandler<GoalsSavingEvent>,
    IEventHandler<CampaignGroupSavingEvent>,
    IEventHandler<SegmentSavingEvent>,
    IEventHandler<SegmentDeletingEvent>,
    IEventHandler<TrafficFilterSavingEvent>
{
    private readonly SyncHandlerCollection _handlers;
    private readonly ILogger<EngageEventBridge> _logger;

    private bool _registering;

    public EngageEventBridge(SyncHandlerCollection handlers, ILogger<EngageEventBridge> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        // what's there now, so the first change after a restart can still spot a delete.
        foreach (var handler in _handlers.Handlers.OfType<IEngageSyncHandler>())
        {
            try
            {
                await handler.TakeSnapshotAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to read the current Engage items for {handler}", handler.GetType().Name);
            }
        }

        // Register replays the last event of each type raised since startup (e.g. by an import
        // at startup), those changes have already happened, so they're ignored here.
        _registering = true;
        try
        {
            SystemEventService.Register<PersonaGroupSavingEvent>(this);
            SystemEventService.Register<CustomerJourneyGroupSavingEvent>(this);
            SystemEventService.Register<GoalsSavingEvent>(this);
            SystemEventService.Register<CampaignGroupSavingEvent>(this);
            SystemEventService.Register<SegmentSavingEvent>(this);
            SystemEventService.Register<SegmentDeletingEvent>(this);
            SystemEventService.Register<TrafficFilterSavingEvent>(this);
        }
        finally
        {
            _registering = false;
        }
    }

    public void Handle(PersonaGroupSavingEvent @event) => ItemsChanged<PersonaGroupHandler>();
    public void Handle(CustomerJourneyGroupSavingEvent @event) => ItemsChanged<CustomerJourneyGroupHandler>();
    public void Handle(GoalsSavingEvent @event) => ItemsChanged<GoalHandler>();
    public void Handle(CampaignGroupSavingEvent @event) => ItemsChanged<CampaignGroupHandler>();
    public void Handle(SegmentSavingEvent @event) => ItemsChanged<SegmentHandler>();
    public void Handle(SegmentDeletingEvent @event) => ItemsChanged<SegmentHandler>();
    public void Handle(TrafficFilterSavingEvent @event) => ItemsChanged<TrafficFilterHandler>();

    // Engage's events are synchronous.
    private void ItemsChanged<THandler>() where THandler : IEngageSyncHandler
    {
        if (_registering) return;

        _handlers.Handlers.OfType<THandler>().FirstOrDefault()?
            .EngageItemsChangedAsync().GetAwaiter().GetResult();
    }
}
