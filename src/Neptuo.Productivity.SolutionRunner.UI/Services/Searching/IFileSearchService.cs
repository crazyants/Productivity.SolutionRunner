﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SolutionRunner.Services.Searching
{
    public interface IFileSearchService
    {
        Task SearchAsync(string searchPattern, FileSearchMode mode, int count, IFileCollection files);
    }
}
