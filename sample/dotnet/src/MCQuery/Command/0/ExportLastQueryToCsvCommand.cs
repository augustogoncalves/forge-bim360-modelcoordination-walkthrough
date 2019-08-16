﻿using MCCommon;
using MCSample;
using MCSample.Model;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Threading.Tasks;

namespace MCQuery.Command
{
    [Export(typeof(IConsoleCommand))]
    internal sealed class ExportLastQueryToCsvCommand : CommandBase
    {
        private readonly IIndexFieldCache _fieldCache;

        private LastQueryState _lastQueryState;

        private IReadOnlyDictionary<string, IndexField> _fields;

        [ImportingConstructor]
        public ExportLastQueryToCsvCommand(IIndexFieldCache fieldCache)
            : base()
        {
            _fieldCache = fieldCache ?? throw new ArgumentNullException(nameof(fieldCache));
        }

        public override string Display => "Export last index query to CSV";

        public override uint Order => 5;

        public override async Task DoInput()
        {
            _lastQueryState = (await SampleFileManager.LoadSavedState<LastQueryState>()) ??
                throw new InvalidOperationException("No cached query not found!");

            _fields = (await _fieldCache.Get(_lastQueryState.Container, _lastQueryState.ModelSet, _lastQueryState.Verison)) ??
                throw new InvalidOperationException("No fields found!");

            Me.OutputPath = SampleFileManager.NewStatePath("out.csv");

            Console.Write($"Output path ({Me.OutputPath.FullName}) : ");
            var path = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(path))
            {
                Me.OutputPath = new FileInfo(path);
            }
        }

        public override async Task RunCommand()
        {
            var resFile = new FileInfo(_lastQueryState.ResultPath);

            if (!resFile.Exists)
            {
                throw new InvalidOperationException("Cached query result file not found!");
            }

            var exporter = new IndexResultCsvExporter(_fields, resFile, Me.OutputPath);

            exporter.ExportProgress += (sender, args) =>
            {
                Console.Write($"\rPercent complete : {args.Percent} %");
            };

            await exporter.Export();

            Console.WriteLine();
        }
    }
}