using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lab3Window
{
    // Keeps the list of blocked words and checks URLs against it
    public class KeywordFilter
    {
        private List<string> keywords = new List<string>();

        // Replaces the whole list (used when loading from the database)
        public void SetAll(IEnumerable<string> words)
        {
            keywords = words.Select(w => w.Trim().ToLower()).ToList();
        }

        // Returns a copy of the list
        public List<string> GetAll()
        {
            return keywords.ToList();
        }

        public bool Contains(string word)
        {
            return keywords.Any(k => k.Equals(word.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // Returns false if the word is empty or already in the list
        public bool Add(string word)
        {
            word = word.Trim().ToLower();
            if (word == "" || Contains(word))
                return false;

            keywords.Add(word);
            return true;
        }

        public void Remove(string word)
        {
            keywords.RemoveAll(k => k.Equals(word, StringComparison.OrdinalIgnoreCase));
        }

        // Checks the URL on a background thread. True = blocked.
        public async Task<bool> IsBlockedAsync(string url)
        {
            // Copy, so the background thread doesn't read a changing list
            List<string> copy = keywords.ToList();

            return await Task.Run(() =>
                copy.Any(k => url.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
        }
    }
}