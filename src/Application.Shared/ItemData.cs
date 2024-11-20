namespace Application.Shared
{
    public class ItemData
    {
        public int map, id, count, job, gender, period;
        private int? prop;

        public ItemData(int map, int id, int count, int? prop, int job, int gender, int period)
        {
            this.map = map;
            this.id = id;
            this.count = count;
            this.prop = prop;
            this.job = job;
            this.gender = gender;
            this.period = period;
        }

        public int getId()
        {
            return id;
        }

        public int getCount()
        {
            return count;
        }

        public int? getProp()
        {
            return prop;
        }

        public int getJob()
        {
            return job;
        }

        public int getGender()
        {
            return gender;
        }

        public int getPeriod()
        {
            return period;
        }
    }
}
