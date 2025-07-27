using System;
using System.Collections.Generic;
using System.Linq;

namespace SGUnitySDK.Http
{
    public class ErrorMessageBag
    {
        private readonly List<string> _messages;
        private readonly string _defaultSeparator;

        public ErrorMessageBag(IEnumerable<string> messages, string defaultSeparator = "\n")
        {
            _messages = messages?.ToList() ?? new List<string>();
            _defaultSeparator = defaultSeparator;
        }

        // Propriedades úteis
        public bool HasErrors => _messages.Any();
        public int Count => _messages.Count;
        public IReadOnlyList<string> Messages => _messages.AsReadOnly();

        // Métodos de formatação
        public string ToString(string separator = null)
        {
            return string.Join(separator ?? _defaultSeparator, _messages);
        }

        public string ToBulletList(string bulletSymbol = "•")
        {
            return string.Join("\n", _messages.Select(m => $"{bulletSymbol} {m}"));
        }

        public string ToHtmlList()
        {
            return $"<ul>{string.Join("", _messages.Select(m => $"<li>{m}</li>"))}</ul>";
        }

        // Métodos de filtro
        public ErrorMessageBag Filter(Func<string, bool> predicate)
        {
            return new ErrorMessageBag(_messages.Where(predicate), _defaultSeparator);
        }

        // Método estático para criar a partir de uma SGRequestFailedException
        public static ErrorMessageBag FromException(RequestFailedException ex)
        {
            if (ex?.ErrorBody?.Messages == null)
                return new ErrorMessageBag(new[] { "An unknown error occurred" });

            return new ErrorMessageBag(ex.ErrorBody.Messages.Where(m => !string.IsNullOrWhiteSpace(m)));
        }
    }
}