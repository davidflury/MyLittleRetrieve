﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MyLittleRetrieve.Helpers;

namespace MyLittleRetrieve.Engine
{
    public class RetrievalEngine
    {
        public Dictionary<string, (FileInfo, string)> Documents { get; set; }

        public Dictionary<FileInfo, Dictionary<string, int>> NonInvertedIndex { get; set; }

        public Dictionary<string, Dictionary<FileInfo, int>> InvertedIndex { get; set; }

        public Dictionary<(string, string), Dictionary<string, int>> QueriesIndex { get; set; }

        public Dictionary<string, double> IdfIndex { get; set; }

        public Dictionary<FileInfo, double> DocumentNormIndex { get; set; }

        private static readonly Regex TokenRegex = new Regex("\\w*");

        private const string SystemName = "MyLittleRetrieve";

        public RetrievalEngine(IEnumerable<string> documents)
        {
            Documents = new Dictionary<string, (FileInfo, string)>();
            NonInvertedIndex = new Dictionary<FileInfo, Dictionary<string, int>>();
            InvertedIndex = new Dictionary<string, Dictionary<FileInfo, int>>();
            QueriesIndex = new Dictionary<(string, string), Dictionary<string, int>>();
            IdfIndex = new Dictionary<string, double>();
            DocumentNormIndex = new Dictionary<FileInfo, double>();

            InitializeDocuments(documents.ToList());
            CalculateIndices();
        }

        private void InitializeDocuments(IReadOnlyCollection<string> documents)
        {
            if (documents == null || !documents.Any())
            {
                return;
            }
            foreach (var document in documents)
            {
                var file = new FileInfo(document);
                var content = file.ReadContent();
                Documents.Add(document, (file, content));
                var tokens = Tokenize(content);
                foreach (var token in tokens)
                {
                    InvertedIndex.Increment(token, file);
                    NonInvertedIndex.Increment(file, token);
                }
            }
        }

        private void CalculateIndices()
        {
            foreach (var (document, tokens) in NonInvertedIndex)
            {
                double dNorm = 0;
                foreach (var (token, count) in tokens)
                {
                    if (!IdfIndex.ContainsKey(token))
                    {
                        IdfIndex.Add(token, Math.Log((1 + Documents.Count) / (double)(1 + InvertedIndex[token].Count)));
                    }
                    dNorm += Math.Pow(count * IdfIndex[token], 2);
                }
                DocumentNormIndex.Add(document, Math.Sqrt(dNorm));
            }
        }

        private void LoadQuery(params string[] files)
        {
            foreach (var query in files)
            {
                var file = new FileInfo(query);
                var content = file.ReadContent();
                LoadQuery(file.Name, content);
            }
        }

        private void LoadQuery(string name, string content)
        {
            var tokens = Tokenize(content);
            foreach (var token in tokens)
            {
                QueriesIndex.Increment((name, content), token);
            }
        }

        public IEnumerable<TrecElement> ProcessQuery(string name, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }
            if (!QueriesIndex.ContainsKey((name, query)))
            {
                LoadQuery(name, query);
            }
            var tokens = QueriesIndex[(name, query)];
            var qNorm = 0.0;
            var accumulators = new Dictionary<FileInfo, double>();
            foreach (var (token, queryCount) in tokens)
            {
                if (!IdfIndex.ContainsKey(token))
                {
                    IdfIndex.Add(token, Math.Log(1 + Documents.Count));
                }
                var b = queryCount * IdfIndex[token];
                qNorm += b * b;
                if (!InvertedIndex.ContainsKey(token)) { continue; }
                foreach (var (document, documentCount) in InvertedIndex[token])
                {
                    var a = documentCount * IdfIndex[token];
                    accumulators.Increment(document, a * b);

                }
            }
            qNorm = Math.Sqrt(qNorm);
            var keys = accumulators.Keys.ToList();
            foreach (var document in keys)
            {
                accumulators[document] /= DocumentNormIndex[document] * qNorm;
            }

            var i = 1;
            return accumulators.ToDictionary(x => (Name: x.Key.Name, File: x.Key.FullName), y => y.Value).OrderByDescending(x => x.Value)
                .Select(x => new TrecElement()
                {
                    Query = name,
                    System = SystemName,
                    Rank = i++,
                    Document = x.Key.Name,
                    RSV = x.Value * 1000,
                    File = x.Key.File
                });
        }

        private static IEnumerable<string> Tokenize(string content)
        {
            var matches = TokenRegex.Matches(content);
            for (var i = 0; i < matches.Count; i++)
            {
                var value = matches[i].Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }
            }
        }
    }
}
