using System.Xml.Linq;

using uSync.Core;

namespace uSync.Engage.Serializers;

/// <summary>
///  persona and journey step scores (on goals and campaign groups), written against the
///  persona/step key rather than Engage's numeric id.
/// </summary>
internal static class ScoringXml
{
    public record Score(Guid Key, int Value, bool IsLocked);

    /// <remarks>
    ///  a score of zero that isn't locked is how Engage 'removes' one, so those aren't written.
    ///  sorted by key, so the file doesn't change when the order on a site does.
    /// </remarks>
    public static XElement Serialize(string name, IEnumerable<(Guid? Key, int Value, bool IsLocked)> scores)
        => new(name,
            scores
                .Where(x => x.Key.HasValue && (x.Value != 0 || x.IsLocked))
                .OrderBy(x => x.Key)
                .Select(x => new XElement("Score",
                    new XAttribute("Key", x.Key!.Value),
                    new XAttribute("Value", x.Value),
                    new XAttribute("Locked", x.IsLocked))));

    public static IEnumerable<Score> Deserialize(XElement? element)
        => element?.Elements("Score")
            .Select(x => new Score(
                x.Attribute("Key").ValueOrDefault(Guid.Empty),
                x.Attribute("Value").ValueOrDefault(0),
                x.Attribute("Locked").ValueOrDefault(false)))
            .Where(x => x.Key != Guid.Empty)
        ?? [];
}
