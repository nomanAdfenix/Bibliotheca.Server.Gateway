﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bibliotheca.Server.Depository.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Depository.Client;
using Bibliotheca.Server.Gateway.Core.DataTransferObjects;
using Microsoft.Extensions.Caching.Memory;

namespace Bibliotheca.Server.Gateway.Core.Services
{
    public class ProjectsService : IProjectsService
    {
        private const string _allProjectsInformationCacheKey = "all-projects-information";

        private readonly IProjectsClient _projectsClient;

        private readonly IMemoryCache _memoryCache;

        public ProjectsService(IProjectsClient projectsClient, IMemoryCache memoryCache)
        {
            _projectsClient = projectsClient;
            _memoryCache = memoryCache;
        }

        public async Task<FilteredResutsDto<ProjectDto>> GetProjectsAsync(ProjectsFilterDto filter)
        {
            IList<ProjectDto> projects = null;
            if (!TryGetProjects(out projects))
            {
                projects = await _projectsClient.Get();
                AddProjects(projects);
            }

            IEnumerable<ProjectDto> query = projects;
            query = FilterByName(filter, query);
            query = FilterByGroups(filter, query);
            query = FilterByTags(filter, query);

            var allResults = query.Count();

            query = query.OrderBy(x => x.Name);
            if (filter.Limit > 0)
            {
                query = query.Skip(filter.Page * filter.Limit).Take(filter.Limit);
            }

            var filteredResults = new FilteredResutsDto<ProjectDto>
            {
                Results = query,
                AllResults = allResults
            };

            return filteredResults;
        }

        public async Task<ProjectDto> GetProjectAsync(string projectId)
        {
            var projects = await _projectsClient.Get(projectId);
            return projects;
        }

        public async Task CreateProjectAsync(ProjectDto project)
        {
            await _projectsClient.Post(project);
        }

        public async Task UpdateProjectAsync(string projectId, ProjectDto project)
        {
            await _projectsClient.Put(projectId, project);
        }

        public async Task DeleteProjectAsync(string projectId)
        {
            await _projectsClient.Delete(projectId);
        }

        public bool TryGetProjects(out IList<ProjectDto> projects)
        {
            return _memoryCache.TryGetValue(_allProjectsInformationCacheKey, out projects);
        }

        public void AddProjects(IList<ProjectDto> projects)
        {
            _memoryCache.Set(_allProjectsInformationCacheKey, projects,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
        }

        private static IEnumerable<ProjectDto> FilterByTags(ProjectsFilterDto filter, IEnumerable<ProjectDto> query)
        {
            if (filter.Tags != null)
            {
                var tagsNormalized = new List<string>(filter.Tags.Count);
                foreach (var item in filter.Tags)
                {
                    tagsNormalized.Add(item.ToUpper());
                }

                query = query.Where(t2 => tagsNormalized.Any(t1 => t2.Tags.Contains(t1)));
            }

            return query;
        }

        private static IEnumerable<ProjectDto> FilterByGroups(ProjectsFilterDto filter, IEnumerable<ProjectDto> query)
        {
            if (filter.Groups != null)
            {
                var groupsNormalized = new List<string>(filter.Groups.Count);
                foreach (var item in filter.Groups)
                {
                    groupsNormalized.Add(item.ToUpper());
                }

                query = query.Where(x => groupsNormalized.Contains(x.Group));
            }

            return query;
        }

        private static IEnumerable<ProjectDto> FilterByName(ProjectsFilterDto filter, IEnumerable<ProjectDto> query)
        {
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var filterQueryNormalized = filter.Query.ToUpper();
                query = query.Where(x => x.Name.Contains(filterQueryNormalized));
            }

            return query;
        }
    }
}
