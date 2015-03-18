﻿using MoreLinq;
using System.Collections.Generic;
using System.Linq;
using NodaTime.Web.Storage;
using System.Collections.Immutable;

namespace NodaTime.Web.Models
{
    public class BenchmarkDiff
    {
        internal const int ImprovementThreshold = 90;
        internal const int RegressionThreshold = 110;

        public BenchmarkRun Left { get; private set; }
        public BenchmarkRun Right { get; private set; }
        public IEnumerable<BenchmarkResult> LeftOnly { get; private set; }
        public IEnumerable<BenchmarkResult> RightOnly { get; private set; }
        public IEnumerable<BenchmarkPair> LeftBetter { get; private set; }
        public IEnumerable<BenchmarkPair> RightBetter { get; private set; }
        public IEnumerable<SourceLogEntry> LogEntries { get; private set; }
        public string Machine { get { return Left.Machine; } }

        public class BenchmarkPair
        {
            public BenchmarkResult Left { get; private set; }
            public BenchmarkResult Right { get; private set; }

            internal long Percent { get { return (Left.PicosecondsPerCall * 100) / Right.PicosecondsPerCall; } }

            internal BenchmarkPair(BenchmarkResult left, BenchmarkResult right)
            {
                this.Left = left;
                this.Right = right;
            }
        }

        public BenchmarkDiff(BenchmarkRun left, BenchmarkRun right, ImmutableList<SourceLogEntry> log)
        {
            Left = left;
            Right = right;
            LeftOnly = left.Results.ExceptBy(right.Results, result => result.FullyQualifiedMethod);
            RightOnly = right.Results.ExceptBy(left.Results, result => result.FullyQualifiedMethod);

            var pairs = (from l in left.Results
                         join r in right.Results on l.FullyQualifiedMethod equals r.FullyQualifiedMethod
                         select new BenchmarkPair(l, r)).ToList();

            LeftBetter = pairs.Where(pair => pair.Percent < ImprovementThreshold).ToList();
            RightBetter = pairs.Where(pair => pair.Percent > RegressionThreshold).ToList();

            bool leftEarlier = BenchmarkRepository.BuildForLabel(left.Label) < BenchmarkRepository.BuildForLabel(right.Label);

            var earlier = leftEarlier ? left : right;
            var later = leftEarlier ? right : left;

            var earlierHash = BenchmarkRepository.HashForLabel(earlier.Label);
            var laterHash = BenchmarkRepository.HashForLabel(later.Label);
            
            LogEntries = log.EntriesBetween(earlierHash, laterHash).ToList();
        }
    }
}