﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Umbraco.Core.Cache;
using Umbraco.Core.Hosting;
using Umbraco.Core.Manifest;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Serialization;

namespace Umbraco.Core.Configuration.Grid
{
    internal class GridEditorsConfig : IGridEditorsConfig
    {
        private readonly AppCaches _appCaches;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IManifestParser _manifestParser;

        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<GridEditorsConfig> _logger;

        public GridEditorsConfig(AppCaches appCaches, IHostingEnvironment hostingEnvironment, IManifestParser manifestParser,IJsonSerializer jsonSerializer, ILogger<GridEditorsConfig> logger)
        {
            _appCaches = appCaches;
            _hostingEnvironment = hostingEnvironment;
            _manifestParser = manifestParser;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        public IEnumerable<IGridEditorConfig> Editors
        {
            get
            {
                List<IGridEditorConfig> GetResult()
                {
                    var configFolder = new DirectoryInfo(_hostingEnvironment.MapPathContentRoot(Constants.SystemDirectories.Config));
                    var editors = new List<IGridEditorConfig>();
                    var gridConfig = Path.Combine(configFolder.FullName, "grid.editors.config.js");
                    if (File.Exists(gridConfig))
                    {
                        var sourceString = File.ReadAllText(gridConfig);

                        try
                        {
                            editors.AddRange(_jsonSerializer.Deserialize<IEnumerable<GridEditor>>(sourceString));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Could not parse the contents of grid.editors.config.js into a JSON array '{Json}", sourceString);
                        }
                    }

                    // add manifest editors, skip duplicates
                    foreach (var gridEditor in _manifestParser.Manifest.GridEditors)
                    {
                        if (editors.Contains(gridEditor) == false) editors.Add(gridEditor);
                    }

                    return editors;
                }

                //cache the result if debugging is disabled
                var result = _hostingEnvironment.IsDebugMode
                    ? GetResult()
                    : _appCaches.RuntimeCache.GetCacheItem<List<IGridEditorConfig>>(typeof(GridEditorsConfig) + ".Editors",GetResult, TimeSpan.FromMinutes(10));

                return result;
            }
        }
    }
}
