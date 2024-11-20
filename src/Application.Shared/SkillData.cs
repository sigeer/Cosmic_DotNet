using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Shared
{
    public class SkillData
    {
        protected int id, level, masterLevel;
        List<int> jobs = new();

        public SkillData(int id, int level, int masterLevel, List<int> jobs)
        {
            this.id = id;
            this.level = level;
            this.masterLevel = masterLevel;
            this.jobs = jobs;
        }

        public int getId()
        {
            return id;
        }

        public int getLevel()
        {
            return level;
        }

        public int getMasterLevel()
        {
            return masterLevel;
        }

        public bool jobsContains(int jobId)
        {
            return jobs.Contains(jobId);
        }


    }
}
