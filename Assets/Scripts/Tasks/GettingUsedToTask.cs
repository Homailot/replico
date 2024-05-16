namespace Tasks
{
    public class GettingUsedToTask : Task
    {
        public override void StartTask(Logger logger)
        {
            // start clock
        }

        public override void CleanTask(bool success)
        {
            // stop clock
        }

        public override bool Next()
        {
            // shouldn't be called i think
            return false;
        }
    }
}