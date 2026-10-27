using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Engage.Common.Composing;

using uSync.BackOffice;
using uSync.Core.Extensions;
using uSync.Engage.Handlers;
using uSync.Engage.Services;

namespace uSync.Engage;

[ComposeAfter(typeof(UmbracoEngageApplicationComposer))]
public class uSyncEngageComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AdduSyncEngage();
    }
}

public static class BuilderuSyncEngageExtension
{
    public static IUmbracoBuilder AdduSyncEngage(this IUmbracoBuilder builder)
    {
        // uSync only runs where the backoffice does.
        if (builder.IsUmbracoBackOfficeEnabled() is false)
            return builder;

        builder.AdduSync();

        builder.Services.AddSingleton<EngageReferenceMapper>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, EngageEventBridge>();

        foreach (var entityType in EngageConstants.UdiEntityType.All)
        {
            UdiParser.RegisterUdiType(entityType, UdiType.GuidUdi);
        }

        builder.Services.AddSingleton<IPackageManifestReader, uSyncEngageManifestReader>();

        return builder;
    }
}
