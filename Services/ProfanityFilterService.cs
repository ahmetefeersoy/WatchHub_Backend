using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace api.Services
{
    public interface IProfanityFilterService
    {
        bool ContainsProfanity(string text);
        string FilterProfanity(string text);
    }

    public class ProfanityFilterService : IProfanityFilterService
    {
        // Türkçe ve İngilizce küfür listesi (genişletilebilir)
        private readonly HashSet<string> _profanityWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Türkçe küfürler (örnekler)
            "amk", "aq", "mk", "oç", "og", "siktir", "sikerim", "amına", "amina", "götünü", "gotunu",
            "piç", "pic", "yarak", "taşak", "tasak", "göt", "got", "bok", "pezevenk",
            
            // İngilizce küfürler (örnekler)
            "fuck", "shit", "bitch", "asshole", "bastard", "damn", "crap", "dick",
            
            // Varyasyonlar (rakamlarla yazılanlar)
            "s1kt1r", "s1kerim", "4mk", "0c", "p1c"
        };

        // Küfür varyasyonları için regex pattern'leri
        private readonly List<string> _profanityPatterns = new List<string>
        {
            @"[a@4][m]+[k1!]+",           // amk, ammmk, a4mk vb.
            @"[a@4][q]+",                  // aq, aqqq, a@q vb.
            @"[s$5][i1!][k]+[t]+[i1!][r]+", // siktir vb.
            @"[o0][ç]+",                   // oç, oçç vb.
            @"[p]+[i1!][ç]+",              // piç vb.
            @"f+[u]+c+k+",                 // fuck vb.
            @"[s$5][h]+[i1!]+t+"           // shit vb.
        };

        public bool ContainsProfanity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lowerText = text.ToLower()
                .Replace(" ", "")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "")
                .Replace("?", "");

            // Doğrudan kelime kontrolü
            foreach (var word in _profanityWords)
            {
                if (lowerText.Contains(word.ToLower()))
                    return true;
            }

            // Regex pattern kontrolü
            foreach (var pattern in _profanityPatterns)
            {
                if (Regex.IsMatch(lowerText, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        public string FilterProfanity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var filteredText = text;

            // Kelime bazlı filtreleme
            foreach (var word in _profanityWords)
            {
                var replacement = new string('*', word.Length);
                filteredText = Regex.Replace(
                    filteredText, 
                    $@"\b{Regex.Escape(word)}\b", 
                    replacement, 
                    RegexOptions.IgnoreCase
                );
            }

            // Pattern bazlı filtreleme
            foreach (var pattern in _profanityPatterns)
            {
                filteredText = Regex.Replace(
                    filteredText,
                    pattern,
                    match => new string('*', match.Value.Length),
                    RegexOptions.IgnoreCase
                );
            }

            return filteredText;
        }
    }
}
