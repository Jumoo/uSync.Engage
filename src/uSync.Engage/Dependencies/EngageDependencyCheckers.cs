using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Engage.Infrastructure.Analytics.Goals;
using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Models;
using Umbraco.Engage.Infrastructure.Personalization.Campaign;
using Umbraco.Engage.Infrastructure.Personalization.CustomerJourney;
using Umbraco.Engage.Infrastructure.Personalization.Personas;
using uSync.Core.Dependency;

using Segment = Umbraco.Engage.Infrastructure.Personalization.Segments.Segment;

namespace uSync.Engage.Dependencies;

/// <summary>
///  the list of things uSync.Complete sends when an Engage item is pushed or pulled.
/// </summary>
/// <remarks>
///  uSync only sends what the dependency checkers return, so without these a push would
///  send nothing. Each item is sent on its own: anything it refers to (personas, journey
///  steps, goals, content) has to be on the other site already, or be pushed first.
///
///  the order is the handler priority, so a push of several types imports in the same
///  order a full import would.
/// </remarks>
public abstract class EngageDependencyCheckerBase<TObject> : ISyncDependencyChecker<TObject>
{
    // Engage items aren't Umbraco entities.
    public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

    protected abstract string EntityType { get; }

    protected abstract int Order { get; }

    protected abstract Guid GetKey(TObject item);

    protected abstract string? GetName(TObject item);

    public Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(TObject item, DependencyFlags flags)
    {
        if (item is null) return Task.FromResult(Enumerable.Empty<uSyncDependency>());

        var key = GetKey(item);

        return Task.FromResult<IEnumerable<uSyncDependency>>([
            new uSyncDependency
            {
                Name = GetName(item) ?? key.ToString(),
                Udi = Udi.Create(EntityType, key),
                Order = Order,
                Level = 0,
                Mode = DependencyMode.MustMatch,
                Flags = flags
            }
        ]);
    }
}

public class PersonaGroupDependencyChecker : EngageDependencyCheckerBase<PersonaGroup>
{
    protected override string EntityType => EngageConstants.UdiEntityType.PersonaGroup;
    protected override int Order => EngageConstants.Priorities.PersonaGroups;
    protected override Guid GetKey(PersonaGroup item) => item.Key;
    protected override string? GetName(PersonaGroup item) => item.Title;
}

public class CustomerJourneyGroupDependencyChecker : EngageDependencyCheckerBase<CustomerJourneyGroup>
{
    protected override string EntityType => EngageConstants.UdiEntityType.CustomerJourneyGroup;
    protected override int Order => EngageConstants.Priorities.CustomerJourneyGroups;
    protected override Guid GetKey(CustomerJourneyGroup item) => item.Key;
    protected override string? GetName(CustomerJourneyGroup item) => item.Title;
}

public class GoalDependencyChecker : EngageDependencyCheckerBase<IGoal>
{
    protected override string EntityType => EngageConstants.UdiEntityType.Goal;
    protected override int Order => EngageConstants.Priorities.Goals;
    protected override Guid GetKey(IGoal item) => item.Key;
    protected override string? GetName(IGoal item) => item.Name;
}

public class CampaignGroupDependencyChecker : EngageDependencyCheckerBase<CampaignGroup>
{
    protected override string EntityType => EngageConstants.UdiEntityType.CampaignGroup;
    protected override int Order => EngageConstants.Priorities.CampaignGroups;
    protected override Guid GetKey(CampaignGroup item) => item.Key;
    protected override string? GetName(CampaignGroup item) => item.Name;
}

public class SegmentDependencyChecker : EngageDependencyCheckerBase<Segment>
{
    protected override string EntityType => EngageConstants.UdiEntityType.Segment;
    protected override int Order => EngageConstants.Priorities.Segments;
    protected override Guid GetKey(Segment item) => item.Key;
    protected override string? GetName(Segment item) => item.Name;
}

public class TrafficFilterDependencyChecker : EngageDependencyCheckerBase<TrafficFilter>
{
    protected override string EntityType => EngageConstants.UdiEntityType.TrafficFilter;
    protected override int Order => EngageConstants.Priorities.TrafficFilters;
    protected override Guid GetKey(TrafficFilter item) => item.Key;
    protected override string? GetName(TrafficFilter item) => item.Name;
}
