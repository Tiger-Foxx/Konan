using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Konan.Models;

namespace Konan.Services;

/// <summary>
/// Service de recherche avancée dans l'historique
/// 🦊 Notre renard détective !
/// </summary>
public class SearchService
{
    /// <summary>
    /// Critères de recherche
    /// </summary>
    public class SearchCriteria
    {
        public string? Query { get; set; }
        public ClipboardItemType? Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsFavoriteOnly { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool UseRegex { get; set; }
        public bool CaseSensitive { get; set; }
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public SortOrder SortBy { get; set; } = SortOrder.DateDescending;
    }

    /// <summary>
    /// Ordre de tri
    /// </summary>
    public enum SortOrder
    {
        DateDescending,
        DateAscending,
        UsageCountDescending,
        UsageCountAscending,
        SizeDescending,
        SizeAscending,
        TypeThenDate,
        Relevance
    }

    /// <summary>
    /// Résultat de recherche
    /// </summary>
    public class SearchResult
    {
        public ClipboardItem Item { get; set; } = null!;
        public double RelevanceScore { get; set; }
        public List<string> MatchedTerms { get; set; } = new();
        public string HighlightedPreview { get; set; } = string.Empty;
    }

    /// <summary>
    /// Effectue une recherche avancée
    /// </summary>
    public List<SearchResult> Search(IEnumerable<ClipboardItem> items, SearchCriteria criteria)
    {
        try
        {
            var results = new List<SearchResult>();

            foreach (var item in items)
            {
                var score = CalculateRelevanceScore(item, criteria);
                if (score > 0)
                {
                    var result = new SearchResult
                    {
                        Item = item,
                        RelevanceScore = score,
                        MatchedTerms = GetMatchedTerms(item, criteria),
                        HighlightedPreview = HighlightMatches(item.SearchablePreview, criteria)
                    };
                    results.Add(result);
                }
            }

            // Appliquer le tri
            results = ApplySorting(results, criteria.SortBy);

            Console.WriteLine($"🦊 Recherche effectuée: {results.Count} résultats trouvés");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur recherche: {ex.Message}");
            return new List<SearchResult>();
        }
    }

    /// <summary>
    /// Recherche rapide simple
    /// </summary>
    public List<ClipboardItem> QuickSearch(IEnumerable<ClipboardItem> items, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return items.ToList();

        var criteria = new SearchCriteria
        {
            Query = query,
            SortBy = SortOrder.Relevance
        };

        return Search(items, criteria).Select(r => r.Item).ToList();
    }

    /// <summary>
    /// Calcule le score de pertinence
    /// </summary>
    private double CalculateRelevanceScore(ClipboardItem item, SearchCriteria criteria)
    {
        double score = 0;

        // Filtres obligatoires (score 0 si échec)
        if (!PassesFilters(item, criteria))
            return 0;

        // Score de base
        score = 1.0;

        // Score basé sur la requête textuelle
        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var textScore = CalculateTextScore(item, criteria.Query, criteria.UseRegex, criteria.CaseSensitive);
            if (textScore == 0) return 0; // Pas de correspondance textuelle
            score *= textScore;
        }

        // Bonus pour les favoris
        if (item.IsFavorite)
            score *= 1.5;

        // Bonus basé sur l'usage
        if (item.UsageCount > 0)
            score *= (1.0 + Math.Log10(item.UsageCount + 1) * 0.1);

        // Bonus pour les éléments récents
        var daysSinceCreation = (DateTime.UtcNow - item.CreatedAt).TotalDays;
        if (daysSinceCreation < 7)
            score *= (1.0 + (7 - daysSinceCreation) * 0.05);

        // Bonus pour les éléments récemment utilisés
        if (item.LastUsedAt.HasValue)
        {
            var daysSinceLastUse = (DateTime.UtcNow - item.LastUsedAt.Value).TotalDays;
            if (daysSinceLastUse < 7)
                score *= (1.0 + (7 - daysSinceLastUse) * 0.03);
        }

        return score;
    }

    /// <summary>
    /// Vérifie si l'élément passe les filtres
    /// </summary>
    private static bool PassesFilters(ClipboardItem item, SearchCriteria criteria)
    {
        // Filtre par type
        if (criteria.Type.HasValue && item.Type != criteria.Type.Value)
            return false;

        // Filtre par date
        if (criteria.StartDate.HasValue && item.CreatedAt < criteria.StartDate.Value)
            return false;

        if (criteria.EndDate.HasValue && item.CreatedAt > criteria.EndDate.Value)
            return false;

        // Filtre favoris uniquement
        if (criteria.IsFavoriteOnly && !item.IsFavorite)
            return false;

        // Filtre par tags
        if (criteria.Tags.Any() && !criteria.Tags.Any(tag => item.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            return false;

        // Filtre par taille
        if (criteria.MinSize.HasValue && item.SizeInBytes < criteria.MinSize.Value)
            return false;

        if (criteria.MaxSize.HasValue && item.SizeInBytes > criteria.MaxSize.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Calcule le score textuel
    /// </summary>
    private static double CalculateTextScore(ClipboardItem item, string query, bool useRegex, bool caseSensitive)
    {
        var comparisonType = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var searchableTexts = new[]
        {
            item.Content,
            item.SearchablePreview,
            string.Join(" ", item.Tags)
        };

        double totalScore = 0;

        foreach (var text in searchableTexts.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            double textScore = 0;

            if (useRegex)
            {
                try
                {
                    var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    var matches = Regex.Matches(text, query, regexOptions);
                    textScore = matches.Count > 0 ? Math.Min(matches.Count * 0.5, 2.0) : 0;
                }
                catch
                {
                    // Regex invalide, fallback vers recherche normale
                    textScore = text.Contains(query, comparisonType) ? 1.0 : 0;
                }
            }
            else
            {
                // Recherche par mots-clés
                var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var matchedWords = queryWords.Count(word => text.Contains(word, comparisonType));
                textScore = queryWords.Length > 0 ? (double)matchedWords / queryWords.Length : 0;

                // Bonus pour correspondance exacte
                if (text.Contains(query, comparisonType))
                    textScore *= 1.5;

                // Bonus si le mot apparaît au début
                if (text.StartsWith(query, comparisonType))
                    textScore *= 1.3;
            }

            totalScore = Math.Max(totalScore, textScore);
        }

        return totalScore;
    }

    /// <summary>
    /// Obtient les termes correspondants
    /// </summary>
    private static List<string> GetMatchedTerms(ClipboardItem item, SearchCriteria criteria)
    {
        var matchedTerms = new List<string>();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var comparisonType = criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var words = criteria.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                if (item.Content.Contains(word, comparisonType) ||
                    item.SearchablePreview.Contains(word, comparisonType) ||
                    item.Tags.Any(tag => tag.Contains(word, comparisonType)))
                {
                    matchedTerms.Add(word);
                }
            }
        }

        return matchedTerms.Distinct().ToList();
    }

    /// <summary>
    /// Met en évidence les correspondances
    /// </summary>
    private static string HighlightMatches(string text, SearchCriteria criteria)
    {
        if (string.IsNullOrWhiteSpace(criteria.Query) || string.IsNullOrWhiteSpace(text))
            return text;

        try
        {
            var comparisonType = criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var words = criteria.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var highlightedText = text;
            foreach (var word in words)
            {
                if (criteria.UseRegex)
                {
                    var regexOptions = criteria.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    highlightedText = Regex.Replace(highlightedText, word, "**$0**", regexOptions);
                }
                else
                {
                    // Simple remplacement (pour le moment, on utilise ** pour le highlight)
                    var index = highlightedText.IndexOf(word, comparisonType);
                    while (index >= 0)
                    {
                        highlightedText = highlightedText.Remove(index, word.Length)
                                                       .Insert(index, $"**{word}**");
                        index = highlightedText.IndexOf(word, index + word.Length + 4, comparisonType);
                    }
                }
            }

            return highlightedText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur highlight: {ex.Message}");
            return text;
        }
    }

    /// <summary>
    /// Applique le tri aux résultats
    /// </summary>
    private static List<SearchResult> ApplySorting(List<SearchResult> results, SortOrder sortOrder)
    {
        return sortOrder switch
        {
            SortOrder.DateDescending => results.OrderByDescending(r => r.Item.CreatedAt).ToList(),
            SortOrder.DateAscending => results.OrderBy(r => r.Item.CreatedAt).ToList(),
            SortOrder.UsageCountDescending => results.OrderByDescending(r => r.Item.UsageCount).ToList(),
            SortOrder.UsageCountAscending => results.OrderBy(r => r.Item.UsageCount).ToList(),
            SortOrder.SizeDescending => results.OrderByDescending(r => r.Item.SizeInBytes).ToList(),
            SortOrder.SizeAscending => results.OrderBy(r => r.Item.SizeInBytes).ToList(),
            SortOrder.TypeThenDate => results.OrderBy(r => r.Item.Type).ThenByDescending(r => r.Item.CreatedAt).ToList(),
            SortOrder.Relevance => results.OrderByDescending(r => r.RelevanceScore).ToList(),
            _ => results.OrderByDescending(r => r.RelevanceScore).ToList()
        };
    }

    /// <summary>
    /// Obtient les suggestions de recherche
    /// </summary>
    public List<string> GetSearchSuggestions(IEnumerable<ClipboardItem> items, string partialQuery)
    {
        try
        {
            var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Suggestions basées sur les mots dans le contenu
            foreach (var item in items.Take(100)) // Limiter pour les performances
            {
                var words = ExtractWords(item.Content)
                    .Concat(ExtractWords(item.SearchablePreview))
                    .Concat(item.Tags);

                foreach (var word in words)
                {
                    if (word.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase) && 
                        word.Length > partialQuery.Length)
                    {
                        suggestions.Add(word);
                    }
                }
            }

            return suggestions.Take(10).OrderBy(s => s).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur suggestions: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Extrait les mots d'un texte
    /// </summary>
    private static IEnumerable<string> ExtractWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Enumerable.Empty<string>();

        return Regex.Matches(text, @"\b\w{3,}\b")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Where(w => w.Length >= 3)
                    .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}