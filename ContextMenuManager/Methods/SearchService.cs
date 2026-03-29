using ContextMenuManager.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextMenuManager.Methods
{
    public interface ISearchable
    {
        string[] GetSearchKeywords();
        int GetSearchPriority();
    }

    public sealed class SearchService
    {
        private readonly List<MyListItem> originalItems = [];
        private readonly Dictionary<MyListItem, SearchableContent> itemContentCache = [];

        public int TotalItemCount => originalItems.Count;

        public void Initialize(IEnumerable<MyListItem> items)
        {
            originalItems.Clear();
            itemContentCache.Clear();

            foreach (var item in items)
            {
                originalItems.Add(item);
                CacheItemContent(item);
            }
        }

        public void Clear()
        {
            originalItems.Clear();
            itemContentCache.Clear();
        }

        public void AddItem(MyListItem item)
        {
            if (!originalItems.Contains(item))
            {
                originalItems.Add(item);
                CacheItemContent(item);
            }
        }

        private void CacheItemContent(MyListItem item)
        {
            var content = new SearchableContent();

            if (!string.IsNullOrEmpty(item.Text))
            {
                content.PrimaryText = item.Text.ToLower();
            }

            if (!string.IsNullOrEmpty(item.SubText))
            {
                content.SecondaryText = item.SubText.ToLower();
            }

            if (item is ISearchable searchable)
            {
                var extraKeywords = searchable.GetSearchKeywords();
                if (extraKeywords != null)
                {
                    foreach (var keyword in extraKeywords)
                    {
                        if (!string.IsNullOrEmpty(keyword))
                        {
                            content.Keywords.Add(keyword.ToLower());
                        }
                    }
                }
            }

            itemContentCache[item] = content;
        }

        public List<SearchResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return originalItems.Select(item => new SearchResult
                {
                    Item = item,
                    Score = 0,
                    MatchedKeywords = []
                }).ToList();
            }

            var searchTerms = ParseSearchTerms(query.ToLower());
            if (searchTerms.Count == 0)
            {
                return originalItems.Select(item => new SearchResult
                {
                    Item = item,
                    Score = 0,
                    MatchedKeywords = []
                }).ToList();
            }

            var results = new List<SearchResult>();

            foreach (var item in originalItems)
            {
                if (!itemContentCache.TryGetValue(item, out var content))
                {
                    content = new SearchableContent();
                    if (!string.IsNullOrEmpty(item.Text)) content.PrimaryText = item.Text.ToLower();
                    if (!string.IsNullOrEmpty(item.SubText)) content.SecondaryText = item.SubText.ToLower();
                    itemContentCache[item] = content;
                }

                var score = CalculateMatchScore(content, searchTerms, out var matchedKeywords);

                if (score > 0)
                {
                    var priority = 0;
                    if (item is ISearchable searchable)
                    {
                        priority = searchable.GetSearchPriority();
                    }

                    results.Add(new SearchResult
                    {
                        Item = item,
                        Score = score + priority,
                        MatchedKeywords = matchedKeywords
                    });
                }
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        private static List<string> ParseSearchTerms(string query)
        {
            var terms = new List<string>();
            var currentTerm = new List<char>();
            var inQuotes = false;

            foreach (var c in query)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentTerm.Count > 0)
                    {
                        terms.Add(new string(currentTerm.ToArray()));
                        currentTerm.Clear();
                    }
                }
                else
                {
                    currentTerm.Add(c);
                }
            }

            if (currentTerm.Count > 0)
            {
                terms.Add(new string(currentTerm.ToArray()));
            }

            return terms;
        }

        private int CalculateMatchScore(SearchableContent content, List<string> searchTerms, out HashSet<string> matchedKeywords)
        {
            matchedKeywords = [];
            var totalScore = 0;
            var matchedTerms = 0;

            foreach (var searchTerm in searchTerms)
            {
                if (string.IsNullOrEmpty(searchTerm)) continue;

                var termScore = 0;
                var termMatched = false;
                var termMatches = new HashSet<string>();

                if (!string.IsNullOrEmpty(content.PrimaryText))
                {
                    var matchResult = MatchText(content.PrimaryText, searchTerm);
                    if (matchResult.Score > 0)
                    {
                        termScore = Math.Max(termScore, matchResult.Score + 30);
                        termMatched = true;
                        if (matchResult.IsMatch) termMatches.Add(content.PrimaryText);
                    }
                }

                if (!string.IsNullOrEmpty(content.SecondaryText))
                {
                    var matchResult = MatchText(content.SecondaryText, searchTerm);
                    if (matchResult.Score > 0)
                    {
                        termScore = Math.Max(termScore, matchResult.Score);
                        termMatched = true;
                        if (matchResult.IsMatch) termMatches.Add(content.SecondaryText);
                    }
                }

                foreach (var keyword in content.Keywords)
                {
                    var matchResult = MatchText(keyword, searchTerm);
                    if (matchResult.Score > 0)
                    {
                        if (matchResult.Score > termScore)
                        {
                            termScore = matchResult.Score;
                            termMatched = true;
                        }
                        if (matchResult.IsMatch) termMatches.Add(keyword);
                    }
                }

                if (termMatched)
                {
                    matchedTerms++;
                    totalScore += termScore;
                    foreach (var m in termMatches)
                    {
                        matchedKeywords.Add(m);
                    }
                }
            }

            if (matchedTerms < searchTerms.Count(t => !string.IsNullOrEmpty(t)))
            {
                return 0;
            }

            return totalScore;
        }

        private static MatchResult MatchText(string text, string searchTerm)
        {
            var result = new MatchResult();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
            {
                return result;
            }

            if (text == searchTerm)
            {
                result.Score = 100;
                result.IsMatch = true;
                return result;
            }

            if (text.StartsWith(searchTerm))
            {
                var ratio = (double)searchTerm.Length / text.Length;
                result.Score = (int)(90 * ratio);
                result.IsMatch = true;
                return result;
            }

            if (searchTerm.Length >= 2 && text.Contains(searchTerm))
            {
                var ratio = (double)searchTerm.Length / text.Length;
                result.Score = (int)(70 * ratio);
                result.IsMatch = true;
                return result;
            }

            return result;
        }

        public List<MyListItem> GetOriginalItems()
        {
            return [.. originalItems];
        }

        public bool HasOriginalItems => originalItems.Count > 0;

        private sealed class SearchableContent
        {
            public string PrimaryText { get; set; }
            public string SecondaryText { get; set; }
            public List<string> Keywords { get; } = [];
        }

        private sealed class MatchResult
        {
            public int Score { get; set; }
            public bool IsMatch { get; set; }
        }
    }

    public sealed class SearchResult
    {
        public MyListItem Item { get; set; }
        public int Score { get; set; }
        public HashSet<string> MatchedKeywords { get; set; }
    }
}
