﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Coveralls
{
    internal enum ServiceType
    {
        AppVeyor,
        Unknown
    }

    public class CoverallsBootstrap
    {
        private ICommandOptions _opts;
        private ServiceType _service;

        public CoverallsBootstrap(ICommandOptions options)
        {
            _opts = options;

            if(_opts.Parser == ParserTypes.Unknown) throw new ArgumentException("Unknown parser specified.");

            if (Environment.GetEnvironmentVariable("APPVEYOR").IsNotBlank())
                _service = ServiceType.AppVeyor;
            else
                _service = ServiceType.Unknown;
        }

        public IFileSystem FileSystem { get; set; }

        public string ServiceName
        {
            get
            {
                switch (_service)
                {
                    case ServiceType.AppVeyor:
                        return "appveyor";
                    default:
                        return "coveralls.net";
                }
            }
        }

        public string ServiceJobId
        {
            get
            {
                switch (_service)
                {
                    case ServiceType.AppVeyor:
                        return Environment.GetEnvironmentVariable("APPVEYOR_JOB_ID");
                    default:
                        return "0";
                }
            }
        }

        private string _repoToken;
        public string RepoToken
        {
            get
            {
                if (_repoToken.IsBlank())
                {
                    _repoToken = Environment.GetEnvironmentVariable("COVERALLS_REPO_TOKEN");
                }

                return _repoToken;
            }
            set { _repoToken = value; }
        }

        private IEnumerable<CoverageFile> _files;
        public IEnumerable<CoverageFile> CoverageFiles
        {
            get
            {
                if (_files == null || !_files.Any())
                {
                    var parser = CreateParser();
                    var reportXml = FileSystem.ReadFileText(_opts.InputFile);
                    if (reportXml.IsBlank())
                    {
                        throw new Exception("Invalid coverage file");
                    }
                    parser.Report = XDocument.Parse(reportXml);

                    _files = parser.Generate();
                }
                return _files;
            }
        }

        private IGitRepository _repository;
        public IGitRepository Repository
        {
            get
            {
                if (_repository == null)
                {
                    switch (_service)
                    {
                        case ServiceType.AppVeyor:
                            _repository = new AppVeyorGit();
                            break;
                        default:
                            _repository = new LocalGit();
                            break;
                    }
                }
                return _repository;
            }
        }

        public ICoverageParser CreateParser()
        {
            switch (_opts.Parser)
            {
                case ParserTypes.OpenCover:
                    return new OpenCoverParser(FileSystem);
            }
            return new OpenCoverParser(FileSystem);
        }

        public IGitRepository CreateGitRepository()
        {
            if (Environment.GetEnvironmentVariable("APPVEYOR_JOB_ID").IsNotBlank()) return new AppVeyorGit();
            else return new LocalGit();
        }
    }
}
